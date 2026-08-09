using ORION.Application.Mensagens;

namespace ORION.Application.IA;

public sealed record AgentExecutionRequest(
    Guid InteracaoId,
    Guid MensagemId);

public enum AgentExecutionStatus
{
    Sucesso = 1,
    InteracaoNaoEncontrada = 2,
    InteracaoInvalida = 3,
    MensagemNaoEncontrada = 4,
    ConversaIncompativel = 5,
    FalhaProvider = 6,
    FalhaPersistencia = 7
}

public sealed record AgentExecutionResult(
    AgentExecutionStatus Status,
    MensagemReadModel? MensagemResposta = null,
    string? Modelo = null);
