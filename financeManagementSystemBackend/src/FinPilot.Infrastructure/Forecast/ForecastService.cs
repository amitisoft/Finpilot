using FinPilot.Application.DTOs.Forecast;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Forecast;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Forecast;

public sealed class ForecastService(FinPilotDbContext dbContext, IDateTimeProvider dateTimeProvider) : IForecastService
{
    public async Task<MonthlyForecastResponse> GetMonthlyForecastAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(userId, cancellationToken);

        return new MonthlyForecastResponse
        {
            CurrentBalance = decimal.Round(snapshot.CurrentBalance, 2),
            ProjectedEndOfMonthBalance = decimal.Round(snapshot.CurrentBalance + snapshot.ProjectedRemainingNetAmount, 2),
            ProjectedMonthNetAmount = decimal.Round(snapshot.CurrentMonthNetAmount + snapshot.ProjectedRemainingNetAmount, 2),
            ProjectedRemainingNetAmount = decimal.Round(snapshot.ProjectedRemainingNetAmount, 2),
            AverageDailyNetAmount = decimal.Round(snapshot.AverageDailyNetAmount, 2),
            DaysTracked = snapshot.DaysTracked,
            DaysRemaining = snapshot.DaysRemaining,
            Confidence = snapshot.Confidence,
            Assumptions = snapshot.Assumptions,
            GeneratedAt = dateTimeProvider.UtcNow
        };
    }

    public async Task<IReadOnlyCollection<DailyForecastPointResponse>> GetDailyForecastAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(userId, cancellationToken);
        var points = new List<DailyForecastPointResponse>();
        var runningBalance = snapshot.OpeningBalance;

        for (var date = snapshot.MonthStartDate; date <= snapshot.MonthEndDate; date = date.AddDays(1))
        {
            var actualNet = snapshot.DailyActualNet.TryGetValue(date, out var net) ? net : 0m;
            var isProjected = date > snapshot.TodayDate;
            var dailyNet = isProjected ? snapshot.AverageDailyNetAmount : actualNet;
            runningBalance += dailyNet;

            points.Add(new DailyForecastPointResponse
            {
                Date = date,
                Label = date.ToString("dd MMM"),
                Balance = decimal.Round(runningBalance, 2),
                DailyNetAmount = decimal.Round(dailyNet, 2),
                IsProjected = isProjected
            });
        }

        return points;
    }

    private async Task<ForecastSnapshot> BuildSnapshotAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var todayDate = utcNow.UtcDateTime.Date;
        var monthStartDate = new DateTime(todayDate.Year, todayDate.Month, 1);
        var monthEndDate = monthStartDate.AddMonths(1).AddDays(-1);
        var monthStart = new DateTimeOffset(monthStartDate, TimeSpan.Zero);
        var nextMonthStart = monthStart.AddMonths(1);

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var currentMonthTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TransactionDate >= monthStart && x.TransactionDate < nextMonthStart)
            .ToListAsync(cancellationToken);

        var recentWindowStartDate = todayDate.AddDays(-89);
        var recentWindowStart = new DateTimeOffset(recentWindowStartDate, TimeSpan.Zero);
        var recentTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TransactionDate >= recentWindowStart && x.TransactionDate < nextMonthStart)
            .ToListAsync(cancellationToken);

        var currentBalance = accounts.Sum(x => x.CurrentBalance);
        var currentMonthNetAmount = currentMonthTransactions.Sum(GetSignedAmount);
        var openingBalance = currentBalance - currentMonthNetAmount;

        var basisDays = Math.Max(1, (todayDate - recentWindowStartDate).Days + 1);
        var averageDailyNetAmount = recentTransactions.Count > 0
            ? recentTransactions.Sum(GetSignedAmount) / basisDays
            : currentMonthTransactions.Count > 0
                ? currentMonthNetAmount / Math.Max(todayDate.Day, 1)
                : 0m;

        var daysRemaining = Math.Max(0, monthEndDate.Day - todayDate.Day);
        var projectedRemainingNetAmount = averageDailyNetAmount * daysRemaining;
        var confidence = recentTransactions.Count switch
        {
            >= 15 when currentMonthTransactions.Count >= 5 => "high",
            >= 6 => "medium",
            _ => "low"
        };

        var assumptions = new List<string>
        {
            "Forecast extends the recent average daily net cashflow across the remaining days of the current month."
        };

        if (recentTransactions.Count == 0)
        {
            assumptions.Add("Very little history is available, so the projection mainly reflects the current month pace.");
        }

        if (!recentTransactions.Any(x => x.Type == TransactionType.Income))
        {
            assumptions.Add("Income history is incomplete, so salary timing may not be fully represented yet.");
        }

        if (currentMonthTransactions.Count < 3)
        {
            assumptions.Add("Only a few transactions have been recorded this month, so confidence remains conservative.");
        }

        var dailyActualNet = currentMonthTransactions
            .GroupBy(x => x.TransactionDate.UtcDateTime.Date)
            .ToDictionary(group => group.Key, group => group.Sum(GetSignedAmount));

        return new ForecastSnapshot
        {
            MonthStartDate = monthStartDate,
            MonthEndDate = monthEndDate,
            TodayDate = todayDate,
            CurrentBalance = currentBalance,
            OpeningBalance = openingBalance,
            CurrentMonthNetAmount = currentMonthNetAmount,
            AverageDailyNetAmount = averageDailyNetAmount,
            ProjectedRemainingNetAmount = projectedRemainingNetAmount,
            DaysTracked = Math.Max(todayDate.Day, 1),
            DaysRemaining = daysRemaining,
            Confidence = confidence,
            Assumptions = assumptions,
            DailyActualNet = dailyActualNet
        };
    }

    private static decimal GetSignedAmount(Domain.Entities.Transaction transaction)
        => transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;

    private sealed class ForecastSnapshot
    {
        public required DateTime MonthStartDate { get; init; }
        public required DateTime MonthEndDate { get; init; }
        public required DateTime TodayDate { get; init; }
        public required decimal CurrentBalance { get; init; }
        public required decimal OpeningBalance { get; init; }
        public required decimal CurrentMonthNetAmount { get; init; }
        public required decimal AverageDailyNetAmount { get; init; }
        public required decimal ProjectedRemainingNetAmount { get; init; }
        public required int DaysTracked { get; init; }
        public required int DaysRemaining { get; init; }
        public required string Confidence { get; init; }
        public required IReadOnlyCollection<string> Assumptions { get; init; }
        public required IReadOnlyDictionary<DateTime, decimal> DailyActualNet { get; init; }
    }
}