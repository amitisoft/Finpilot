using FinPilot.Application.DTOs.Forecast;

namespace FinPilot.Application.Interfaces.Forecast;

public interface IForecastService
{
    Task<MonthlyForecastResponse> GetMonthlyForecastAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DailyForecastPointResponse>> GetDailyForecastAsync(Guid userId, CancellationToken cancellationToken = default);
}