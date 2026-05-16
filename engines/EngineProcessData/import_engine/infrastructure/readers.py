from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol


@dataclass(frozen=True)
class TabularData:
    headers: list[str]
    rows: list[dict[str, str]]


class SpreadsheetReader(Protocol):
    def read(self, path: str | Path) -> TabularData:
        raise NotImplementedError


class CsvReader:
    def read(self, path: str | Path) -> TabularData:
        with Path(path).open("r", encoding="utf-8-sig", newline="") as file:
            reader = csv.DictReader(file)
            headers = list(reader.fieldnames or [])
            return TabularData(headers=headers, rows=[dict(row) for row in reader])


class ExcelReader:
    def read(self, path: str | Path) -> TabularData:
        try:
            import pandas as pd
        except ImportError as exc:
            raise RuntimeError(
                "ExcelReader requires pandas and an Excel backend such as openpyxl."
            ) from exc

        frame = pd.read_excel(path, dtype=str).fillna("")
        headers = [str(column) for column in frame.columns]
        return TabularData(headers=headers, rows=frame.to_dict(orient="records"))


class ReaderFactory:
    def for_path(self, path: str | Path) -> SpreadsheetReader:
        suffix = Path(path).suffix.lower()
        if suffix == ".csv":
            return CsvReader()
        if suffix in {".xlsx", ".xls"}:
            return ExcelReader()
        raise ValueError(f"Unsupported file type: {suffix}")

