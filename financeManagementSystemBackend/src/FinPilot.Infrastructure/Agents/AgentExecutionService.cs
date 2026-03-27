using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Agents;

internal sealed class AgentExecutionService(
    FinPilotDbContext dbContext,
    AnomalyAgentService anomalyAgentService,
    BudgetAdvisorAgentService budgetAdvisorAgentService,
    FinancialCoachAgentService financialCoachAgentService,
    InvestmentAdvisorAgentService investmentAdvisorAgentService,
    ReportGeneratorAgentService reportGeneratorAgentService) : IAgentExecutionService
{
    public async Task<AgentResultResponse> ExecuteAnomalyAsync(Guid userId, Guid transactionId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default)
    {
        var analysis = await anomalyAgentService.AnalyzeTransactionAsync(userId, transactionId, cancellationToken);
        var entity = await LoadOrCreateAsync(existingResultId, userId, AgentType.Anomaly, trigger, "transaction", transactionId, cancellationToken);
        Apply(entity, trigger, ParseSeverity(analysis.Severity), analysis.Explanation, analysis.GeneratedAt, AgentResultMappings.Serialize(analysis));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AgentResultMappings.ToResponse(entity);
    }

    public async Task<AgentResultResponse> ExecuteBudgetAsync(Guid userId, Guid budgetId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default)
    {
        var analysis = await budgetAdvisorAgentService.AnalyzeBudgetAsync(userId, budgetId, cancellationToken);
        var severity = analysis.Status switch
        {
            "over_budget" => AgentSeverity.High,
            "at_risk" => AgentSeverity.Medium,
            _ => AgentSeverity.Low
        };
        var entity = await LoadOrCreateAsync(existingResultId, userId, AgentType.Budget, trigger, "budget", budgetId, cancellationToken);
        var summary = analysis.Status switch
        {
            "over_budget" => $"{analysis.BudgetName} is over budget by {analysis.TotalSpent - analysis.TotalLimit:0.##}.",
            "at_risk" => $"{analysis.BudgetName} is at {analysis.UsagePercent}% of its limit with {analysis.DaysRemainingInMonth} day(s) remaining.",
            _ => $"{analysis.BudgetName} is on track with {analysis.RemainingAmount:0.##} left to spend."
        };
        Apply(entity, trigger, severity, summary, analysis.GeneratedAt, AgentResultMappings.Serialize(analysis));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AgentResultMappings.ToResponse(entity);
    }

    public async Task<AgentResultResponse> ExecuteCoachAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, string? userQuestion = null, CancellationToken cancellationToken = default)
    {
        var analysis = await financialCoachAgentService.AnalyzeAsync(userId, userQuestion, cancellationToken);
        var entity = await LoadOrCreateAsync(existingResultId, userId, AgentType.Coach, trigger, "coach", null, cancellationToken);
        var summary = $"Financial coach scored this period at {analysis.HealthScore}/100 with {analysis.Suggestions.Count} active suggestion(s).";
        var severity = analysis.HealthScore switch
        {
            >= 60 => AgentSeverity.Low,
            >= 40 => AgentSeverity.Medium,
            _ => AgentSeverity.High
        };
        Apply(entity, trigger, severity, summary, analysis.GeneratedAt, AgentResultMappings.Serialize(analysis));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AgentResultMappings.ToResponse(entity);
    }

    public async Task<AgentResultResponse> ExecuteInvestmentAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, string? riskProfile = null, int? age = null, CancellationToken cancellationToken = default)
    {
        var analysis = await investmentAdvisorAgentService.AnalyzeAsync(userId, riskProfile, age, cancellationToken);
        var entity = await LoadOrCreateAsync(existingResultId, userId, AgentType.Investment, trigger, "investment", null, cancellationToken);
        var summary = analysis.MonthlySurplus > 0
            ? $"Investment guidance prepared for a {analysis.RiskProfile} profile with {analysis.MonthlySurplus:0.##} monthly surplus."
            : "Investment guidance prioritized liquidity because monthly surplus is limited.";
        var severity = analysis.MonthlySurplus > 0 ? AgentSeverity.Low : AgentSeverity.Medium;
        Apply(entity, trigger, severity, summary, analysis.GeneratedAt, AgentResultMappings.Serialize(analysis));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AgentResultMappings.ToResponse(entity);
    }

    public async Task<AgentResultResponse> ExecuteReportAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default)
    {
        var analysis = await reportGeneratorAgentService.AnalyzeAsync(userId, cancellationToken);
        var entity = await LoadOrCreateAsync(existingResultId, userId, AgentType.Report, trigger, "report", null, cancellationToken);
        Apply(entity, trigger, AgentSeverity.Low, analysis.Summary, analysis.GeneratedAt, AgentResultMappings.Serialize(analysis));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AgentResultMappings.ToResponse(entity);
    }

    public async Task MarkFailedAsync(Guid resultId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AgentResults.FirstOrDefaultAsync(x => x.Id == resultId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.Status = AgentExecutionStatus.Failed;
        entity.Severity = AgentSeverity.None;
        entity.Summary = "Agent execution failed.";
        entity.ErrorMessage = errorMessage;
        entity.ResultJson = null;
        entity.GeneratedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AgentResult> LoadOrCreateAsync(Guid? existingResultId, Guid userId, AgentType agentType, AgentTrigger trigger, string sourceEntityName, Guid? sourceEntityId, CancellationToken cancellationToken)
    {
        var entity = existingResultId.HasValue
            ? await dbContext.AgentResults.FirstOrDefaultAsync(x => x.Id == existingResultId.Value && x.UserId == userId, cancellationToken)
            : null;

        if (entity is not null)
        {
            return entity;
        }

        entity = new AgentResult
        {
            UserId = userId,
            AgentType = agentType,
            Trigger = trigger,
            SourceEntityName = sourceEntityName,
            SourceEntityId = sourceEntityId
        };
        dbContext.AgentResults.Add(entity);
        return entity;
    }

    private static void Apply(AgentResult entity, AgentTrigger trigger, AgentSeverity severity, string summary, DateTimeOffset generatedAt, string resultJson)
    {
        entity.Trigger = trigger;
        entity.Status = AgentExecutionStatus.Completed;
        entity.Severity = severity;
        entity.Summary = summary;
        entity.ResultJson = resultJson;
        entity.ErrorMessage = null;
        entity.GeneratedAt = generatedAt;
        entity.ExpiresAt = generatedAt.AddHours(24);
        entity.IsDismissed = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static AgentSeverity ParseSeverity(string severity)
        => Enum.TryParse<AgentSeverity>(severity, true, out var parsed) ? parsed : AgentSeverity.None;
}
