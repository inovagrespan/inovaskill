from __future__ import annotations

from dataclasses import dataclass
from decimal import Decimal
from typing import Any


@dataclass(frozen=True)
class CanonicalField:
    name: str
    label: str
    required: bool = False
    data_type: type = str
    description: str = ""


@dataclass(frozen=True)
class CanonicalSchema:
    name: str
    version: str
    fields: tuple[CanonicalField, ...]

    def field_names(self) -> set[str]:
        return {field.name for field in self.fields}

    def required_fields(self) -> set[str]:
        return {field.name for field in self.fields if field.required}

    def get_field(self, name: str) -> CanonicalField:
        for field in self.fields:
            if field.name == name:
                return field
        raise KeyError(f"Unknown canonical field: {name}")


CUSTOMER_IMPORT_SCHEMA = CanonicalSchema(
    name="customer_import",
    version="1.0",
    fields=(
        CanonicalField("customer_name", "Customer name", True, str),
        CanonicalField("email", "Email", True, str),
        CanonicalField("document_number", "Document number", False, str),
        CanonicalField("phone", "Phone", False, str),
        CanonicalField("city", "City", False, str),
        CanonicalField("state", "State", False, str),
        CanonicalField("amount", "Amount", False, Decimal),
    ),
)


CanonicalRecord = dict[str, Any]

