namespace ORION.Application.Usuarios;

public interface IUsuarioRepository
{
    Task<UsuarioReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UsuarioReadModel?> ObterPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<UsuarioCreateResult> CriarAsync(
        UsuarioCreateModel model,
        CancellationToken cancellationToken = default);
}
