using ORION.Application.InteracoesIA;
using ORION.Application.Mensagens;
using ORION.Infrastructure.Persistence.Repositories;

public static class AiCoreEndpoints
{
    public static IServiceCollection AddAiCore(this IServiceCollection services)
    {
        services.AddScoped<IMensagemRepository, MensagemRepository>();
        services.AddScoped<IInteracaoIARepository, InteracaoIARepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapAiCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/mensagens/{id:guid}", async (
            Guid id,
            IMensagemRepository repository,
            CancellationToken cancellationToken) =>
        {
            var mensagem = await repository.ObterPorIdAsync(id, cancellationToken);
            return mensagem is null ? Results.NotFound() : Results.Ok(mensagem);
        });

        endpoints.MapPost("/api/mensagens", async (
            MensagemCreateModel model,
            IMensagemRepository repository,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (model.ConversaId == Guid.Empty) errors["conversaId"] = ["conversaId é obrigatório."];
            if (string.IsNullOrWhiteSpace(model.Tipo)) errors["tipo"] = ["tipo é obrigatório."];
            if (string.IsNullOrWhiteSpace(model.Conteudo)) errors["conteudo"] = ["conteudo é obrigatório."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var result = await repository.CriarAsync(model, cancellationToken);
            return result.Status switch
            {
                MensagemCreateStatus.Criada when result.Mensagem is not null =>
                    Results.Created($"/api/mensagens/{result.Mensagem.Id}", result.Mensagem),
                MensagemCreateStatus.ConversaNaoEncontrada => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["conversaId"] = ["Conversa ativa não encontrada."] }),
                MensagemCreateStatus.UsuarioNaoEncontrado => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["usuarioId"] = ["Usuário ativo não encontrado."] }),
                _ => Results.Problem(title: "Não foi possível criar a mensagem.", statusCode: StatusCodes.Status500InternalServerError)
            };
        });

        endpoints.MapGet("/api/interacoes-ia/{id:guid}", async (
            Guid id,
            IInteracaoIARepository repository,
            CancellationToken cancellationToken) =>
        {
            var interacao = await repository.ObterPorIdAsync(id, cancellationToken);
            return interacao is null ? Results.NotFound() : Results.Ok(interacao);
        });

        endpoints.MapPost("/api/interacoes-ia", async (
            InteracaoIACreateModel model,
            IInteracaoIARepository repository,
            CancellationToken cancellationToken) =>
        {
            if (model.AgenteId == Guid.Empty)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["agenteId"] = ["agenteId é obrigatório."] });
            }

            var result = await repository.CriarAsync(model, cancellationToken);
            return result.Status switch
            {
                InteracaoIACreateStatus.Criada when result.Interacao is not null =>
                    Results.Created($"/api/interacoes-ia/{result.Interacao.Id}", result.Interacao),
                InteracaoIACreateStatus.AgenteNaoEncontrado => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["agenteId"] = ["Agente ativo não encontrado."] }),
                InteracaoIACreateStatus.ConversaNaoEncontrada => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["conversaId"] = ["Conversa ativa não encontrada."] }),
                InteracaoIACreateStatus.PacienteNaoEncontrado => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["pacienteId"] = ["Paciente ativo não encontrado."] }),
                InteracaoIACreateStatus.UsuarioNaoEncontrado => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["usuarioId"] = ["Usuário ativo não encontrado."] }),
                _ => Results.Problem(title: "Não foi possível criar a interação IA.", statusCode: StatusCodes.Status500InternalServerError)
            };
        });

        return endpoints;
    }
}
