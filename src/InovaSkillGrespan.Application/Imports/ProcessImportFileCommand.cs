namespace InovaSkillGrespan.Application.Imports;

public sealed record ProcessImportFileCommand(
    string OriginalFileName,
    long SizeInBytes,
    Stream Content,
    string? ConfirmationsJson,
    string TemplateType,
    Guid? TemplateId,
    Guid? TemplateVersionId,
    int TemplateVersionNumber,
    bool DryRun,
    int MaxErrors,
    string OnRowError,
    string PersistenceMode,
    Guid? RequestedBy,
    string? CorrelationId);
