using InovaSkillGrespan.Application.Abstractions.Templates;
using Microsoft.AspNetCore.Mvc;

namespace InovaSkillGrespan.Api.Controllers;

[ApiController]
[Route("api/templates")]
public sealed class TemplatesController(ITemplateRepository templateRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var templates = await templateRepository.ListTemplatesAsync(type, cancellationToken);
        return Ok(templates);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTemplateBody body,
        CancellationToken cancellationToken)
    {
        var id = await templateRepository.CreateTemplateAsync(
            new TemplateCreateRequest(
                body.Name,
                body.Type,
                body.Status,
                body.CreatedBy),
            cancellationToken);

        return Ok(new { templateId = id });
    }

    [HttpGet("{templateId:guid}/versions")]
    public async Task<IActionResult> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var versions = await templateRepository.ListTemplateVersionsAsync(templateId, cancellationToken);
        return Ok(versions);
    }

    [HttpPost("{templateId:guid}/versions")]
    public async Task<IActionResult> CreateVersionAsync(
        Guid templateId,
        [FromBody] CreateTemplateVersionBody body,
        CancellationToken cancellationToken)
    {
        var versionId = await templateRepository.CreateTemplateVersionAsync(
            new TemplateVersionCreateRequest(
                templateId,
                body.VersionNumber,
                body.IsActive,
                body.ConfigJson,
                body.CreatedBy),
            cancellationToken);
        return Ok(new { versionId });
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/activate")]
    public async Task<IActionResult> ActivateVersionAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        await templateRepository.ActivateTemplateVersionAsync(templateId, versionId, cancellationToken);
        return Ok(new { templateId, versionId, status = "active" });
    }

    [HttpPost("simple")]
    public async Task<IActionResult> CreateSimpleAsync(
        [FromBody] CreateSimpleTemplateBody body,
        CancellationToken cancellationToken)
    {
        var templateId = await templateRepository.CreateTemplateAsync(
            new TemplateCreateRequest(
                body.Name,
                body.Type,
                "active",
                body.CreatedBy),
            cancellationToken);

        await templateRepository.CreateTemplateVersionAsync(
            new TemplateVersionCreateRequest(
                templateId,
                1,
                true,
                body.ConfigJson,
                body.CreatedBy),
            cancellationToken);

        return Ok(new { templateId });
    }

    public sealed record CreateTemplateBody(
        string Name,
        string Type,
        string Status,
        Guid CreatedBy);

    public sealed record CreateTemplateVersionBody(
        int VersionNumber,
        bool IsActive,
        string ConfigJson,
        Guid CreatedBy);

    public sealed record CreateSimpleTemplateBody(
        string Name,
        string Type,
        string ConfigJson,
        Guid CreatedBy);
}
