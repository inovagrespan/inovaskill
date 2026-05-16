namespace InovaSkillGrespan.Application.Imports;

public sealed record ProcessImportFileCommand(
    string OriginalFileName,
    long SizeInBytes,
    Stream Content,
    string? ConfirmationsJson);
