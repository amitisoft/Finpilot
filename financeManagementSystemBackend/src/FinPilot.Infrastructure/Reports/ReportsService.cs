using FinPilot.Application.DTOs.Reports;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Reports;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Reports;

public sealed class ReportsService(FinPilotDbContext dbContext, IDateTimeProvider dateTimeProvider) : IReportsService
{
    public async Task<IReadOnlyCollection<ReportTrendPointResponse>> GetTrendsAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 12);
        var start = new DateTimeOffset(new DateTime(dateTimeProvider.UtcNow.Year, dateTimeProvider.UtcNow.Month, 1), TimeSpan.Zero).AddMonths(-(months - 1));
        var end = start.AddMonths(months);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TransactionDate >= start && x.TransactionDate < end)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, months)
            .Select(offset =>
            {
                var pointDate = start.AddMonths(offset);
                var monthTransactions = transactions.Where(x => x.TransactionDate.Year == pointDate.Year && x.TransactionDate.Month == pointDate.Month).ToList();
                var income = monthTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
                var expense = monthTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

                return new ReportTrendPointResponse
                {
                    Year = pointDate.Year,
                    Month = pointDate.Month,
                    Label = pointDate.ToString("MMM yyyy"),
                    Income = income,
                    Expense = expense,
                    NetAmount = income - expense
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyCollection<NetWorthPointResponse>> GetNetWorthAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 12);
        var start = new DateTimeOffset(new DateTime(dateTimeProvider.UtcNow.Year, dateTimeProvider.UtcNow.Month, 1), TimeSpan.Zero).AddMonths(-(months - 1));
        var end = start.AddMonths(months);

        var openingBalance = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.OpeningBalance, cancellationToken);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TransactionDate < end)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, months)
            .Select(offset =>
            {
                var pointDate = start.AddMonths(offset);
                var monthEndExclusive = pointDate.AddMonths(1);
                var netWorth = openingBalance + transactions
                    .Where(x => x.TransactionDate < monthEndExclusive)
                    .Sum(x => x.Type == TransactionType.Income ? x.Amount : -x.Amount);

                return new NetWorthPointResponse
                {
                    Year = pointDate.Year,
                    Month = pointDate.Month,
                    Label = pointDate.ToString("MMM yyyy"),
                    NetWorth = decimal.Round(netWorth, 2)
                };
            })
            .ToList();
    }
}