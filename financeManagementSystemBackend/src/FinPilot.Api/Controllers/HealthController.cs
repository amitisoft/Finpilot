using FinPilot.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

public sealed class HealthController : BaseApiController
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new
        {
            Status = "Healthy",
            Service = "FinPilot.Api",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        return Success<object>(payload, "Health check completed");
    }
}
