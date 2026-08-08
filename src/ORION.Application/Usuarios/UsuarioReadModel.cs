namespace ORION.Application.Usuarios;

public sealed class UsuarioReadModel
{
    public Guid Id { get; set; }
    public Guid PerfilId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Documento { get; set; }
    public string? TipoDocumento { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? UltimoAcesso { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
