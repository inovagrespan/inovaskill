namespace InovaSkillGrespan.Application.Abstractions;

public sealed record StoredUploadedFile(
    string AbsolutePath,
    string OriginalName,
    string Extension,
    long SizeInBytes);
