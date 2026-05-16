using InovaSkillGrespan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InovaSkillGrespan.Api.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseController(
    IDatabaseConnectionValidator databaseConnectionValidator) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HealthAsync(CancellationToken cancellationToken)
    {
        var status = await databaseConnectionValidator.ValidateAsync(cancellationToken);
        return Ok(status);
    }
}
