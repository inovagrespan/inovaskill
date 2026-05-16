using InovaSkillGrespan.Domain.Exceptions;

namespace InovaSkillGrespan.Domain.ValueObjects;

public sealed record SpreadsheetFile
{
    private static readonly HashSet<string> SupportedExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".csv",
        ".xlsx",
        ".xls"
    };

    private SpreadsheetFile(string originalName, string extension, long sizeInBytes)
    {
        OriginalName = originalName;
        Extension = extension;
        SizeInBytes = sizeInBytes;
    }

    public string OriginalName { get; }
    public string Extension { get; }
    public long SizeInBytes { get; }

    public static SpreadsheetFile Create(string originalName, long sizeInBytes)
    {
        if (sizeInBytes <= 0)
        {
            throw new DomainException("Arquivo vazio.");
        }

        var extension = Path.GetExtension(originalName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new DomainException("Tipo de arquivo nao suportado. Envie .csv, .xlsx ou .xls.");
        }

        return new SpreadsheetFile(originalName, extension, sizeInBytes);
    }
}
