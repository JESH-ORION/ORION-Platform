namespace ORION.Application.IA;

public interface IModeloIAProvider
{
    string NomeModelo { get; }

    Task<ModeloIAResponse> GerarRespostaAsync(
        ModeloIARequest request,
        CancellationToken cancellationToken = default);
}
