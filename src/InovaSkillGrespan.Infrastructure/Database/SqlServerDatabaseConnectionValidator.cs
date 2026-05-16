using InovaSkillGrespan.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace InovaSkillGrespan.Infrastructure.Database;

public sealed class SqlServerDatabaseConnectionValidator(
    IOptions<DatabaseOptions> options) : IDatabaseConnectionValidator
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task<DatabaseConnectionStatus> ValidateAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select db_name()";

        var databaseName = Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken)) ?? "";

        return new DatabaseConnectionStatus(
            IsConnected: true,
            DatabaseName: databaseName,
            ServerVersion: connection.ServerVersion);
    }
}
