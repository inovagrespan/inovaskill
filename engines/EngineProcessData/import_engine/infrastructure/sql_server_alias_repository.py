from __future__ import annotations

from contextlib import closing


class SqlServerAliasRepository:
    """SQL Server persistence boundary for learned column aliases."""

    def __init__(self, connection_string: str) -> None:
        if not connection_string:
            raise ValueError("SQL Server connection string is required.")
        self.connection_string = connection_string
        self._initialize()

    def _connect(self):
        try:
            import pyodbc
        except ImportError as exc:
            raise RuntimeError(
                "SqlServerAliasRepository requires pyodbc. Install it with: "
                "python -m pip install pyodbc"
            ) from exc

        return pyodbc.connect(self.connection_string)

    def _initialize(self) -> None:
        with closing(self._connect()) as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                if object_id('dbo.column_aliases', 'U') is null
                begin
                    create table dbo.column_aliases (
                        id int identity(1,1) primary key,
                        schema_name nvarchar(128) not null,
                        canonical_field nvarchar(128) not null,
                        alias nvarchar(255) not null,
                        normalized_alias nvarchar(255) not null,
                        confidence float not null default 1.0,
                        source nvarchar(64) not null default 'user',
                        usage_count int not null default 0,
                        created_at datetime2 not null default sysdatetime(),
                        updated_at datetime2 not null default sysdatetime(),
                        constraint uq_column_aliases_schema_alias
                            unique(schema_name, normalized_alias)
                    );
                end
                """
            )
            cursor.execute(
                """
                if not exists (
                    select 1
                    from sys.indexes
                    where name = 'idx_column_aliases_lookup'
                      and object_id = object_id('dbo.column_aliases')
                )
                begin
                    create index idx_column_aliases_lookup
                    on dbo.column_aliases(schema_name, normalized_alias);
                end
                """
            )
            conn.commit()

    def find_by_normalized_alias(
        self, schema_name: str, normalized_alias: str
    ) -> str | None:
        with closing(self._connect()) as conn:
            row = conn.cursor().execute(
                """
                select canonical_field
                from dbo.column_aliases
                where schema_name = ? and normalized_alias = ?
                """,
                schema_name,
                normalized_alias,
            ).fetchone()
            return row[0] if row else None

    def list_aliases(self, schema_name: str) -> dict[str, list[str]]:
        with closing(self._connect()) as conn:
            rows = conn.cursor().execute(
                """
                select canonical_field, normalized_alias
                from dbo.column_aliases
                where schema_name = ?
                order by usage_count desc, updated_at desc
                """,
                schema_name,
            ).fetchall()

        aliases: dict[str, list[str]] = {}
        for canonical_field, normalized_alias in rows:
            aliases.setdefault(canonical_field, []).append(normalized_alias)
        return aliases

    def upsert_alias(
        self,
        *,
        schema_name: str,
        canonical_field: str,
        alias: str,
        normalized_alias: str,
        confidence: float,
        source: str,
    ) -> None:
        with closing(self._connect()) as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                merge dbo.column_aliases with (holdlock) as target
                using (
                    select
                        ? as schema_name,
                        ? as canonical_field,
                        ? as alias,
                        ? as normalized_alias,
                        ? as confidence,
                        ? as source
                ) as source_data
                on target.schema_name = source_data.schema_name
                   and target.normalized_alias = source_data.normalized_alias
                when matched then
                    update set
                        canonical_field = source_data.canonical_field,
                        alias = source_data.alias,
                        confidence = case
                            when target.confidence > source_data.confidence
                                then target.confidence
                            else source_data.confidence
                        end,
                        source = source_data.source,
                        usage_count = target.usage_count + 1,
                        updated_at = sysdatetime()
                when not matched then
                    insert (
                        schema_name,
                        canonical_field,
                        alias,
                        normalized_alias,
                        confidence,
                        source,
                        usage_count
                    )
                    values (
                        source_data.schema_name,
                        source_data.canonical_field,
                        source_data.alias,
                        source_data.normalized_alias,
                        source_data.confidence,
                        source_data.source,
                        1
                    );
                """,
                schema_name,
                canonical_field,
                alias,
                normalized_alias,
                confidence,
                source,
            )
            conn.commit()
