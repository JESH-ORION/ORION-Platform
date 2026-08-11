namespace ORION.Application.Usuarios;

public sealed class UsuarioCreateModel
{
    public Guid PerfilId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefone { get; init; }
    public string? Documento { get; init; }
    public string? TipoDocumento { get; init; }
}
