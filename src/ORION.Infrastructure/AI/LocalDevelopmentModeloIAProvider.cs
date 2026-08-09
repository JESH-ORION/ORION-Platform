using ORION.Application.IA;

namespace ORION.Infrastructure.AI;

public sealed class LocalDevelopmentModeloIAProvider : IModeloIAProvider
{
    public string NomeModelo => "ORION-LOCAL-DEV";

    public Task<ModeloIAResponse> GerarRespostaAsync(
        ModeloIARequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var conteudo =
            "Resposta simulada de desenvolvimento. " +
            "A mensagem foi recebida pelo executor do agente ORION e o fluxo de IA foi processado com sucesso. " +
            "Nenhuma orientacao clinica foi gerada neste provider local.";

        return Task.FromResult(new ModeloIAResponse(conteudo));
    }
}
