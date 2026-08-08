namespace ORION.Application.Usuarios;

public enum UsuarioCreateStatus
{
    Criado = 1,
    PerfilNaoEncontrado = 2,
    EmailDuplicado = 3,
    DocumentoDuplicado = 4
}

public sealed record UsuarioCreateResult(
    UsuarioCreateStatus Status,
    UsuarioReadModel? Usuario = null);
