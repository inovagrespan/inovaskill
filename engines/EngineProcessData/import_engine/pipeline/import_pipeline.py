from __future__ import annotations

from pathlib import Path

from import_engine.domain.canonical import CanonicalRecord, CanonicalSchema
from import_engine.domain.import_result import ImportResult, MappingPlan
from import_engine.infrastructure.readers import ReaderFactory
from import_engine.services.dynamic_mapper import DynamicMapper
from import_engine.services.validator import ValidationLayer


class ImportPipeline:
    def __init__(
        self,
        *,
        reader_factory: ReaderFactory,
        mapper: DynamicMapper,
        validator: ValidationLayer,
    ) -> None:
        self.reader_factory = reader_factory
        self.mapper = mapper
        self.validator = validator

    def preview_mapping(
        self,
        *,
        file_path: str | Path,
        schema: CanonicalSchema,
    ) -> MappingPlan:
        tabular_data = self.reader_factory.for_path(file_path).read(file_path)
        return self.mapper.build_mapping_plan(headers=tabular_data.headers, schema=schema)

    def import_file(
        self,
        *,
        file_path: str | Path,
        schema: CanonicalSchema,
        confirmations: dict[str, str] | None = None,
    ) -> ImportResult:
        tabular_data = self.reader_factory.for_path(file_path).read(file_path)
        mapping_plan = self.mapper.build_mapping_plan(
            headers=tabular_data.headers,
            schema=schema,
        )

        if confirmations:
            mapping_plan = self.mapper.apply_user_confirmations(
                schema=schema,
                mapping_plan=mapping_plan,
                confirmations=confirmations,
            )

        mapping = mapping_plan.accepted_mapping()
        records: list[CanonicalRecord] = []
        issues = []

        for row_number, row in enumerate(tabular_data.rows, start=2):
            record = self._to_canonical_record(row, mapping)
            issues.extend(
                self.validator.validate_record(
                    schema=schema,
                    record=record,
                    row_number=row_number,
                )
            )
            records.append(record)

        return ImportResult(
            mapping_plan=mapping_plan,
            records=records,
            validation_issues=issues,
        )

    def _to_canonical_record(
        self,
        row: dict[str, str],
        mapping: dict[str, str],
    ) -> CanonicalRecord:
        canonical: CanonicalRecord = {}
        for source_column, canonical_field in mapping.items():
            canonical[canonical_field] = row.get(source_column)
        return canonical

