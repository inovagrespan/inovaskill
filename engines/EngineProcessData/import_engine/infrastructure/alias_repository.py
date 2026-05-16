from __future__ import annotations

import sqlite3
from contextlib import closing
from pathlib import Path


class AliasRepository:
    """Persistence boundary for learned aliases.

    SQLite is enough for local/demo usage. In an enterprise deployment this
    class can be replaced by Postgres, DynamoDB or a central metadata service
    without changing the mapper.
    """

    def __init__(self, database_path: str | Path) -> None:
        self.database_path = Path(database_path)
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def _connect(self) -> sqlite3.Connection:
        return sqlite3.connect(self.database_path)

    def _initialize(self) -> None:
        with closing(self._connect()) as conn:
            conn.execute(
                """
                create table if not exists column_aliases (
                    id integer primary key autoincrement,
                    schema_name text not null,
                    canonical_field text not null,
                    alias text not null,
                    normalized_alias text not null,
                    confidence real not null default 1.0,
                    source text not null default 'user',
                    usage_count integer not null default 0,
                    created_at text not null default current_timestamp,
                    updated_at text not null default current_timestamp,
                    unique(schema_name, normalized_alias)
                )
                """
            )
            conn.execute(
                """
                create index if not exists idx_column_aliases_lookup
                on column_aliases(schema_name, normalized_alias)
                """
            )
            conn.commit()

    def find_by_normalized_alias(
        self, schema_name: str, normalized_alias: str
    ) -> str | None:
        with closing(self._connect()) as conn:
            row = conn.execute(
                """
                select canonical_field
                from column_aliases
                where schema_name = ? and normalized_alias = ?
                """,
                (schema_name, normalized_alias),
            ).fetchone()
            return row[0] if row else None

    def list_aliases(self, schema_name: str) -> dict[str, list[str]]:
        with closing(self._connect()) as conn:
            rows = conn.execute(
                """
                select canonical_field, normalized_alias
                from column_aliases
                where schema_name = ?
                order by usage_count desc, updated_at desc
                """,
                (schema_name,),
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
            conn.execute(
                """
                insert into column_aliases (
                    schema_name,
                    canonical_field,
                    alias,
                    normalized_alias,
                    confidence,
                    source,
                    usage_count
                )
                values (?, ?, ?, ?, ?, ?, 1)
                on conflict(schema_name, normalized_alias)
                do update set
                    canonical_field = excluded.canonical_field,
                    alias = excluded.alias,
                    confidence = max(column_aliases.confidence, excluded.confidence),
                    source = excluded.source,
                    usage_count = column_aliases.usage_count + 1,
                    updated_at = current_timestamp
                """,
                (
                    schema_name,
                    canonical_field,
                    alias,
                    normalized_alias,
                    confidence,
                    source,
                ),
            )
            conn.commit()
