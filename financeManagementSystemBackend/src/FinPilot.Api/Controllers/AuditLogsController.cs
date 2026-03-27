using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Audit;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
[Route("api/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AuditLogResponse>>>> GetRecent([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
        var items = await auditLogService.GetRecentAsync(userId, take, cancellationToken);
        return Success(items, "Audit logs fetched successfully");
    }
}
