using System.Text.Json;
using InovaSkillGrespan.Application.Abstractions.Imports;
using InovaSkillGrespan.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace InovaSkillGrespan.Infrastructure.Imports;

public sealed class SqlImportJobRepository(SqlConnectionFactory connectionFactory) : IImportJobRepository
{
    public async Task CreateAsync(ImportJobCreateRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into dbo.import_job
            (id, template_version_id, file_name, file_path, requested_by, status, correlation_id, created_at, updated_at)
            values
            (@id, @templateVersionId, @fileName, @filePath, @requestedBy, @status, @correlationId, @createdAt, @createdAt);
            """;
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", request.Id);
        command.Parameters.AddWithValue("@templateVersionId", request.TemplateVersionId);
        command.Parameters.AddWithValue("@fileName", request.FileName);
        command.Parameters.AddWithValue("@filePath", request.FilePath);
        command.Parameters.AddWithValue("@requestedBy", request.RequestedBy);
        command.Parameters.AddWithValue("@status", request.Status);
        command.Parameters.AddWithValue("@correlationId", request.CorrelationId);
        command.Parameters.AddWithValue("@createdAt", request.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        const string sql = """
            update dbo.import_job
            set status = 'Running', started_at = sysutcdatetime(), updated_at = sysutcdatetime()
            where id = @jobId;
            """;
        await ExecuteNonQueryAsync(sql, [("@jobId", jobId)], cancellationToken);
    }

    public async Task MarkCompletedAsync(
        Guid jobId,
        JsonElement summary,
        IReadOnlyList<JsonElement> logs,
        IReadOnlyList<JsonElement> errors,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string jobSql = """
            update dbo.import_job
            set status = @status,
                summary_json = @summary,
                finished_at = sysutcdatetime(),
                updated_at = sysutcdatetime()
            where id = @jobId;
            """;
        var status = errors.Count > 0 ? "CompletedWithWarnings" : "Completed";
        await using (var update = new SqlCommand(jobSql, connection, (SqlTransaction)transaction))
        {
            update.Parameters.AddWithValue("@jobId", jobId);
            update.Parameters.AddWithValue("@status", status);
            update.Parameters.AddWithValue("@summary", summary.GetRawText());
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        const string logSql = """
            insert into dbo.import_job_log (job_id, stage, level, message, details_json, created_at)
            values (@jobId, @stage, @level, @message, @details, sysutcdatetime());
            """;
        foreach (var log in logs)
        {
            await using var insert = new SqlCommand(logSql, connection, (SqlTransaction)transaction);
            insert.Parameters.AddWithValue("@jobId", jobId);
            insert.Parameters.AddWithValue("@stage", log.TryGetProperty("stage", out var stage) ? stage.GetString() ?? "unknown" : "unknown");
            insert.Parameters.AddWithValue("@level", log.TryGetProperty("level", out var level) ? level.GetString() ?? "info" : "info");
            insert.Parameters.AddWithValue("@message", log.TryGetProperty("message", out var message) ? message.GetString() ?? string.Empty : string.Empty);
            insert.Parameters.AddWithValue("@details", log.TryGetProperty("details", out var details) ? details.GetRawText() : "{}");
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        const string errorSql = """
            insert into dbo.import_job_error (job_id, row_number, column_name, error_code, error_message, raw_value, severity, created_at)
            values (@jobId, @rowNumber, @columnName, @errorCode, @errorMessage, @rawValue, @severity, sysutcdatetime());
            """;
        foreach (var error in errors)
        {
            await using var insert = new SqlCommand(errorSql, connection, (SqlTransaction)transaction);
            insert.Parameters.AddWithValue("@jobId", jobId);
            insert.Parameters.AddWithValue("@rowNumber", error.TryGetProperty("rowNumber", out var row) && row.ValueKind == JsonValueKind.Number ? row.GetInt32() : DBNull.Value);
            insert.Parameters.AddWithValue("@columnName", error.TryGetProperty("columnName", out var col) ? (object?)col.GetString() ?? DBNull.Value : DBNull.Value);
            insert.Parameters.AddWithValue("@errorCode", error.TryGetProperty("code", out var code) ? code.GetString() ?? "ERROR" : "ERROR");
            insert.Parameters.AddWithValue("@errorMessage", error.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : string.Empty);
            insert.Parameters.AddWithValue("@rawValue", error.TryGetProperty("rawValue", out var value) ? (object?)value.GetString() ?? DBNull.Value : DBNull.Value);
            insert.Parameters.AddWithValue("@severity", error.TryGetProperty("severity", out var severity) ? severity.GetString() ?? "error" : "error");
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid jobId,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string updateSql = """
            update dbo.import_job
            set status = 'Failed', finished_at = sysutcdatetime(), updated_at = sysutcdatetime()
            where id = @jobId;
            """;
        await using (var update = new SqlCommand(updateSql, connection, (SqlTransaction)transaction))
        {
            update.Parameters.AddWithValue("@jobId", jobId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        const string errorSql = """
            insert into dbo.import_job_error (job_id, row_number, column_name, error_code, error_message, raw_value, severity, created_at)
            values (@jobId, null, null, @errorCode, @errorMessage, null, 'error', sysutcdatetime());
            """;
        await using (var error = new SqlCommand(errorSql, connection, (SqlTransaction)transaction))
        {
            error.Parameters.AddWithValue("@jobId", jobId);
            error.Parameters.AddWithValue("@errorCode", errorCode);
            error.Parameters.AddWithValue("@errorMessage", errorMessage);
            await error.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ImportJobStatusDto?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, status, summary_json, created_at, started_at, finished_at, correlation_id
            from dbo.import_job
            where id = @jobId;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ImportJobStatusDto(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            AsUtc(reader.GetDateTime(3)),
            reader.IsDBNull(4) ? null : AsUtc(reader.GetDateTime(4)),
            reader.IsDBNull(5) ? null : AsUtc(reader.GetDateTime(5)),
            reader.IsDBNull(6) ? string.Empty : reader.GetString(6));
    }

    public async Task<IReadOnlyList<ImportJobErrorDto>> ListErrorsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, row_number, column_name, error_code, error_message, raw_value, severity, created_at
            from dbo.import_job_error
            where job_id = @jobId
            order by id asc;
            """;
        var list = new List<ImportJobErrorDto>();
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(
                new ImportJobErrorDto(
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetString(6),
                    AsUtc(reader.GetDateTime(7))));
        }

        return list;
    }

    private async Task ExecuteNonQueryAsync(
        string sql,
        IEnumerable<(string Name, object Value)> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
