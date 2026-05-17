namespace InovaSkillGrespan.Application.Abstractions.Imports;

public sealed record ImportJobCreateRequest(
    Guid Id,
    Guid TemplateVersionId,
    string FileName,
    string FilePath,
    Guid RequestedBy,
    string Status,
    string CorrelationId,
    DateTimeOffset CreatedAt);

public sealed record ImportJobStatusDto(
    Guid Id,
    string Status,
    string? SummaryJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string CorrelationId);

public sealed record ImportJobErrorDto(
    long Id,
    int? RowNumber,
    string? ColumnName,
    string ErrorCode,
    string ErrorMessage,
    string? RawValue,
    string Severity,
    DateTimeOffset CreatedAt);

