namespace InovaSkillGrespan.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } =
        "Server=localhost;Database=InovaSkillGrespan;Trusted_Connection=True;TrustServerCertificate=True;";
}
