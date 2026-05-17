using InovaSkillGrespan.Application.Imports;
using InovaSkillGrespan.Application.Abstractions.Imports;
using Microsoft.AspNetCore.Mvc;

namespace InovaSkillGrespan.Api.Controllers;

[ApiController]
[Route("api/imports")]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
public sealed class ImportsController(
    ProcessImportFileUseCase useCase,
    IImportJobRepository importJobRepository) : ControllerBase
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
        [FromForm] string? templateType,
        [FromForm] Guid? templateId,
        [FromForm] Guid? templateVersionId,
        [FromForm] int? templateVersionNumber,
        [FromForm] bool? dryRun,
        [FromForm] int? maxErrors,
        [FromForm] string? onRowError,
        [FromForm] string? persistenceMode,
        [FromForm] Guid? requestedBy,
        [FromForm] string? correlationId,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateType = string.IsNullOrWhiteSpace(templateType)
            ? "customers"
            : templateType;

        await using var content = file.OpenReadStream();
        var result = await useCase.ExecuteAsync(
            new ProcessImportFileCommand(
                file.FileName,
                file.Length,
                content,
                confirmations,
                normalizedTemplateType,
                templateId,
                templateVersionId,
                templateVersionNumber ?? 1,
                dryRun ?? false,
                maxErrors ?? 500,
                string.IsNullOrWhiteSpace(onRowError) ? "skip_row" : onRowError,
                string.IsNullOrWhiteSpace(persistenceMode) ? "upsert" : persistenceMode,
                requestedBy,
                correlationId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetStatusAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await importJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound(new { message = "Import job nao encontrado." });
        }

        return Ok(job);
    }

    [HttpGet("{jobId:guid}/errors")]
    public async Task<IActionResult> ListErrorsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var errors = await importJobRepository.ListErrorsAsync(jobId, cancellationToken);
        return Ok(errors);
    }
}
