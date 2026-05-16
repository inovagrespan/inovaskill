namespace InovaSkillGrespan.Infrastructure.Python;

public sealed class PythonEngineOptions
{
    public const string SectionName = "PythonEngine";

    public string PythonExecutable { get; set; } = "python";
    public string ScriptPath { get; set; } = "../../engines/EngineProcessData/tools/process_import.py";
    public string WorkingDirectory { get; set; } = "../../engines/EngineProcessData";
    public string UploadDirectory { get; set; } = "uploads";
    public string DatabaseProvider { get; set; } = "sqlserver";
    public string DatabaseConnectionString { get; set; } =
        "Driver={ODBC Driver 18 for SQL Server};Server=localhost;Database=InovaSkillGrespan;Trusted_Connection=yes;TrustServerCertificate=yes;";
    public int TimeoutSeconds { get; set; } = 120;
    public long MaxUploadBytes { get; set; } = 50 * 1024 * 1024;
}
