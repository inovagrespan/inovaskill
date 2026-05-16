namespace InovaSkillGrespan.Application.Abstractions;

public interface IDatabaseConnectionValidator
{
    Task<DatabaseConnectionStatus> ValidateAsync(CancellationToken cancellationToken);
}
