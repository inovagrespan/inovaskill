using System.Text.Json;

namespace InovaSkillGrespan.Application.Abstractions;

public interface IImportEngine
{
    Task<JsonElement> ProcessAsync(
        StoredUploadedFile file,
        string? confirmationsJson,
        CancellationToken cancellationToken);
}
