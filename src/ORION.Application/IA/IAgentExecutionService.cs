namespace ORION.Application.IA;

public interface IAgentExecutionService
{
    Task<AgentExecutionResult> ExecutarAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default);
}
