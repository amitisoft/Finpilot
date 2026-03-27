using FinPilot.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T? data, string message = "Operation completed")
        => Ok(ApiResponse<T>.Ok(data, message));
}
