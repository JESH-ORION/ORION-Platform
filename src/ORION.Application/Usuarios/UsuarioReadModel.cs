namespace ORION.Application.Usuarios;

public sealed record UsuarioReadModel(
    Guid Id,
    Guid PerfilId,
    string Nome,
    string Email,
    string? Telefone,
    string? Documento,
    string? TipoDocumento,
    string Status,
    DateTimeOffset? UltimoAcesso,
    DateTimeOffset DataCriacao,
    DateTimeOffset DataAtualizacao);
