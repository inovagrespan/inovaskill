from __future__ import annotations

from decimal import Decimal, InvalidOperation
from typing import Any

from import_engine.domain.canonical import CanonicalRecord, CanonicalSchema
from import_engine.domain.import_result import ValidationIssue


class ValidationLayer:
    def validate_record(
        self,
        *,
        schema: CanonicalSchema,
        record: CanonicalRecord,
        row_number: int,
    ) -> list[ValidationIssue]:
        issues: list[ValidationIssue] = []

        for field_name in schema.required_fields():
            value = record.get(field_name)
            if value is None or str(value).strip() == "":
                issues.append(
                    ValidationIssue(
                        row_number=row_number,
                        field=field_name,
                        message="Required field is missing.",
                        value=value,
                    )
                )

        for field in schema.fields:
            value = record.get(field.name)
            if value is None or value == "":
                continue
            try:
                record[field.name] = self._coerce(value, field.data_type)
            except (ValueError, InvalidOperation) as exc:
                issues.append(
                    ValidationIssue(
                        row_number=row_number,
                        field=field.name,
                        message=f"Invalid {field.data_type.__name__}: {exc}",
                        value=value,
                    )
                )

        return issues

    def _coerce(self, value: Any, data_type: type) -> Any:
        if data_type is Decimal:
            return Decimal(str(value).replace(",", "."))
        return data_type(value)

