using System.Text.Json;
using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Domain.ValueObjects;

namespace InovaSkillGrespan.Application.Imports;

public sealed class ProcessImportFileUseCase(
    IUploadedFileStorage storage,
    IImportEngine importEngine)
{
    public async Task<JsonElement> ExecuteAsync(
        ProcessImportFileCommand command,
        CancellationToken cancellationToken)
    {
        ValidateConfirmations(command.ConfirmationsJson);

        var spreadsheet = SpreadsheetFile.Create(
            command.OriginalFileName,
            command.SizeInBytes);

        var storedFile = await storage.SaveAsync(
            spreadsheet,
            command.Content,
            cancellationToken);

        return await importEngine.ProcessAsync(
            storedFile,
            command.ConfirmationsJson,
            cancellationToken);
    }

    private static void ValidateConfirmations(string? confirmationsJson)
    {
        if (string.IsNullOrWhiteSpace(confirmationsJson))
        {
            return;
        }

        using var document = JsonDocument.Parse(confirmationsJson);
        if (document.RootElement.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Confirmations must be a JSON object.");
        }
    }
}
