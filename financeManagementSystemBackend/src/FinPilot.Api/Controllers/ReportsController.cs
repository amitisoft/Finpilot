using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Reports;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class ReportsController(
    IReportsService reportsService,
    IReportExportService reportExportService,
    ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ReportTrendPointResponse>>>> GetTrends([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var userId = EnsureUser();
        var items = await reportsService.GetTrendsAsync(userId, months, cancellationToken);
        return Success(items, "Reporting trend fetched successfully");
    }

    [HttpGet("net-worth")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NetWorthPointResponse>>>> GetNetWorth([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var userId = EnsureUser();
        var items = await reportsService.GetNetWorthAsync(userId, months, cancellationToken);
        return Success(items, "Net worth report fetched successfully");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> DownloadPdf(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var (content, fileName) = await reportExportService.GenerateFinancialReportPdfAsync(userId, cancellationToken);
        return File(content, "application/pdf", fileName);
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}