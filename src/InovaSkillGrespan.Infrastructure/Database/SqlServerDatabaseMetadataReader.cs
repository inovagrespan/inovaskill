using InovaSkillGrespan.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace InovaSkillGrespan.Infrastructure.Database;

public sealed class SqlServerDatabaseMetadataReader(
    IOptions<DatabaseOptions> options) : IDatabaseMetadataReader
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task<IReadOnlyList<DatabaseTableMetadata>> ListTablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select t.TABLE_SCHEMA, t.TABLE_NAME
            from INFORMATION_SCHEMA.TABLES t
            where t.TABLE_TYPE = 'BASE TABLE'
            order by t.TABLE_SCHEMA, t.TABLE_NAME;
            """;

        var result = new List<DatabaseTableMetadata>();
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DatabaseTableMetadata(
                reader.GetString(0),
                reader.GetString(1)));
        }

        return result;
    }

    public async Task<IReadOnlyList<DatabaseColumnMetadata>> ListColumnsAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE
            from INFORMATION_SCHEMA.COLUMNS c
            where c.TABLE_NAME = @tableName
            order by c.ORDINAL_POSITION;
            """;

        var result = new List<DatabaseColumnMetadata>();
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DatabaseColumnMetadata(
                reader.GetString(0),
                reader.GetString(1),
                string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase)));
        }

        return result;
    }
}

