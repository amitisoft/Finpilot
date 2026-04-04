using FinPilot.Application.DTOs.Reports;

namespace FinPilot.Application.Interfaces.Reports;

public interface IReportsService
{
    Task<IReadOnlyCollection<ReportTrendPointResponse>> GetTrendsAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NetWorthPointResponse>> GetNetWorthAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default);
}