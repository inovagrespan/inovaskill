using InovaSkillGrespan.Domain.ValueObjects;

namespace InovaSkillGrespan.Application.Abstractions;

public interface IUploadedFileStorage
{
    Task<StoredUploadedFile> SaveAsync(
        SpreadsheetFile file,
        Stream content,
        CancellationToken cancellationToken);
}
