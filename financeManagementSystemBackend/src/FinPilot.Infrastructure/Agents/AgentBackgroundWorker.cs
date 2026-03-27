using FinPilot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinPilot.Infrastructure.Agents;

internal sealed class AgentBackgroundWorker(IServiceScopeFactory scopeFactory, IAgentJobQueue queue, ILogger<AgentBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            AgentWorkItem? workItem = null;

            try
            {
                workItem = await queue.DequeueAsync(stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();

                if (workItem.AgentType == AgentType.Anomaly && workItem.SourceEntityId.HasValue)
                {
                    await executor.ExecuteAnomalyAsync(workItem.UserId, workItem.SourceEntityId.Value, workItem.Trigger, workItem.ResultId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Agent background worker failed while processing a queued job.");

                if (workItem is not null)
                {
                    using var scope = scopeFactory.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
                    await executor.MarkFailedAsync(workItem.ResultId, exception.Message, CancellationToken.None);
                }
            }
        }
    }
}
