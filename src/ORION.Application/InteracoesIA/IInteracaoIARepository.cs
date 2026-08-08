namespace ORION.Application.InteracoesIA;

public interface IInteracaoIARepository
{
    Task<InteracaoIAReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InteracaoIACreateResult> CriarAsync(InteracaoIACreateModel model, CancellationToken cancellationToken = default);
}
