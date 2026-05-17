using InovaSkillGrespan.Application.Abstractions.Templates;
using InovaSkillGrespan.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace InovaSkillGrespan.Infrastructure.Templates;

public sealed class SqlTemplateRepository(SqlConnectionFactory connectionFactory) : ITemplateRepository
{
    public async Task<Guid> CreateTemplateAsync(
        TemplateCreateRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        const string sql = """
            insert into dbo.template (id, name, type, status, created_by, created_at, updated_at)
            values (@id, @name, @type, @status, @createdBy, sysutcdatetime(), sysutcdatetime());
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@type", request.Type);
        command.Parameters.AddWithValue("@status", request.Status);
        command.Parameters.AddWithValue("@createdBy", request.CreatedBy);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return id;
    }

    public async Task<Guid> CreateTemplateVersionAsync(
        TemplateVersionCreateRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (request.IsActive)
        {
            const string deactivateSql = """
                update dbo.template_version
                set is_active = 0
                where template_id = @templateId;
                """;
            await using var deactivate = new SqlCommand(deactivateSql, connection, (SqlTransaction)transaction);
            deactivate.Parameters.AddWithValue("@templateId", request.TemplateId);
            await deactivate.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = """
            insert into dbo.template_version (id, template_id, version_number, is_active, config_json, created_by, created_at)
            values (@id, @templateId, @versionNumber, @isActive, @configJson, @createdBy, sysutcdatetime());
            """;
        await using var insert = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
        insert.Parameters.AddWithValue("@id", id);
        insert.Parameters.AddWithValue("@templateId", request.TemplateId);
        insert.Parameters.AddWithValue("@versionNumber", request.VersionNumber);
        insert.Parameters.AddWithValue("@isActive", request.IsActive);
        insert.Parameters.AddWithValue("@configJson", request.ConfigJson);
        insert.Parameters.AddWithValue("@createdBy", request.CreatedBy);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task ActivateTemplateVersionAsync(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string deactivateSql = """
            update dbo.template_version
            set is_active = 0
            where template_id = @templateId;
            """;
        await using var deactivate = new SqlCommand(deactivateSql, connection, (SqlTransaction)transaction);
        deactivate.Parameters.AddWithValue("@templateId", templateId);
        await deactivate.ExecuteNonQueryAsync(cancellationToken);

        const string activateSql = """
            update dbo.template_version
            set is_active = 1
            where id = @versionId and template_id = @templateId;
            """;
        await using var activate = new SqlCommand(activateSql, connection, (SqlTransaction)transaction);
        activate.Parameters.AddWithValue("@templateId", templateId);
        activate.Parameters.AddWithValue("@versionId", versionId);
        await activate.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TemplateSummaryDto>> ListTemplatesAsync(
        string? type,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select id, name, type, status, created_at, updated_at
            from dbo.template
            where (@type is null or type = @type)
            order by updated_at desc;
            """;
        var list = new List<TemplateSummaryDto>();
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@type", (object?)type ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(
                new TemplateSummaryDto(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    AsUtc(reader.GetDateTime(4)),
                    AsUtc(reader.GetDateTime(5))));
        }
        return list;
    }

    public async Task<IReadOnlyList<TemplateVersionDto>> ListTemplateVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select id, template_id, version_number, is_active, config_json, created_at
            from dbo.template_version
            where template_id = @templateId
            order by version_number desc;
            """;
        var list = new List<TemplateVersionDto>();
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@templateId", templateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(
                new TemplateVersionDto(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetInt32(2),
                    reader.GetBoolean(3),
                    reader.GetString(4),
                    AsUtc(reader.GetDateTime(5))));
        }
        return list;
    }

    public async Task<TemplateVersionDto?> GetActiveVersionAsync(
        string templateType,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select top 1 tv.id, tv.template_id, tv.version_number, tv.is_active, tv.config_json, tv.created_at
            from dbo.template_version tv
            inner join dbo.template t on t.id = tv.template_id
            where t.type = @templateType and tv.is_active = 1
            order by tv.version_number desc;
            """;
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@templateType", templateType);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TemplateVersionDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            reader.GetBoolean(3),
            reader.GetString(4),
            AsUtc(reader.GetDateTime(5)));
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
