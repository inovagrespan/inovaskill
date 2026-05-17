from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict
from datetime import UTC, datetime
from decimal import Decimal
from pathlib import Path
from typing import Any
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from import_engine.domain.canonical import (
    CUSTOMER_IMPORT_SCHEMA,
    CanonicalField,
    CanonicalSchema,
)
from import_engine.domain.import_result import ColumnMatch, ValidationIssue
from import_engine.infrastructure.alias_repository_factory import build_alias_repository
from import_engine.infrastructure.readers import ReaderFactory
from import_engine.pipeline.import_pipeline import ImportPipeline
from import_engine.services.dynamic_mapper import DynamicMapper
from import_engine.services.normalizer import HeaderNormalizer
from import_engine.services.similarity import JaroWinklerSimilarity
from import_engine.services.validator import ValidationLayer

DATA_TYPE_MAPPING: dict[str, type] = {
    "string": str,
    "str": str,
    "int": int,
    "integer": int,
    "float": float,
    "decimal": Decimal,
    "bool": bool,
}


def build_pipeline(schema_name: str, alias_seed: dict[str, list[str]] | None) -> ImportPipeline:
    alias_repository = build_alias_repository(ROOT)
    normalizer = HeaderNormalizer()
    seed_aliases(alias_repository, normalizer, schema_name, alias_seed)

    return ImportPipeline(
        reader_factory=ReaderFactory(),
        mapper=DynamicMapper(
            alias_repository=alias_repository,
            normalizer=normalizer,
            similarity=JaroWinklerSimilarity(),
        ),
        validator=ValidationLayer(),
    )


def seed_aliases(
    alias_repository: Any,
    normalizer: HeaderNormalizer,
    schema_name: str,
    alias_seed: dict[str, list[str]] | None,
) -> None:
    aliases = alias_seed or {
        "customer_name": ["nome", "nome cliente", "cliente", "customer"],
        "email": ["email", "e mail", "e-mail"],
        "document_number": ["cpf", "cnpj", "cpf cnpj", "documento"],
        "phone": ["telefone", "fone", "phone"],
        "city": ["cidade", "municipio", "city"],
        "state": ["estado", "uf", "state"],
        "amount": ["valor", "valor total", "amount", "total"],
    }

    for canonical_field, values in aliases.items():
        for alias in values:
            alias_repository.upsert_alias(
                schema_name=schema_name,
                canonical_field=canonical_field,
                alias=alias,
                normalized_alias=normalizer.normalize(alias),
                confidence=1.0,
                source="seed",
            )


def serialize_match(match: ColumnMatch) -> dict[str, Any]:
    payload = asdict(match)
    payload["source"] = match.source.value
    return payload


def serialize_issue(issue: ValidationIssue) -> dict[str, Any]:
    return asdict(issue)


def json_default(value: Any) -> Any:
    if isinstance(value, Decimal):
        return str(value)
    raise TypeError(f"Object of type {type(value).__name__} is not JSON serializable")


def load_confirmations(raw_json: str | None) -> dict[str, str] | None:
    if not raw_json:
        return None

    data = json.loads(raw_json)
    if not isinstance(data, dict):
        raise ValueError("Confirmations must be a JSON object.")

    return {str(key): str(value) for key, value in data.items()}


def resolve_data_type(raw_type: str | None) -> type:
    if not raw_type:
        return str
    return DATA_TYPE_MAPPING.get(raw_type.strip().lower(), str)


def build_schema_from_request(request_payload: dict[str, Any]) -> tuple[CanonicalSchema, dict[str, list[str]]]:
    template = request_payload.get("template") or {}
    config = template.get("config") or {}
    fields_payload = config.get("fields") or []
    schema_name = template.get("type") or "customer_import"
    version = str(template.get("version") or "1")

    fields: list[CanonicalField] = []
    aliases: dict[str, list[str]] = {}
    for field in fields_payload:
        internal_name = str(field.get("internalName") or field.get("name") or "").strip()
        if not internal_name:
            continue

        fields.append(
            CanonicalField(
                name=internal_name,
                label=str(field.get("label") or internal_name),
                required=bool(field.get("required", False)),
                data_type=resolve_data_type(field.get("dataType")),
                description=str(field.get("description") or ""),
            )
        )

        raw_aliases = field.get("aliases") or []
        aliases[internal_name] = [str(alias) for alias in raw_aliases if str(alias).strip()]

    if not fields:
        return CUSTOMER_IMPORT_SCHEMA, {}

    return CanonicalSchema(name=schema_name, version=version, fields=tuple(fields)), aliases


def process_file(
    file_path: Path,
    confirmations: dict[str, str] | None,
    schema: CanonicalSchema,
    alias_seed: dict[str, list[str]],
) -> dict[str, Any]:
    pipeline = build_pipeline(schema.name, alias_seed)
    result = pipeline.import_file(
        file_path=file_path,
        schema=schema,
        confirmations=confirmations,
    )

    return {
        "schema": {
            "name": schema.name,
            "version": schema.version,
        },
        "mappingPlan": {
            "matches": [serialize_match(match) for match in result.mapping_plan.matches],
            "pendingConfirmation": [
                serialize_match(match)
                for match in result.mapping_plan.pending_confirmation()
            ],
            "unmappedColumns": result.mapping_plan.unmapped_columns(),
        },
        "records": result.records,
        "validationIssues": [
            serialize_issue(issue) for issue in result.validation_issues
        ],
        "summary": {
            "totalRecords": len(result.records),
            "validationIssueCount": len(result.validation_issues),
            "pendingConfirmationCount": len(
                result.mapping_plan.pending_confirmation()
            ),
            "unmappedColumnCount": len(result.mapping_plan.unmapped_columns()),
        },
    }


def build_v1_response(
    request_payload: dict[str, Any],
    pipeline_payload: dict[str, Any],
    started_at: datetime,
) -> dict[str, Any]:
    summary = pipeline_payload["summary"]
    validation_issues = pipeline_payload["validationIssues"]
    options = request_payload.get("options") or {}
    max_errors = int(options.get("maxErrors") or 500)
    now = datetime.now(UTC)

    all_errors = [
        {
            "code": "VALIDATION_ISSUE",
            "message": issue["message"],
            "severity": "error",
            "rowNumber": issue.get("row_number"),
            "columnName": issue.get("field"),
            "rawValue": issue.get("value"),
        }
        for issue in validation_issues
    ]
    errors = all_errors[:max_errors]

    mapping_preview = [
        {
            "sourceColumn": match["source_column"],
            "targetField": match["canonical_field"],
            "score": match["score"],
            "strategy": match["source"],
        }
        for match in pipeline_payload["mappingPlan"]["matches"]
    ]

    return {
        "contractVersion": "1.0",
        "jobId": request_payload.get("jobId", str(uuid4())),
        "correlationId": request_payload.get("correlationId", f"corr-{uuid4()}"),
        "status": "CompletedWithWarnings" if errors else "Completed",
        "summary": {
            "totalRows": summary["totalRecords"],
            "importedRows": max(0, summary["totalRecords"] - len(errors)),
            "failedRows": len(errors),
            "warningCount": 0,
            "errorCount": len(all_errors),
            "durationMs": int((now - started_at).total_seconds() * 1000),
        },
        "preview": {
            "detectedColumns": [
                match["source_column"]
                for match in pipeline_payload["mappingPlan"]["matches"]
            ],
            "mapping": mapping_preview,
            "sampleRows": pipeline_payload["records"][:10],
        },
        "warnings": [],
        "errors": errors,
        "logs": [
            {
                "stage": "import_pipeline",
                "level": "info",
                "message": "Pipeline finished successfully.",
                "timestamp": now.isoformat(),
                "details": {
                    "pendingConfirmationCount": summary["pendingConfirmationCount"],
                    "unmappedColumnCount": summary["unmappedColumnCount"],
                    "returnedErrorCount": len(errors),
                    "totalErrorCount": len(all_errors),
                },
            }
        ],
    }


def load_request(raw_json: str | None, file_path: Path) -> dict[str, Any]:
    if not raw_json:
        return {
            "contractVersion": "1.0",
            "jobId": str(uuid4()),
            "correlationId": f"corr-{uuid4()}",
            "file": {"path": str(file_path), "name": file_path.name},
            "confirmations": None,
        }

    data = json.loads(raw_json)
    if not isinstance(data, dict):
        raise ValueError("Request payload must be a JSON object.")

    return data


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Runs the dynamic import engine and returns JSON."
    )
    parser.add_argument("file_path", type=Path)
    parser.add_argument("--confirmations-json", default=None)
    parser.add_argument("--request-json", default=None)
    args = parser.parse_args()

    try:
        started_at = datetime.now(UTC)
        request_payload = load_request(args.request_json, args.file_path)
        schema, alias_seed = build_schema_from_request(request_payload)
        confirmations = request_payload.get("confirmations")
        if confirmations is None:
            confirmations = load_confirmations(args.confirmations_json)

        payload = process_file(args.file_path, confirmations, schema, alias_seed)
        payload = build_v1_response(request_payload, payload, started_at)
        print(json.dumps(payload, ensure_ascii=False, default=json_default))
        return 0
    except Exception as exc:
        error = {"error": type(exc).__name__, "message": str(exc)}
        print(json.dumps(error, ensure_ascii=False), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
