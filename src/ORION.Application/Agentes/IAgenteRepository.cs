namespace ORION.Application.Agentes;

public interface IAgenteRepository
{
    Task<AgenteReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AgenteCreateResult> CriarAsync(
        AgenteCreateModel model,
        CancellationToken cancellationToken = default);
}
