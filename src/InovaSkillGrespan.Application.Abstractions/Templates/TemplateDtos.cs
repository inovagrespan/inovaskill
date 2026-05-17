namespace InovaSkillGrespan.Application.Abstractions.Templates;

public sealed record TemplateSummaryDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    bool IsActive,
    string ConfigJson,
    DateTimeOffset CreatedAt);

public sealed record TemplateCreateRequest(
    string Name,
    string Type,
    string Status,
    Guid CreatedBy);

public sealed record TemplateVersionCreateRequest(
    Guid TemplateId,
    int VersionNumber,
    bool IsActive,
    string ConfigJson,
    Guid CreatedBy);

