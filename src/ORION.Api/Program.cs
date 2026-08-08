using ORION.Application.Agentes;
using ORION.Application.Conversas;
using ORION.Application.Pacientes;
using ORION.Application.Triagens;
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
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<ITriagemRepository, TriagemRepository>();
builder.Services.AddScoped<IAgenteRepository, AgenteRepository>();
builder.Services.AddScoped<IConversaRepository, ConversaRepository>();

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
            ? Results.Ok(new { status = "healthy", database = "postgresql" })
            : Results.Problem(title: "PostgreSQL indisponível.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Problem(title: "PostgreSQL indisponível.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/usuarios/{id:guid}", async (Guid id, IUsuarioRepository repository, CancellationToken cancellationToken) =>
{
    var usuario = await repository.ObterPorIdAsync(id, cancellationToken);
    return usuario is null ? Results.NotFound() : Results.Ok(usuario);
});

app.MapPost("/api/usuarios", async (UsuarioCreateModel model, IUsuarioRepository repository, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (model.PerfilId == Guid.Empty) errors["perfilId"] = ["perfilId é obrigatório."];
    if (string.IsNullOrWhiteSpace(model.Nome)) errors["nome"] = ["nome é obrigatório."];
    if (string.IsNullOrWhiteSpace(model.Email)) errors["email"] = ["email é obrigatório."];

    var documentoInformado = !string.IsNullOrWhiteSpace(model.Documento);
    var tipoDocumentoInformado = !string.IsNullOrWhiteSpace(model.TipoDocumento);
    if (documentoInformado != tipoDocumentoInformado)
        errors["documento"] = ["documento e tipoDocumento devem ser informados juntos."];

    if (tipoDocumentoInformado && model.TipoDocumento is not null &&
        !string.Equals(model.TipoDocumento, "CPF", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(model.TipoDocumento, "CNPJ", StringComparison.OrdinalIgnoreCase))
        errors["tipoDocumento"] = ["tipoDocumento deve ser CPF ou CNPJ."];

    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var result = await repository.CriarAsync(model, cancellationToken);
    return result.Status switch
    {
        UsuarioCreateStatus.Criado when result.Usuario is not null => Results.Created($"/api/usuarios/{result.Usuario.Id}", result.Usuario),
        UsuarioCreateStatus.PerfilNaoEncontrado => Results.ValidationProblem(new Dictionary<string, string[]> { ["perfilId"] = ["Perfil não encontrado."] }),
        UsuarioCreateStatus.EmailDuplicado => Results.Conflict(new { code = "usuario_email_duplicado", message = "Já existe um usuário com este e-mail." }),
        UsuarioCreateStatus.DocumentoDuplicado => Results.Conflict(new { code = "usuario_documento_duplicado", message = "Já existe um usuário com este documento." }),
        _ => Results.Problem(title: "Não foi possível criar o usuário.", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/api/pacientes/{id:guid}", async (Guid id, IPacienteRepository repository, CancellationToken cancellationToken) =>
{
    var paciente = await repository.ObterPorIdAsync(id, cancellationToken);
    return paciente is null ? Results.NotFound() : Results.Ok(paciente);
});

app.MapPost("/api/pacientes", async (PacienteCreateModel model, IPacienteRepository repository, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (model.UsuarioId == Guid.Empty) errors["usuarioId"] = ["usuarioId é obrigatório."];
    if (string.IsNullOrWhiteSpace(model.NomeCompleto)) errors["nomeCompleto"] = ["nomeCompleto é obrigatório."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var result = await repository.CriarAsync(model, cancellationToken);
    return result.Status switch
    {
        PacienteCreateStatus.Criado when result.Paciente is not null => Results.Created($"/api/pacientes/{result.Paciente.Id}", result.Paciente),
        PacienteCreateStatus.UsuarioNaoEncontrado => Results.ValidationProblem(new Dictionary<string, string[]> { ["usuarioId"] = ["Usuário não encontrado."] }),
        PacienteCreateStatus.UsuarioJaPossuiPaciente => Results.Conflict(new { code = "paciente_usuario_duplicado", message = "Este usuário já possui cadastro de paciente." }),
        PacienteCreateStatus.CpfDuplicado => Results.Conflict(new { code = "paciente_cpf_duplicado", message = "Já existe um paciente com este CPF." }),
        _ => Results.Problem(title: "Não foi possível criar o paciente.", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/api/triagens/{id:guid}", async (Guid id, ITriagemRepository repository, CancellationToken cancellationToken) =>
{
    var triagem = await repository.ObterPorIdAsync(id, cancellationToken);
    return triagem is null ? Results.NotFound() : Results.Ok(triagem);
});

app.MapPost("/api/triagens", async (TriagemCreateModel model, ITriagemRepository repository, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (model.PacienteId == Guid.Empty) errors["pacienteId"] = ["pacienteId é obrigatório."];

    var origem = model.Origem?.Trim().ToUpperInvariant();
    if (string.IsNullOrWhiteSpace(origem)) errors["origem"] = ["origem é obrigatória."];
    else if (origem is not ("IA" or "PROFISSIONAL" or "MANUAL")) errors["origem"] = ["origem deve ser IA, PROFISSIONAL ou MANUAL."];

    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var result = await repository.CriarAsync(model, cancellationToken);
    return result.Status switch
    {
        TriagemCreateStatus.Criada when result.Triagem is not null => Results.Created($"/api/triagens/{result.Triagem.Id}", result.Triagem),
        TriagemCreateStatus.PacienteNaoEncontrado => Results.ValidationProblem(new Dictionary<string, string[]> { ["pacienteId"] = ["Paciente ativo não encontrado."] }),
        TriagemCreateStatus.ConsultaNaoEncontrada => Results.ValidationProblem(new Dictionary<string, string[]> { ["consultaId"] = ["Consulta não encontrada."] }),
        _ => Results.Problem(title: "Não foi possível criar a triagem.", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/api/agentes/{id:guid}", async (Guid id, IAgenteRepository repository, CancellationToken cancellationToken) =>
{
    var agente = await repository.ObterPorIdAsync(id, cancellationToken);
    return agente is null ? Results.NotFound() : Results.Ok(agente);
});

app.MapPost("/api/agentes", async (AgenteCreateModel model, IAgenteRepository repository, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(model.Identificador)) errors["identificador"] = ["identificador é obrigatório."];
    if (string.IsNullOrWhiteSpace(model.Nome)) errors["nome"] = ["nome é obrigatório."];
    if (string.IsNullOrWhiteSpace(model.Versao)) errors["versao"] = ["versao é obrigatória."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var result = await repository.CriarAsync(model, cancellationToken);
    return result.Status switch
    {
        AgenteCreateStatus.Criado when result.Agente is not null => Results.Created($"/api/agentes/{result.Agente.Id}", result.Agente),
        AgenteCreateStatus.IdentificadorVersaoDuplicado => Results.Conflict(new { code = "agente_identificador_versao_duplicado", message = "Já existe um agente com este identificador e versão." }),
        _ => Results.Problem(title: "Não foi possível criar o agente.", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/api/conversas/{id:guid}", async (Guid id, IConversaRepository repository, CancellationToken cancellationToken) =>
{
    var conversa = await repository.ObterPorIdAsync(id, cancellationToken);
    return conversa is null ? Results.NotFound() : Results.Ok(conversa);
});

app.MapPost("/api/conversas", async (ConversaCreateModel model, IConversaRepository repository, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(model.Tipo)) errors["tipo"] = ["tipo é obrigatório."];
    if (!model.PacienteId.HasValue && !model.ProfissionalId.HasValue)
        errors["participante"] = ["Informe pacienteId e/ou profissionalId."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var result = await repository.CriarAsync(model, cancellationToken);
    return result.Status switch
    {
        ConversaCreateStatus.Criada when result.Conversa is not null => Results.Created($"/api/conversas/{result.Conversa.Id}", result.Conversa),
        ConversaCreateStatus.PacienteNaoEncontrado => Results.ValidationProblem(new Dictionary<string, string[]> { ["pacienteId"] = ["Paciente ativo não encontrado."] }),
        ConversaCreateStatus.ProfissionalNaoEncontrado => Results.ValidationProblem(new Dictionary<string, string[]> { ["profissionalId"] = ["Profissional ativo não encontrado."] }),
        _ => Results.Problem(title: "Não foi possível criar a conversa.", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();
