using System.Threading.Channels;

namespace FinPilot.Infrastructure.Agents;

internal sealed class AgentBackgroundQueue : IAgentJobQueue
{
    private readonly Channel<AgentWorkItem> channel = Channel.CreateUnbounded<AgentWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask QueueAsync(AgentWorkItem item, CancellationToken cancellationToken = default)
        => channel.Writer.WriteAsync(item, cancellationToken);

    public ValueTask<AgentWorkItem> DequeueAsync(CancellationToken cancellationToken)
        => channel.Reader.ReadAsync(cancellationToken);
}
