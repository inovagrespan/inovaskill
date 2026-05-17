namespace InovaSkillGrespan.Application.Abstractions;

public interface IDatabaseMetadataReader
{
    Task<IReadOnlyList<DatabaseTableMetadata>> ListTablesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DatabaseColumnMetadata>> ListColumnsAsync(
        string tableName,
        CancellationToken cancellationToken);
}

public sealed record DatabaseTableMetadata(
    string SchemaName,
    string TableName);

public sealed record DatabaseColumnMetadata(
    string ColumnName,
    string DataType,
    bool IsNullable);

