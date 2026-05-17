using InovaSkillGrespan.Application.Abstractions;
using InovaSkillGrespan.Application.Abstractions.Imports;
using InovaSkillGrespan.Application.Abstractions.Templates;
using InovaSkillGrespan.Infrastructure.Database;
using InovaSkillGrespan.Infrastructure.Imports;
using InovaSkillGrespan.Infrastructure.Persistence;
using InovaSkillGrespan.Infrastructure.Python;
using InovaSkillGrespan.Infrastructure.Storage;
using InovaSkillGrespan.Infrastructure.Templates;
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
        services.AddSingleton<IDatabaseMetadataReader, SqlServerDatabaseMetadataReader>();
        services.AddSingleton<IUploadedFileStorage, LocalUploadedFileStorage>();
        services.AddSingleton<IImportEngine, PythonImportEngine>();
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<ITemplateRepository, SqlTemplateRepository>();
        services.AddSingleton<IImportJobRepository, SqlImportJobRepository>();

        return services;
    }
}
