using ORION.Application.Usuarios;
using ORION.Infrastructure.Persistence;
using ORION.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var postgreSqlConnectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException(
        "Connection string 'PostgreSQL' não configurada. Use User Secrets ou variável de ambiente.");

builder.Services.AddSingleton<IDbConnectionFactory>(
    _ => new NpgsqlConnectionFactory(postgreSqlConnectionString));
builder.Services.AddScoped<PostgreSqlHealthCheck>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/database", async (
    PostgreSqlHealthCheck healthCheck,
    CancellationToken cancellationToken) =>
{
    try
    {
        var healthy = await healthCheck.IsHealthyAsync(cancellationToken);

        return healthy
            ? Results.Ok(new
            {
                status = "healthy",
                database = "postgresql"
            })
            : Results.Problem(
                title: "PostgreSQL indisponível.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Problem(
            title: "PostgreSQL indisponível.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/usuarios/{id:guid}", async (
    Guid id,
    IUsuarioRepository repository,
    CancellationToken cancellationToken) =>
{
    var usuario = await repository.ObterPorIdAsync(id, cancellationToken);

    return usuario is null
        ? Results.NotFound()
        : Results.Ok(usuario);
});

app.MapPost("/api/usuarios", async (
    UsuarioCreateModel model,
    IUsuarioRepository repository,
    CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();

    if (model.PerfilId == Guid.Empty)
    {
        errors["perfilId"] = ["perfilId é obrigatório."];
    }

    if (string.IsNullOrWhiteSpace(model.Nome))
    {
        errors["nome"] = ["nome é obrigatório."];
    }

    if (string.IsNullOrWhiteSpace(model.Email))
    {
        errors["email"] = ["email é obrigatório."];
    }

    var documentoInformado = !string.IsNullOrWhiteSpace(model.Documento);
    var tipoDocumentoInformado = !string.IsNullOrWhiteSpace(model.TipoDocumento);

    if (documentoInformado != tipoDocumentoInformado)
    {
        errors["documento"] = ["documento e tipoDocumento devem ser informados juntos."];
    }

    if (tipoDocumentoInformado &&
        model.TipoDocumento is not null &&
        !string.Equals(model.TipoDocumento, "CPF", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(model.TipoDocumento, "CNPJ", StringComparison.OrdinalIgnoreCase))
    {
        errors["tipoDocumento"] = ["tipoDocumento deve ser CPF ou CNPJ."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var result = await repository.CriarAsync(model, cancellationToken);

    return result.Status switch
    {
        UsuarioCreateStatus.Criado when result.Usuario is not null => Results.Created(
            $"/api/usuarios/{result.Usuario.Id}",
            result.Usuario),

        UsuarioCreateStatus.PerfilNaoEncontrado => Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["perfilId"] = ["Perfil não encontrado."]
            }),

        UsuarioCreateStatus.EmailDuplicado => Results.Conflict(new
        {
            code = "usuario_email_duplicado",
            message = "Já existe um usuário com este e-mail."
        }),

        UsuarioCreateStatus.DocumentoDuplicado => Results.Conflict(new
        {
            code = "usuario_documento_duplicado",
            message = "Já existe um usuário com este documento."
        }),

        _ => Results.Problem(
            title: "Não foi possível criar o usuário.",
            statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();
