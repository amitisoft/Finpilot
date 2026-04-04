using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Forecast;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Forecast;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class ForecastController(IForecastService forecastService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet("month")]
    public async Task<ActionResult<ApiResponse<MonthlyForecastResponse>>> GetMonth(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await forecastService.GetMonthlyForecastAsync(userId, cancellationToken);
        return Success(item, "Monthly forecast generated successfully");
    }

    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DailyForecastPointResponse>>>> GetDaily(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await forecastService.GetDailyForecastAsync(userId, cancellationToken);
        return Success(items, "Daily forecast generated successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}