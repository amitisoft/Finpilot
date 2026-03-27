using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Agents;

public sealed class AnomalyAgentService(FinPilotDbContext dbContext)
{
    public async Task<AnomalyAnalysisResponse> AnalyzeTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Transaction not found for anomaly analysis.");

        var historyStart = transaction.TransactionDate.AddDays(-90);
        var priorTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id != transactionId && x.TransactionDate >= historyStart && x.TransactionDate <= transaction.TransactionDate)
            .ToListAsync(cancellationToken);

        var sameTypeHistory = priorTransactions.Where(x => x.Type == transaction.Type).ToList();
        var sameCategoryHistory = sameTypeHistory.Where(x => x.CategoryId == transaction.CategoryId).ToList();
        var recentWindow = priorTransactions.Where(x => x.TransactionDate >= transaction.TransactionDate.AddDays(-7)).ToList();
        var recentMerchantMatches = !string.IsNullOrWhiteSpace(transaction.Merchant)
            ? recentWindow.Where(x => x.Type == transaction.Type && !string.IsNullOrWhiteSpace(x.Merchant) && string.Equals(x.Merchant!.Trim(), transaction.Merchant.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()
            : new List<FinPilot.Domain.Entities.Transaction>();
        var merchantHistory = !string.IsNullOrWhiteSpace(transaction.Merchant)
            ? sameTypeHistory.Where(x => !string.IsNullOrWhiteSpace(x.Merchant) && string.Equals(x.Merchant!.Trim(), transaction.Merchant.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()
            : new List<FinPilot.Domain.Entities.Transaction>();
        var exactAmountRecentMatches = recentWindow.Where(x => x.Type == transaction.Type && x.Amount == transaction.Amount).ToList();

        var userAverage = sameTypeHistory.Count == 0 ? 0m : sameTypeHistory.Average(x => x.Amount);
        var categoryAverage = sameCategoryHistory.Count == 0 ? 0m : sameCategoryHistory.Average(x => x.Amount);
        var signals = new List<string>();
        var riskScore = 0;
        var anomalyType = "none";

        var largeSpikeThreshold = new[]
        {
            userAverage > 0 ? userAverage * 3m : 0m,
            categoryAverage > 0 ? categoryAverage * 2.5m : 0m,
            transaction.Type == TransactionType.Expense ? 5000m : 10000m
        }.Max();

        if (transaction.Amount >= largeSpikeThreshold && largeSpikeThreshold > 0)
        {
            riskScore += 55;
            anomalyType = "amount_spike";
            signals.Add($"Amount {transaction.Amount:0.##} is well above your usual range for this type of transaction.");
        }

        if (!string.IsNullOrWhiteSpace(transaction.Merchant) && merchantHistory.Count == 0 && transaction.Amount >= Math.Max(userAverage * 1.5m, 1000m))
        {
            riskScore += 20;
            anomalyType = anomalyType == "none" ? "new_merchant" : anomalyType;
            signals.Add($"{transaction.Merchant.Trim()} is a new merchant for your recent history.");
        }

        if (recentMerchantMatches.Count >= 2)
        {
            riskScore += 15;
            anomalyType = anomalyType == "none" ? "merchant_velocity" : anomalyType;
            signals.Add($"{transaction.Merchant!.Trim()} has appeared {recentMerchantMatches.Count + 1} times in the last 7 days.");
        }

        if (exactAmountRecentMatches.Count >= 2)
        {
            riskScore += 15;
            anomalyType = anomalyType == "none" ? "repeat_amount" : anomalyType;
            signals.Add($"This exact amount has appeared {exactAmountRecentMatches.Count + 1} times in the last 7 days.");
        }

        riskScore = Math.Min(riskScore, 100);
        var severity = riskScore switch
        {
            >= 75 => AgentSeverity.High,
            >= 50 => AgentSeverity.Medium,
            >= 25 => AgentSeverity.Low,
            _ => AgentSeverity.None
        };

        if (severity == AgentSeverity.None)
        {
            signals.Add("This transaction is within your recent normal range.");
        }

        var explanation = severity == AgentSeverity.None
            ? "This transaction does not look unusual compared with your recent activity."
            : string.Join(" ", signals);

        return new AnomalyAnalysisResponse
        {
            TransactionId = transaction.Id,
            Severity = severity.ToString().ToLowerInvariant(),
            AnomalyType = anomalyType,
            RiskScore = riskScore,
            Explanation = explanation,
            RecommendedAction = severity >= AgentSeverity.Medium ? "verify" : severity == AgentSeverity.Low ? "monitor" : "none",
            FlagForReview = severity >= AgentSeverity.Medium,
            Signals = signals,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
