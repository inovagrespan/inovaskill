using System.Text.Json;
using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Application.Abstractions.Imports;
using InovaSkillGrespan.Application.Abstractions.Templates;
using InovaSkillGrespan.Application.Imports.Contracts;
using InovaSkillGrespan.Domain.Exceptions;
using InovaSkillGrespan.Domain.ValueObjects;

namespace InovaSkillGrespan.Application.Imports;

public sealed class ProcessImportFileUseCase(
    IUploadedFileStorage storage,
    IImportEngine importEngine,
    ITemplateRepository templateRepository,
    IImportJobRepository importJobRepository)
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

        var templateVersion = await ResolveTemplateVersionAsync(command, cancellationToken);
        var jobId = Guid.NewGuid();
        var correlationId = string.IsNullOrWhiteSpace(command.CorrelationId)
            ? $"corr-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{jobId:N}"[..48]
            : command.CorrelationId!;

        await importJobRepository.CreateAsync(
            new ImportJobCreateRequest(
                Id: jobId,
                TemplateVersionId: templateVersion.Id,
                FileName: storedFile.OriginalName,
                FilePath: storedFile.AbsolutePath,
                RequestedBy: command.RequestedBy ?? Guid.Empty,
                Status: "Pending",
                CorrelationId: correlationId,
                CreatedAt: DateTimeOffset.UtcNow),
            cancellationToken);

        await importJobRepository.MarkRunningAsync(jobId, cancellationToken);

        var requestJson = BuildImportRequestJson(
            command,
            storedFile,
            templateVersion,
            jobId,
            correlationId);

        try
        {
            var result = await importEngine.ProcessAsync(
                storedFile,
                requestJson,
                cancellationToken);

            var summary = result.GetProperty("summary").Clone();
            var logs = ExtractArray(result, "logs");
            var errors = ExtractArray(result, "errors");

            await importJobRepository.MarkCompletedAsync(
                jobId,
                summary,
                logs,
                errors,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await importJobRepository.MarkFailedAsync(
                jobId,
                "IMPORT_FAILED",
                ex.Message,
                cancellationToken);
            throw;
        }
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

    private static string BuildImportRequestJson(
        ProcessImportFileCommand command,
        StoredUploadedFile storedFile,
        TemplateVersionDto templateVersion,
        Guid jobId,
        string correlationId)
    {
        var confirmations = ParseConfirmations(command.ConfirmationsJson);
        JsonElement config;
        using (var document = JsonDocument.Parse(templateVersion.ConfigJson))
        {
            config = document.RootElement.Clone();
        }

        var request = new ImportEngineRequest(
            ContractVersion: "1.0",
            JobId: jobId,
            CorrelationId: correlationId,
            File: new ImportEngineFilePayload(
                Path: storedFile.AbsolutePath,
                Name: storedFile.OriginalName),
            Template: new ImportEngineTemplatePayload(
                TemplateId: templateVersion.TemplateId,
                VersionId: templateVersion.Id,
                Type: command.TemplateType,
                Version: templateVersion.VersionNumber,
                Config: config),
            Options: new ImportEngineOptionsPayload(
                DryRun: command.DryRun,
                MaxErrors: command.MaxErrors,
                OnRowError: command.OnRowError,
                PersistenceMode: command.PersistenceMode),
            Confirmations: confirmations);

        return JsonSerializer.Serialize(request);
    }

    private async Task<TemplateVersionDto> ResolveTemplateVersionAsync(
        ProcessImportFileCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TemplateId.HasValue)
        {
            var versions = await templateRepository.ListTemplateVersionsAsync(
                command.TemplateId.Value,
                cancellationToken);

            if (command.TemplateVersionId.HasValue)
            {
                var selected = versions.FirstOrDefault(x => x.Id == command.TemplateVersionId.Value);
                if (selected is not null)
                {
                    return selected;
                }
            }

            var activeByTemplate = versions.FirstOrDefault(x => x.IsActive)
                ?? versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();

            if (activeByTemplate is not null)
            {
                return activeByTemplate;
            }
        }

        var active = await templateRepository.GetActiveVersionAsync(command.TemplateType, cancellationToken);
        if (active is null)
        {
            throw new DomainException($"Nao existe template ativo para o tipo '{command.TemplateType}'.");
        }

        return active;
    }

    private static IReadOnlyDictionary<string, string>? ParseConfirmations(string? confirmationsJson)
    {
        if (string.IsNullOrWhiteSpace(confirmationsJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(confirmationsJson);
        var root = document.RootElement;
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return dictionary;
    }

    private static IReadOnlyList<JsonElement> ExtractArray(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<JsonElement>();
        foreach (var item in array.EnumerateArray())
        {
            items.Add(item.Clone());
        }

        return items;
    }
}
