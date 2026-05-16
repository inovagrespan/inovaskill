from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict
from decimal import Decimal
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from import_engine.domain.canonical import CUSTOMER_IMPORT_SCHEMA
from import_engine.domain.import_result import ColumnMatch, ValidationIssue
from import_engine.infrastructure.alias_repository_factory import build_alias_repository
from import_engine.infrastructure.readers import ReaderFactory
from import_engine.pipeline.import_pipeline import ImportPipeline
from import_engine.services.dynamic_mapper import DynamicMapper
from import_engine.services.normalizer import HeaderNormalizer
from import_engine.services.similarity import JaroWinklerSimilarity
from import_engine.services.validator import ValidationLayer


def build_pipeline() -> ImportPipeline:
    alias_repository = build_alias_repository(ROOT)
    normalizer = HeaderNormalizer()
    seed_aliases(alias_repository, normalizer)

    return ImportPipeline(
        reader_factory=ReaderFactory(),
        mapper=DynamicMapper(
            alias_repository=alias_repository,
            normalizer=normalizer,
            similarity=JaroWinklerSimilarity(),
        ),
        validator=ValidationLayer(),
    )


def seed_aliases(alias_repository: AliasRepository, normalizer: HeaderNormalizer) -> None:
    aliases = {
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
                schema_name=CUSTOMER_IMPORT_SCHEMA.name,
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


def process_file(file_path: Path, confirmations: dict[str, str] | None) -> dict[str, Any]:
    pipeline = build_pipeline()
    result = pipeline.import_file(
        file_path=file_path,
        schema=CUSTOMER_IMPORT_SCHEMA,
        confirmations=confirmations,
    )

    return {
        "schema": {
            "name": CUSTOMER_IMPORT_SCHEMA.name,
            "version": CUSTOMER_IMPORT_SCHEMA.version,
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


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Runs the dynamic import engine and returns JSON."
    )
    parser.add_argument("file_path", type=Path)
    parser.add_argument("--confirmations-json", default=None)
    args = parser.parse_args()

    try:
        confirmations = load_confirmations(args.confirmations_json)
        payload = process_file(args.file_path, confirmations)
        print(json.dumps(payload, ensure_ascii=False, default=json_default))
        return 0
    except Exception as exc:
        error = {"error": type(exc).__name__, "message": str(exc)}
        print(json.dumps(error, ensure_ascii=False), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
