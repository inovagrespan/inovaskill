using InovaSkillGrespan.Application.Imports;
using Microsoft.AspNetCore.Mvc;

namespace InovaSkillGrespan.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController(ProcessImportFileUseCase useCase) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ImportAsync(
        [FromForm] IFormFile file,
        [FromForm] string? confirmations,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await useCase.ExecuteAsync(
            new ProcessImportFileCommand(
                file.FileName,
                file.Length,
                content,
                confirmations),
            cancellationToken);

        return Ok(result);
    }
}
