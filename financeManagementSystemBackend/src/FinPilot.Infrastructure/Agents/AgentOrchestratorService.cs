using System.Globalization;
using FinPilot.Application.DTOs.Agents;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinPilot.Infrastructure.Agents;

internal sealed class AgentOrchestratorService(FinPilotDbContext dbContext, IServiceScopeFactory scopeFactory, ILogger<AgentOrchestratorService> logger) : IAgentOrchestratorService
{
    public Task QueueTransactionAnomalyCheckAsync(Guid userId, Guid transactionId, AgentTrigger trigger, CancellationToken cancellationToken = default)
        => QueueAsync(userId, AgentType.Anomaly, trigger, "transaction", transactionId, cancellationToken);

    public Task QueueBudgetCheckAsync(Guid userId, Guid budgetId, AgentTrigger trigger, CancellationToken cancellationToken = default)
        => QueueAsync(userId, AgentType.Budget, trigger, "budget", budgetId, cancellationToken);

    public async Task QueueBudgetChecksForPeriodAsync(Guid userId, int month, int year, AgentTrigger trigger, CancellationToken cancellationToken = default)
    {
        var budgetIds = await dbContext.Budgets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Month == month && x.Year == year)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var budgetId in budgetIds)
        {
            await QueueBudgetCheckAsync(userId, budgetId, trigger, cancellationToken);
        }
    }

    public async Task<AgentInvocationResponse> InvokeAsync(Guid userId, InvokeAgentRequest request, CancellationToken cancellationToken = default)
    {
        return request.Agent switch
        {
            AgentType.Anomaly => await InvokeAnomalyAsync(userId, request, cancellationToken),
            AgentType.Budget => await InvokeBudgetAsync(userId, request, cancellationToken),
            AgentType.Coach => await InvokeCoachAsync(userId, request.Trigger, cancellationToken),
            AgentType.Investment => await InvokeInvestmentAsync(userId, request, cancellationToken),
            AgentType.Report => await InvokeReportAsync(userId, request.Trigger, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported agent type.")
        };
    }

    public async Task<AgentChatResponse> ChatAsync(Guid userId, AgentChatRequest request, CancellationToken cancellationToken = default)
    {
        var message = SanitizeMessage(request.Message);
        var enrichedRequest = await EnrichChatRequestAsync(userId, request, message, cancellationToken);
        var agent = ResolveAgent(message, enrichedRequest);

        return agent switch
        {
            AgentType.Anomaly => await BuildAnomalyChatResponseAsync(userId, enrichedRequest, message, cancellationToken),
            AgentType.Budget => await BuildBudgetChatResponseAsync(userId, enrichedRequest, message, cancellationToken),
            AgentType.Investment => await BuildInvestmentChatResponseAsync(userId, enrichedRequest, message, cancellationToken),
            AgentType.Report => await BuildReportChatResponseAsync(userId, message, cancellationToken),
            _ => await BuildCoachChatResponseAsync(userId, enrichedRequest, message, cancellationToken)
        };
    }

    private async Task<AgentInvocationResponse> InvokeAnomalyAsync(Guid userId, InvokeAgentRequest request, CancellationToken cancellationToken)
    {
        var transactionId = request.TransactionId ?? throw new InvalidOperationException("TransactionId is required for anomaly invocation.");
        var cached = await GetCachedAsync(userId, AgentType.Anomaly, "transaction", transactionId, cancellationToken);
        if (cached is not null)
        {
            return ToInvocationResponse(AgentType.Anomaly, true, cached);
        }

        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteAnomalyAsync(userId, transactionId, request.Trigger, null, cancellationToken);
        return CreateInvocationResponse(AgentType.Anomaly, false, result);
    }

    private async Task<AgentInvocationResponse> InvokeBudgetAsync(Guid userId, InvokeAgentRequest request, CancellationToken cancellationToken)
    {
        var budgetId = request.BudgetId ?? throw new InvalidOperationException("BudgetId is required for budget invocation.");
        var cached = await GetCachedAsync(userId, AgentType.Budget, "budget", budgetId, cancellationToken);
        if (cached is not null)
        {
            return ToInvocationResponse(AgentType.Budget, true, cached);
        }

        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteBudgetAsync(userId, budgetId, request.Trigger, null, cancellationToken);
        return CreateInvocationResponse(AgentType.Budget, false, result);
    }

    private async Task<AgentInvocationResponse> InvokeCoachAsync(Guid userId, AgentTrigger trigger, CancellationToken cancellationToken)
    {
        var cached = await GetCachedAsync(userId, AgentType.Coach, "coach", null, cancellationToken);
        if (cached is not null)
        {
            return ToInvocationResponse(AgentType.Coach, true, cached);
        }

        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteCoachAsync(userId, trigger, null, null, cancellationToken);
        return CreateInvocationResponse(AgentType.Coach, false, result);
    }

    private async Task<AgentInvocationResponse> InvokeInvestmentAsync(Guid userId, InvokeAgentRequest request, CancellationToken cancellationToken)
    {
        var cached = await GetCachedAsync(userId, AgentType.Investment, "investment", null, cancellationToken);
        if (cached is not null && string.IsNullOrWhiteSpace(request.RiskProfile) && !request.Age.HasValue)
        {
            return ToInvocationResponse(AgentType.Investment, true, cached);
        }

        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteInvestmentAsync(userId, request.Trigger, null, request.RiskProfile, request.Age, cancellationToken);
        return CreateInvocationResponse(AgentType.Investment, false, result);
    }

    private async Task<AgentInvocationResponse> InvokeReportAsync(Guid userId, AgentTrigger trigger, CancellationToken cancellationToken)
    {
        var cached = await GetCachedAsync(userId, AgentType.Report, "report", null, cancellationToken);
        if (cached is not null)
        {
            return ToInvocationResponse(AgentType.Report, true, cached);
        }

        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteReportAsync(userId, trigger, null, cancellationToken);
        return CreateInvocationResponse(AgentType.Report, false, result);
    }

    private async Task<AgentResult?> GetCachedAsync(Guid userId, AgentType agentType, string sourceEntityName, Guid? sourceEntityId, CancellationToken cancellationToken)
    {
        return await dbContext.AgentResults
            .AsNoTracking()
            .Where(x => x.UserId == userId
                && x.AgentType == agentType
                && x.Status == AgentExecutionStatus.Completed
                && x.SourceEntityName == sourceEntityName
                && x.SourceEntityId == sourceEntityId
                && !x.IsDismissed
                && (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private AgentInvocationResponse ToInvocationResponse(AgentType agent, bool cached, AgentResult result)
        => new()
        {
            Agent = agent,
            Cached = cached,
            GeneratedAt = result.GeneratedAt,
            Disclaimer = ResolveDisclaimer(agent),
            Result = AgentResultMappings.ToResponse(result)
        };

    private AgentInvocationResponse CreateInvocationResponse(AgentType agent, bool cached, AgentResultResponse result)
        => new()
        {
            Agent = agent,
            Cached = cached,
            GeneratedAt = result.GeneratedAt,
            Disclaimer = ResolveDisclaimer(agent),
            Result = result
        };

    private async Task QueueAsync(Guid userId, AgentType agentType, AgentTrigger trigger, string sourceEntityName, Guid sourceEntityId, CancellationToken cancellationToken)
    {
        var queued = new AgentResult
        {
            UserId = userId,
            AgentType = agentType,
            Trigger = trigger,
            Status = AgentExecutionStatus.Queued,
            Severity = AgentSeverity.None,
            SourceEntityName = sourceEntityName,
            SourceEntityId = sourceEntityId,
            Summary = $"Queued for {agentType.ToString().ToLowerInvariant()} screening.",
            GeneratedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        dbContext.AgentResults.Add(queued);
        await dbContext.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
                switch (agentType)
                {
                    case AgentType.Anomaly:
                        await executor.ExecuteAnomalyAsync(userId, sourceEntityId, trigger, queued.Id, CancellationToken.None);
                        break;
                    case AgentType.Budget:
                        await executor.ExecuteBudgetAsync(userId, sourceEntityId, trigger, queued.Id, CancellationToken.None);
                        break;
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Queued {AgentType} screening failed for source {SourceEntityName} {SourceEntityId}", agentType, sourceEntityName, sourceEntityId);

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
                    await executor.MarkFailedAsync(queued.Id, exception.Message, CancellationToken.None);
                }
                catch (Exception markFailedException)
                {
                    logger.LogError(markFailedException, "Unable to persist agent failure state for queued result {ResultId}", queued.Id);
                }
            }
        });
    }

    private static string SanitizeMessage(string message)
    {
        var sanitized = (message ?? string.Empty).Trim();
        if (sanitized.Length > 500)
        {
            sanitized = sanitized[..500];
        }

        var lowered = sanitized.ToLowerInvariant();
        var blockedPatterns = new[] { "ignore previous instructions", "system:", "assistant:", "<|" };
        if (blockedPatterns.Any(lowered.Contains))
        {
            throw new InvalidOperationException("Invalid chat input detected.");
        }

        return sanitized;
    }

    private async Task<AgentChatRequest> EnrichChatRequestAsync(Guid userId, AgentChatRequest request, string message, CancellationToken cancellationToken)
    {
        var transactionId = request.TransactionId;
        var budgetId = request.BudgetId;

        if (!budgetId.HasValue && ContainsAny(message, "budget", "overspend", "over budget", "safe to spend", "limit"))
        {
            budgetId = await dbContext.Budgets
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!transactionId.HasValue && ContainsAny(message, "fraud", "unusual", "suspicious", "duplicate", "merchant", "anomaly"))
        {
            transactionId = await dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AgentChatRequest
        {
            Message = request.Message,
            TransactionId = transactionId,
            BudgetId = budgetId,
            RiskProfile = request.RiskProfile,
            Age = request.Age,
            ConversationHistory = request.ConversationHistory
        };
    }

    private static AgentType ResolveAgent(string message, AgentChatRequest request)
    {
        if (ContainsAny(message, "invest", "investment", "allocate", "retirement", "index fund", "surplus"))
        {
            return AgentType.Investment;
        }

        if (ContainsAny(message, "report", "summary", "monthly review", "digest", "forecast"))
        {
            return AgentType.Report;
        }

        if (ContainsAny(message, "fraud", "unusual", "suspicious", "duplicate", "merchant", "anomaly") || request.TransactionId.HasValue)
        {
            return AgentType.Anomaly;
        }

        if (ContainsAny(message, "budget", "overspend", "over budget", "safe to spend", "limit") || request.BudgetId.HasValue)
        {
            return AgentType.Budget;
        }

        return AgentType.Coach;
    }

    private async Task<AgentChatResponse> BuildCoachChatResponseAsync(Guid userId, AgentChatRequest request, string message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IAgentExecutionService>();
        var result = await executor.ExecuteCoachAsync(userId, AgentTrigger.OnDemand, null, message, cancellationToken);
        var coach = result.Coach ?? throw new InvalidOperationException("Coach analysis was not generated.");

        var topPattern = coach.BehavioralPatterns.FirstOrDefault();
        var primarySuggestion = coach.Suggestions.FirstOrDefault();
        var secondarySuggestion = coach.Suggestions.Skip(1).FirstOrDefault();
        var lowered = message.ToLowerInvariant();
        var lead = BuildConversationLeadIn(request, "Here is the clearest read on it.", "Building on that, here is the next layer I'd focus on.");
        var moneySnapshot = coach.TotalIncome > 0
            ? $"This month you have {FormatMoney(coach.TotalIncome)} coming in, {FormatMoney(coach.TotalExpenses)} going out, and a net of {FormatMoney(coach.NetAmount)}."
            : "I still don't see enough income data to measure your true savings rate confidently.";
        var actionSentence = ToRecommendationSentence(primarySuggestion);
        var secondarySentence = secondarySuggestion is null ? string.Empty : $"After that, {LowercaseFirst(secondarySuggestion.Action)}";
        var impactSentence = ToImpactSentence(primarySuggestion);

        string reply;
        IReadOnlyCollection<string> followUps;

        if (ContainsAny(lowered, "food", "grocery", "restaurant", "dining"))
        {
            var categorySentence = coach.TopCategoryName is not null && coach.TopCategoryName.Contains("food", StringComparison.OrdinalIgnoreCase)
                ? $"Food is currently your biggest expense bucket at {FormatMoney(coach.TopCategoryAmount)}, which is about {FormatPercent(coach.TopCategoryPercentage)} of expense spend."
                : coach.TopCategoryName is not null
                    ? $"Your biggest expense bucket right now is {coach.TopCategoryName} at {FormatMoney(coach.TopCategoryAmount)}, so food may not be the only place to tighten."
                    : "I need a little more category history before I can isolate the food pattern cleanly.";

            reply = $"{lead} {categorySentence} {moneySnapshot} {actionSentence} {impactSentence} {secondarySentence} {coach.Encouragement}".Trim();
            followUps =
            [
                "Break down my food spend further",
                "How much should I cap food per week?",
                "What category should I cut after food?"
            ];
        }
        else if (ContainsAny(lowered, "save", "savings", "goal", "emergency", "fund"))
        {
            var goalSentence = !string.IsNullOrWhiteSpace(coach.ActiveGoalName)
                ? $"Your leading goal right now is {coach.ActiveGoalName}, and it is {FormatPercent(coach.ActiveGoalProgressPercent)} funded."
                : "You do not have an active savings goal with progress yet, so the first win is creating one and funding it automatically.";

            reply = $"{lead} {moneySnapshot} {goalSentence} {actionSentence} {impactSentence} {secondarySentence} Your current financial health score is {coach.HealthScore}/100, so even one automated move this month would make the plan feel more stable.".Trim();
            followUps =
            [
                "Which goal should I prioritize?",
                "How much can I move into savings this month?",
                "Help me build an emergency fund plan"
            ];
        }
        else if (ContainsAny(lowered, "spend", "expense", "cut", "reduce", "overspend"))
        {
            var pressureSentence = coach.TopCategoryName is not null
                ? $"The biggest pressure point I see is {coach.TopCategoryName} at {FormatMoney(coach.TopCategoryAmount)} or roughly {FormatPercent(coach.TopCategoryPercentage)} of your tracked expenses."
                : topPattern?.Description ?? "One expense pattern is putting more pressure on the month than it should.";
            var budgetSentence = coach.AtRiskBudgetCount > 0
                ? $"You also have {coach.AtRiskBudgetCount} budget area(s) close to the line, so cutting the next discretionary purchase matters more than usual."
                : string.Empty;

            reply = $"{lead} {pressureSentence} {budgetSentence} {actionSentence} {impactSentence} {secondarySentence}".Trim();
            followUps =
            [
                "Show my biggest spending driver",
                "What should I reduce this week?",
                "How can I avoid overspending again next month?"
            ];
        }
        else if (ContainsAny(lowered, "income", "salary", "surplus", "cashflow"))
        {
            var cashflowSentence = coach.TotalIncome > 0
                ? $"Your savings rate is sitting around {FormatPercent(coach.SavingsRatePercent)}, with a net monthly position of {FormatMoney(coach.NetAmount)}."
                : "Right now the blocker is missing income entries, so the app cannot tell whether your cashflow is truly healthy or just incomplete.";

            reply = $"{lead} {cashflowSentence} {actionSentence} {impactSentence} {secondarySentence} {coach.Encouragement}".Trim();
            followUps =
            [
                "How healthy is my monthly cashflow?",
                "What should I do with surplus income?",
                "How can I increase my savings rate?"
            ];
        }
        else
        {
            var patternSentence = topPattern?.Description ?? coach.Encouragement;
            reply = $"{lead} {patternSentence} {moneySnapshot} {actionSentence} {impactSentence} {secondarySentence} Overall financial health is {coach.HealthScore}/100.".Trim();
            followUps =
            [
                "What should I focus on this week?",
                "How can I improve my savings rate?",
                "Show my most risky spending pattern"
            ];
        }

        return new AgentChatResponse
        {
            AgentUsed = AgentType.Coach,
            Reply = NormalizeSpacing(reply),
            FollowUpSuggestions = followUps,
            GeneratedAt = result.GeneratedAt
        };
    }

    private async Task<AgentChatResponse> BuildBudgetChatResponseAsync(Guid userId, AgentChatRequest request, string message, CancellationToken cancellationToken)
    {
        if (!request.BudgetId.HasValue)
        {
            return new AgentChatResponse
            {
                AgentUsed = AgentType.Budget,
                Reply = "I could not find a saved budget yet. Create a monthly budget first and then I can tell you whether you are over plan, what is safe to spend, and which categories need attention.",
                FollowUpSuggestions =
                [
                    "Help me create my first budget",
                    "What categories should I include in a budget?",
                    "How much should I allocate to essentials?"
                ],
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        var response = await InvokeBudgetAsync(userId, new InvokeAgentRequest
        {
            Agent = AgentType.Budget,
            Trigger = AgentTrigger.OnDemand,
            BudgetId = request.BudgetId
        }, cancellationToken);

        var budget = response.Result.Budget ?? throw new InvalidOperationException("Budget analysis was not generated.");
        var topAction = budget.Recommendations.FirstOrDefault() ?? "Review the categories closest to their limit.";
        var topCategory = budget.OverrunCategories.FirstOrDefault();
        var lead = BuildConversationLeadIn(request, $"I checked {budget.BudgetName}.", $"Looking at {budget.BudgetName} again,");

        string reply = budget.Status switch
        {
            "over_budget" => $"{lead} you have spent {FormatMoney(budget.TotalSpent)} against a limit of {FormatMoney(budget.TotalLimit)}, so you are over by {FormatMoney(budget.TotalSpent - budget.TotalLimit)}. {(topCategory is null ? string.Empty : $"The sharpest pressure is in {topCategory.CategoryName}, where usage is already {FormatPercent(topCategory.UsagePercent)}. ")}My next move would be to {LowercaseFirst(topAction)}",
            "at_risk" => $"{lead} you are at {FormatPercent(budget.UsagePercent)} of the budget with {budget.DaysRemainingInMonth} day(s) left. {(topCategory is null ? string.Empty : $"{topCategory.CategoryName} is the category to watch first because it has only {FormatMoney(Math.Max(topCategory.RemainingAmount, 0))} left. ")}{topAction}",
            _ => $"{lead} you have used {FormatPercent(budget.UsagePercent)} of the limit, so you still have about {FormatMoney(Math.Max(budget.RemainingAmount, 0))} left for the month. {topAction}"
        };

        return new AgentChatResponse
        {
            AgentUsed = AgentType.Budget,
            Reply = NormalizeSpacing(reply),
            FollowUpSuggestions =
            [
                "Show me categories that are at risk",
                "How much is safe to spend this month?",
                "Which budget needs attention first?"
            ],
            GeneratedAt = response.GeneratedAt
        };
    }

    private async Task<AgentChatResponse> BuildAnomalyChatResponseAsync(Guid userId, AgentChatRequest request, string message, CancellationToken cancellationToken)
    {
        if (!request.TransactionId.HasValue)
        {
            return new AgentChatResponse
            {
                AgentUsed = AgentType.Anomaly,
                Reply = "I do not have a recent transaction to inspect yet. Once you add transactions, I can explain what looks suspicious, duplicated, or unusual.",
                FollowUpSuggestions =
                [
                    "What makes a transaction look suspicious?",
                    "How does FinPilot detect anomalies?",
                    "Review my next transaction for risk"
                ],
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        var response = await InvokeAnomalyAsync(userId, new InvokeAgentRequest
        {
            Agent = AgentType.Anomaly,
            Trigger = AgentTrigger.OnDemand,
            TransactionId = request.TransactionId
        }, cancellationToken);

        var anomaly = response.Result.Anomaly ?? throw new InvalidOperationException("Anomaly analysis was not generated.");
        var topSignal = anomaly.Signals.FirstOrDefault();
        var lead = BuildConversationLeadIn(request, "I checked that transaction for anything unusual.", "I reviewed it again with the same risk lens.");
        var reply = anomaly.Severity switch
        {
            "high" => $"{lead} I would treat it as genuinely suspicious. The risk score is {anomaly.RiskScore}/100, and the strongest signal is {LowercaseFirst(topSignal ?? anomaly.Explanation)}. My recommendation is to verify it right away.",
            "medium" => $"{lead} it is not definitely fraudulent, but it does stand out. The risk score is {anomaly.RiskScore}/100, mainly because {LowercaseFirst(topSignal ?? anomaly.Explanation)}. I would verify it before ignoring it.",
            "low" => $"{lead} it only looks mildly unusual. The main signal is {LowercaseFirst(topSignal ?? anomaly.Explanation)}. I would keep an eye on it, but there is no strong alarm here yet.",
            _ => $"{lead} it looks consistent with your recent history, so there is no real anomaly signal at the moment."
        };

        return new AgentChatResponse
        {
            AgentUsed = AgentType.Anomaly,
            Reply = NormalizeSpacing(reply),
            FollowUpSuggestions =
            [
                "Show other recent unusual transactions",
                "Why was this merchant flagged?",
                "Should I monitor this transaction?"
            ],
            GeneratedAt = response.GeneratedAt
        };
    }

    private async Task<AgentChatResponse> BuildInvestmentChatResponseAsync(Guid userId, AgentChatRequest request, string message, CancellationToken cancellationToken)
    {
        var response = await InvokeInvestmentAsync(userId, new InvokeAgentRequest
        {
            Agent = AgentType.Investment,
            Trigger = AgentTrigger.OnDemand,
            RiskProfile = request.RiskProfile,
            Age = request.Age
        }, cancellationToken);

        var investment = response.Result.Investment ?? throw new InvalidOperationException("Investment analysis was not generated.");
        var topBuckets = investment.AllocationSuggestions.OrderByDescending(x => x.Percentage).Take(2).ToArray();
        var allocationSummary = topBuckets.Length == 0
            ? "liquidity first"
            : string.Join(" and ", topBuckets.Select(x => $"{x.Percentage}% in {x.Bucket}"));
        var priorityAction = investment.PriorityActions.FirstOrDefault() ?? "Review the plan once a quarter instead of reacting to short-term noise.";
        var lead = BuildConversationLeadIn(request, "Here is the practical version of the plan.", "If I keep the same assumptions, this is still the plan I'd use.");
        var surplusSentence = investment.MonthlySurplus > 0
            ? $"You currently have about {FormatMoney(investment.MonthlySurplus)} of monthly surplus to direct."
            : "Right now there is not much free monthly surplus, so liquidity should stay ahead of growth.";
        var reply = $"{lead} {surplusSentence} For a {investment.RiskProfile} profile, I would lean toward {allocationSummary}. {priorityAction} {investment.Disclaimer}";

        return new AgentChatResponse
        {
            AgentUsed = AgentType.Investment,
            Reply = NormalizeSpacing(reply),
            FollowUpSuggestions =
            [
                "How much surplus should I automate each month?",
                "Should I build emergency savings first?",
                "Give me a conservative allocation instead"
            ],
            GeneratedAt = response.GeneratedAt
        };
    }

    private async Task<AgentChatResponse> BuildReportChatResponseAsync(Guid userId, string message, CancellationToken cancellationToken)
    {
        var response = await InvokeReportAsync(userId, AgentTrigger.OnDemand, cancellationToken);
        var report = response.Result.Report ?? throw new InvalidOperationException("Report analysis was not generated.");
        var topHighlight = report.Highlights.FirstOrDefault();
        var cleanedHighlight = topHighlight is null ? null : LowercaseFirst(topHighlight).TrimEnd('.');
        var reply = cleanedHighlight is null
            ? $"{report.Summary} {report.Forecast}"
            : $"{report.Summary} One thing that stands out is {cleanedHighlight}. {report.Forecast}";

        return new AgentChatResponse
        {
            AgentUsed = AgentType.Report,
            Reply = NormalizeSpacing(reply),
            FollowUpSuggestions =
            [
                "Show the full markdown report",
                "What is my top spending category this month?",
                "How does this compare with recent months?"
            ],
            GeneratedAt = response.GeneratedAt
        };
    }

    private static string FormatMoney(decimal amount)
        => amount.ToString("#,##0.##", CultureInfo.InvariantCulture);

    private static string FormatPercent(decimal value)
        => $"{decimal.Round(value, 1):0.#}%";

    private static string BuildConversationLeadIn(AgentChatRequest request, string firstTurnLead, string followUpLead)
        => request.ConversationHistory.Count > 1 ? followUpLead : firstTurnLead;

    private static string ToRecommendationSentence(CoachSuggestionResponse? suggestion)
        => suggestion is null
            ? "The next move I would make is to review this week's spending before new discretionary purchases land."
            : $"The next move I would make is to {LowercaseFirst(suggestion.Action)}";

    private static string ToImpactSentence(CoachSuggestionResponse? suggestion)
        => suggestion is null || suggestion.ExpectedMonthlyImpact <= 0
            ? string.Empty
            : $"If you do that consistently, it could improve the month by roughly {FormatMoney(suggestion.ExpectedMonthlyImpact)}.";

    private static string LowercaseFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var lowered = char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
        return lowered.EndsWith('.') ? lowered : lowered + ".";
    }

    private static string NormalizeSpacing(string value)
        => string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool ContainsAny(string message, params string[] terms)
        => terms.Any(term => message.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static string ResolveDisclaimer(AgentType agent)
        => agent == AgentType.Investment
            ? "This is informational guidance only and not licensed investment advice."
            : "FinPilot provides informational guidance only and does not execute financial actions.";
}

