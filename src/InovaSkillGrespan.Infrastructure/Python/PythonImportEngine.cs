using System.Diagnostics;
using System.Text.Json;
using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Infrastructure.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InovaSkillGrespan.Infrastructure.Python;

public sealed class PythonImportEngine : IImportEngine
{
    private readonly IHostEnvironment _environment;
    private readonly PythonEngineOptions _options;

    public PythonImportEngine(
        IOptions<PythonEngineOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<JsonElement> ProcessAsync(
        StoredUploadedFile file,
        string requestJson,
        CancellationToken cancellationToken)
    {
        var scriptPath = ResolvePath(_options.ScriptPath);
        var workingDirectory = ResolvePath(_options.WorkingDirectory);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Script da engine Python nao encontrado.", scriptPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["INOVASKILL_DATABASE_PROVIDER"] =
            _options.DatabaseProvider;
        startInfo.Environment["INOVASKILL_DATABASE_CONNECTION_STRING"] =
            _options.DatabaseConnectionString;

        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(file.AbsolutePath);
        startInfo.ArgumentList.Add("--request-json");
        startInfo.ArgumentList.Add(requestJson);

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);

        using var process = Process.Start(startInfo)
            ?? throw new ExternalImportEngineException("Nao foi possivel iniciar o processo Python.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException("Tempo limite excedido ao processar o arquivo.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new ExternalImportEngineException(
                $"Engine Python retornou erro. ExitCode={process.ExitCode}. Detalhes: {stderr}");
        }

        using var document = JsonDocument.Parse(stdout);
        return document.RootElement.Clone();
    }

    private string ResolvePath(string path)
    {
        return Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(_environment.ContentRootPath, path));
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup after timeout.
        }
    }
}
