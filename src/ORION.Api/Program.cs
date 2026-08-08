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

app.Run();
