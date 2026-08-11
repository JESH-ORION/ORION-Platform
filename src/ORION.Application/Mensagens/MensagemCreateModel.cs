namespace ORION.Application.Mensagens;

public sealed record MensagemCreateModel(
    Guid ConversaId,
    Guid? UsuarioId,
    string Tipo,
    string Conteudo);
