namespace ORION.Application.Mensagens;

public interface IMensagemRepository
{
    Task<MensagemReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MensagemCreateResult> CriarAsync(MensagemCreateModel model, CancellationToken cancellationToken = default);
}
