namespace FinPilot.Infrastructure.Agents;

internal interface IAgentJobQueue
{
    ValueTask QueueAsync(AgentWorkItem item, CancellationToken cancellationToken = default);
    ValueTask<AgentWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
