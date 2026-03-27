using System.Text.Json;
using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;

namespace FinPilot.Infrastructure.Agents;

internal static class AgentResultMappings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AgentResultResponse ToResponse(AgentResult entity)
    {
        return new AgentResultResponse
        {
            Id = entity.Id,
            Agent = entity.AgentType,
            Trigger = entity.Trigger,
            Status = entity.Status.ToString().ToLowerInvariant(),
            Severity = entity.Severity.ToString().ToLowerInvariant(),
            SourceEntityName = entity.SourceEntityName,
            SourceEntityId = entity.SourceEntityId,
            Summary = entity.Summary,
            ErrorMessage = entity.ErrorMessage,
            IsDismissed = entity.IsDismissed,
            GeneratedAt = entity.GeneratedAt,
            ExpiresAt = entity.ExpiresAt,
            Anomaly = Deserialize<AnomalyAnalysisResponse>(entity, AgentType.Anomaly),
            Budget = Deserialize<BudgetAdvisorAnalysisResponse>(entity, AgentType.Budget),
            Coach = Deserialize<CoachAnalysisResponse>(entity, AgentType.Coach),
            Investment = Deserialize<InvestmentAdvisorAnalysisResponse>(entity, AgentType.Investment),
            Report = Deserialize<ReportGeneratorAnalysisResponse>(entity, AgentType.Report)
        };
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static T? Deserialize<T>(AgentResult entity, AgentType expectedAgentType)
    {
        if (entity.AgentType != expectedAgentType || string.IsNullOrWhiteSpace(entity.ResultJson))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(entity.ResultJson, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
