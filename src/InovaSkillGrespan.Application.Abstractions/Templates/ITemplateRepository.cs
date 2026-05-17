namespace InovaSkillGrespan.Application.Abstractions.Templates;

public interface ITemplateRepository
{
    Task<Guid> CreateTemplateAsync(
        TemplateCreateRequest request,
        CancellationToken cancellationToken);

    Task<Guid> CreateTemplateVersionAsync(
        TemplateVersionCreateRequest request,
        CancellationToken cancellationToken);

    Task ActivateTemplateVersionAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TemplateSummaryDto>> ListTemplatesAsync(
        string? type,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TemplateVersionDto>> ListTemplateVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken);

    Task<TemplateVersionDto?> GetActiveVersionAsync(
        string templateType,
        CancellationToken cancellationToken);
}

