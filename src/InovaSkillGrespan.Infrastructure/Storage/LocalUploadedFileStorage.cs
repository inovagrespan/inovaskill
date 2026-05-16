using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Domain.Exceptions;
using InovaSkillGrespan.Domain.ValueObjects;
using InovaSkillGrespan.Infrastructure.Python;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InovaSkillGrespan.Infrastructure.Storage;

public sealed class LocalUploadedFileStorage : IUploadedFileStorage
{
    private readonly IHostEnvironment _environment;
    private readonly PythonEngineOptions _options;

    public LocalUploadedFileStorage(
        IOptions<PythonEngineOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<StoredUploadedFile> SaveAsync(
        SpreadsheetFile file,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (file.SizeInBytes > _options.MaxUploadBytes)
        {
            throw new DomainException(
                $"Arquivo excede o limite de {_options.MaxUploadBytes} bytes.");
        }

        var uploadDirectory = ResolvePath(_options.UploadDirectory);
        Directory.CreateDirectory(uploadDirectory);

        var safeFileName = $"{Guid.NewGuid():N}{file.Extension}";
        var uploadedPath = Path.Combine(uploadDirectory, safeFileName);

        await using var stream = File.Create(uploadedPath);
        await content.CopyToAsync(stream, cancellationToken);

        return new StoredUploadedFile(
            uploadedPath,
            file.OriginalName,
            file.Extension,
            file.SizeInBytes);
    }

    private string ResolvePath(string path)
    {
        return Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(_environment.ContentRootPath, path));
    }
}
