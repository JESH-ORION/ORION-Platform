namespace ORION.Application.InteracoesIA;

public enum InteracaoIACreateStatus
{
    Criada = 1,
    AgenteNaoEncontrado = 2,
    ConversaNaoEncontrada = 3,
    PacienteNaoEncontrado = 4,
    UsuarioNaoEncontrado = 5
}

public sealed record InteracaoIACreateResult(
    InteracaoIACreateStatus Status,
    InteracaoIAReadModel? Interacao = null);
