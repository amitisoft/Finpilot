using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Agents;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinPilot.Api.Controllers;

[Authorize]
[EnableRateLimiting("agent")]
public sealed class AgentsController(IAgentOrchestratorService agentOrchestratorService, IAgentResultService agentResultService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpPost("invoke")]
    public async Task<ActionResult<ApiResponse<AgentInvocationResponse>>> Invoke(InvokeAgentRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var result = await agentOrchestratorService.InvokeAsync(userId, request, cancellationToken);
        return Success(result, "Agent invoked successfully");
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AgentChatResponse>>> Chat(AgentChatRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var result = await agentOrchestratorService.ChatAsync(userId, request, cancellationToken);
        return Success(result, "Agent chat response generated successfully");
    }

    [HttpGet("widgets/coach")]
    public async Task<ActionResult<ApiResponse<DashboardCoachWidgetResponse>>> GetCoachWidget(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var invocation = await agentOrchestratorService.InvokeAsync(userId, new InvokeAgentRequest
        {
            Agent = AgentType.Coach,
            Trigger = AgentTrigger.OnDemand
        }, cancellationToken);

        var coach = invocation.Result.Coach ?? throw new InvalidOperationException("Coach widget data is unavailable.");
        var primarySuggestion = coach.Suggestions.FirstOrDefault();
        var widget = new DashboardCoachWidgetResponse
        {
            HealthScore = coach.HealthScore,
            Headline = primarySuggestion is null
                ? "Your financial coach is ready with a fresh snapshot."
                : $"Coach focus: {primarySuggestion.Title}",
            Encouragement = coach.Encouragement,
            TopPatterns = coach.BehavioralPatterns.Select(x => x.Description).ToList(),
            PrimaryAction = primarySuggestion?.Action ?? "Keep tracking income, budgets, and goals for richer coaching.",
            EstimatedMonthlyImpact = primarySuggestion?.ExpectedMonthlyImpact ?? 0m,
            Disclaimer = invocation.Disclaimer,
            GeneratedAt = coach.GeneratedAt
        };

        return Success(widget, "Coach widget fetched successfully");
    }

    [HttpGet("widgets/report")]
    public async Task<ActionResult<ApiResponse<DashboardReportWidgetResponse>>> GetReportWidget(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var invocation = await agentOrchestratorService.InvokeAsync(userId, new InvokeAgentRequest
        {
            Agent = AgentType.Report,
            Trigger = AgentTrigger.OnDemand
        }, cancellationToken);

        var report = invocation.Result.Report ?? throw new InvalidOperationException("Report widget data is unavailable.");
        var widget = new DashboardReportWidgetResponse
        {
            Title = report.Title,
            Summary = report.Summary,
            Highlights = report.Highlights,
            Forecast = report.Forecast,
            Disclaimer = invocation.Disclaimer,
            GeneratedAt = report.GeneratedAt
        };

        return Success(widget, "Report widget fetched successfully");
    }

    [HttpGet("results")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AgentResultResponse>>>> GetResults([FromQuery] AgentType? agent, [FromQuery] Guid? sourceEntityId, [FromQuery] bool includeDismissed, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await agentResultService.GetResultsAsync(userId, agent, sourceEntityId, includeDismissed, cancellationToken);
        return Success(items, "Agent results fetched successfully");
    }

    [HttpGet("results/transactions/{transactionId:guid}")]
    public async Task<ActionResult<ApiResponse<AgentResultResponse>>> GetLatestForTransaction(Guid transactionId, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await agentResultService.GetLatestForTransactionAsync(userId, transactionId, cancellationToken);
        return item is null ? NotFound(ApiResponse<AgentResultResponse>.Fail("Agent result not found")) : Success(item, "Agent result fetched successfully");
    }

    [HttpPost("results/{id:guid}/dismiss")]
    public async Task<ActionResult<ApiResponse<object>>> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await agentResultService.DismissAsync(userId, id, cancellationToken);
        return Success<object>(null, "Agent result dismissed successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
