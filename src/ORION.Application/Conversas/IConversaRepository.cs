namespace ORION.Application.Conversas;

public interface IConversaRepository
{
    Task<ConversaReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ConversaCreateResult> CriarAsync(
        ConversaCreateModel model,
        CancellationToken cancellationToken = default);
}
