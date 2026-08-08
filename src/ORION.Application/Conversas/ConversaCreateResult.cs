namespace ORION.Application.Conversas;

public enum ConversaCreateStatus
{
    Criada = 1,
    PacienteNaoEncontrado = 2,
    ProfissionalNaoEncontrado = 3
}

public sealed record ConversaCreateResult(
    ConversaCreateStatus Status,
    ConversaReadModel? Conversa = null);
