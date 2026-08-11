using ORION.Application.IA;
using ORION.Application.InteracoesIA;
using ORION.Application.Mensagens;
using ORION.Infrastructure.AI;
using ORION.Infrastructure.Persistence.Repositories;

public static class AiCoreEndpoints
{
    public static IServiceCollection AddAiCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMensagemRepository, MensagemRepository>();
        services.AddScoped<IInteracaoIARepository, InteracaoIARepository>();
        services.AddScoped<IAgentExecutionService, AgentExecutionService>();

        services.AddScoped<LocalDevelopmentModeloIAProvider>();

        var openAIOptions = new OpenAIModeloIAOptions
        {
            ApiKey = configuration["OpenAI:ApiKey"] ?? string.Empty,
            Model = configuration["OpenAI:Model"] ?? "gpt-5-mini",
            BaseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1"
        };

        services.AddSingleton(openAIOptions);
        services.AddHttpClient<OpenAIModeloIAProvider>();

        services.AddScoped<IModeloIAProvider>(serviceProvider =>
        {
            var provider = configuration["AI:Provider"]?.Trim();

            return string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<OpenAIModeloIAProvider>()
                : serviceProvider.GetRequiredService<LocalDevelopmentModeloIAProvider>();
        });

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

        endpoints.MapPost("/api/agentes/executar", async (
            AgentExecutionRequest request,
            IAgentExecutionService executionService,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (request.InteracaoId == Guid.Empty) errors["interacaoId"] = ["interacaoId é obrigatório."];
            if (request.MensagemId == Guid.Empty) errors["mensagemId"] = ["mensagemId é obrigatório."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var result = await executionService.ExecutarAsync(request, cancellationToken);

            return result.Status switch
            {
                AgentExecutionStatus.Sucesso when result.MensagemResposta is not null => Results.Ok(new
                {
                    status = "finalizada",
                    modelo = result.Modelo,
                    mensagem = result.MensagemResposta
                }),
                AgentExecutionStatus.InteracaoNaoEncontrada => Results.NotFound(new
                {
                    code = "interacao_nao_encontrada",
                    message = "Interação IA não encontrada."
                }),
                AgentExecutionStatus.MensagemNaoEncontrada => Results.NotFound(new
                {
                    code = "mensagem_nao_encontrada",
                    message = "Mensagem não encontrada."
                }),
                AgentExecutionStatus.InteracaoInvalida => Results.Conflict(new
                {
                    code = "interacao_estado_invalido",
                    message = "A interação IA não está em um estado executável."
                }),
                AgentExecutionStatus.ConversaIncompativel => Results.Conflict(new
                {
                    code = "conversa_incompativel",
                    message = "A mensagem e a interação IA não pertencem à mesma conversa."
                }),
                AgentExecutionStatus.FalhaProvider => Results.Problem(
                    title: "Falha ao executar o provider de IA.",
                    detail: environment.IsDevelopment() ? result.Diagnostico : null,
                    statusCode: StatusCodes.Status502BadGateway),
                _ => Results.Problem(
                    title: "Falha ao persistir a execução do agente.",
                    statusCode: StatusCodes.Status500InternalServerError)
            };
        });

        return endpoints;
    }
}
