from __future__ import annotations

import os
from pathlib import Path
from typing import Any

from import_engine.infrastructure.alias_repository import AliasRepository
from import_engine.infrastructure.sql_server_alias_repository import (
    SqlServerAliasRepository,
)


def build_alias_repository(root: Path) -> Any:
    provider = os.getenv("INOVASKILL_DATABASE_PROVIDER", "sqlite").strip().lower()

    if provider in {"sqlserver", "sql_server", "mssql"}:
        connection_string = os.getenv("INOVASKILL_DATABASE_CONNECTION_STRING", "")
        return SqlServerAliasRepository(connection_string)

    sqlite_path = os.getenv(
        "INOVASKILL_SQLITE_PATH",
        str(root / "data" / "aliases.sqlite"),
    )
    return AliasRepository(sqlite_path)
