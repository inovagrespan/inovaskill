from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from import_engine.domain.canonical import CUSTOMER_IMPORT_SCHEMA
from import_engine.infrastructure.alias_repository import AliasRepository
from import_engine.infrastructure.readers import ReaderFactory
from import_engine.pipeline.import_pipeline import ImportPipeline
from import_engine.services.dynamic_mapper import DynamicMapper
from import_engine.services.normalizer import HeaderNormalizer
from import_engine.services.similarity import JaroWinklerSimilarity
from import_engine.services.validator import ValidationLayer


def build_pipeline() -> ImportPipeline:
    alias_repository = AliasRepository(ROOT / "data" / "aliases.sqlite")
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


def seed_aliases(
    alias_repository: AliasRepository,
    normalizer: HeaderNormalizer,
) -> None:
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


def main() -> None:
    pipeline = build_pipeline()
    file_path = ROOT / "examples" / "sample_customers.csv"

    preview = pipeline.preview_mapping(
        file_path=file_path,
        schema=CUSTOMER_IMPORT_SCHEMA,
    )

    print("Mapping sugerido:")
    for match in preview.matches:
        print(
            f"- {match.source_column!r} -> {match.canonical_field} "
            f"score={match.score:.3f} source={match.source.value} "
            f"confirm={match.requires_confirmation}"
        )

    confirmations = {
        match.source_column: match.canonical_field
        for match in preview.pending_confirmation()
        if match.canonical_field
    }

    result = pipeline.import_file(
        file_path=file_path,
        schema=CUSTOMER_IMPORT_SCHEMA,
        confirmations=confirmations,
    )

    print("\nRegistros canonicos:")
    for record in result.records:
        print(record)

    print("\nErros de validacao:")
    for issue in result.validation_issues:
        print(issue)


if __name__ == "__main__":
    main()
