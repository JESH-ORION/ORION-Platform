namespace ORION.Application.Mensagens;

public enum MensagemCreateStatus
{
    Criada = 1,
    ConversaNaoEncontrada = 2,
    UsuarioNaoEncontrado = 3
}

public sealed record MensagemCreateResult(
    MensagemCreateStatus Status,
    MensagemReadModel? Mensagem = null);
