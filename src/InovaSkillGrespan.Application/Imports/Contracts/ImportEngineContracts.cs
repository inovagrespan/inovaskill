namespace InovaSkillGrespan.Application.Imports.Contracts;

public sealed record ImportEngineRequest(
    string ContractVersion,
    Guid JobId,
    string CorrelationId,
    ImportEngineFilePayload File,
    ImportEngineTemplatePayload Template,
    ImportEngineOptionsPayload Options,
    IReadOnlyDictionary<string, string>? Confirmations);

public sealed record ImportEngineFilePayload(
    string Path,
    string Name);

public sealed record ImportEngineTemplatePayload(
    Guid TemplateId,
    Guid VersionId,
    string Type,
    int Version,
    object Config);

public sealed record ImportEngineOptionsPayload(
    bool DryRun,
    int MaxErrors,
    string OnRowError,
    string PersistenceMode);

public sealed record ImportEngineResponse(
    string ContractVersion,
    Guid JobId,
    string CorrelationId,
    string Status,
    ImportEngineSummaryPayload Summary,
    ImportEnginePreviewPayload? Preview,
    IReadOnlyList<ImportEngineIssuePayload> Warnings,
    IReadOnlyList<ImportEngineIssuePayload> Errors,
    IReadOnlyList<ImportEngineLogPayload> Logs);

public sealed record ImportEngineSummaryPayload(
    int TotalRows,
    int ImportedRows,
    int FailedRows,
    int WarningCount,
    int ErrorCount,
    long DurationMs);

public sealed record ImportEnginePreviewPayload(
    IReadOnlyList<string> DetectedColumns,
    IReadOnlyList<ImportEngineMappingPayload> Mapping,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> SampleRows);

public sealed record ImportEngineMappingPayload(
    string SourceColumn,
    string TargetField,
    decimal Score,
    string Strategy);

public sealed record ImportEngineIssuePayload(
    string Code,
    string Message,
    string Severity,
    int? RowNumber,
    string? ColumnName,
    string? RawValue);

public sealed record ImportEngineLogPayload(
    string Stage,
    string Level,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Details);

