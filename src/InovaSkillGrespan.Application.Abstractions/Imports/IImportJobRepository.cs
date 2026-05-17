using System.Text.Json;

namespace InovaSkillGrespan.Application.Abstractions.Imports;

public interface IImportJobRepository
{
    Task CreateAsync(ImportJobCreateRequest request, CancellationToken cancellationToken);

    Task MarkRunningAsync(Guid jobId, CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        Guid jobId,
        JsonElement summary,
        IReadOnlyList<JsonElement> logs,
        IReadOnlyList<JsonElement> errors,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid jobId,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken);

    Task<ImportJobStatusDto?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ImportJobErrorDto>> ListErrorsAsync(Guid jobId, CancellationToken cancellationToken);
}

