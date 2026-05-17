using InovaSkillGrespan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InovaSkillGrespan.Api.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseController(
    IDatabaseConnectionValidator databaseConnectionValidator,
    IDatabaseMetadataReader databaseMetadataReader) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HealthAsync(CancellationToken cancellationToken)
    {
        var status = await databaseConnectionValidator.ValidateAsync(cancellationToken);
        return Ok(status);
    }

    [HttpGet("tables")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTablesAsync(CancellationToken cancellationToken)
    {
        var tables = await databaseMetadataReader.ListTablesAsync(cancellationToken);
        return Ok(tables);
    }

    [HttpGet("tables/{tableName}/columns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListColumnsAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = await databaseMetadataReader.ListColumnsAsync(tableName, cancellationToken);
        return Ok(columns);
    }
}
