from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Any


class MatchSource(str, Enum):
    ALIAS = "alias"
    SIMILARITY = "similarity"
    USER_CONFIRMED = "user_confirmed"
    UNMAPPED = "unmapped"


@dataclass(frozen=True)
class ColumnMatch:
    source_column: str
    normalized_source: str
    canonical_field: str | None
    score: float
    source: MatchSource
    requires_confirmation: bool = False


@dataclass
class MappingPlan:
    matches: list[ColumnMatch]

    def accepted_mapping(self) -> dict[str, str]:
        return {
            match.source_column: match.canonical_field
            for match in self.matches
            if match.canonical_field and not match.requires_confirmation
        }

    def pending_confirmation(self) -> list[ColumnMatch]:
        return [match for match in self.matches if match.requires_confirmation]

    def unmapped_columns(self) -> list[str]:
        return [
            match.source_column
            for match in self.matches
            if match.canonical_field is None
        ]


@dataclass
class ValidationIssue:
    row_number: int
    field: str
    message: str
    value: Any


@dataclass
class ImportResult:
    mapping_plan: MappingPlan
    records: list[dict[str, Any]]
    validation_issues: list[ValidationIssue] = field(default_factory=list)

