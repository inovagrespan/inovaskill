using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Infrastructure.Database;
using InovaSkillGrespan.Infrastructure.Python;
using InovaSkillGrespan.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InovaSkillGrespan.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PythonEngineOptions>(
            configuration.GetSection(PythonEngineOptions.SectionName));
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.AddSingleton<IDatabaseConnectionValidator, SqlServerDatabaseConnectionValidator>();
        services.AddSingleton<IUploadedFileStorage, LocalUploadedFileStorage>();
        services.AddSingleton<IImportEngine, PythonImportEngine>();

        return services;
    }
}
