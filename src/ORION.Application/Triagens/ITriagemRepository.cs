namespace ORION.Application.Triagens;

public interface ITriagemRepository
{
    Task<TriagemReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TriagemCreateResult> CriarAsync(
        TriagemCreateModel model,
        CancellationToken cancellationToken = default);
}
