from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from import_engine.domain.canonical import CUSTOMER_IMPORT_SCHEMA
from import_engine.infrastructure.alias_repository import AliasRepository
from import_engine.infrastructure.readers import ReaderFactory
from import_engine.pipeline.import_pipeline import ImportPipeline
from import_engine.services.dynamic_mapper import DynamicMapper
from import_engine.services.normalizer import HeaderNormalizer
from import_engine.services.similarity import JaroWinklerSimilarity
from import_engine.services.validator import ValidationLayer


class ImportEngineTest(unittest.TestCase):
    def test_imports_csv_using_learned_alias(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            csv_path = root / "customers.csv"
            csv_path.write_text(
                "Nome Cliente,E-mail\nAna,ana@example.com\n",
                encoding="utf-8",
            )

            normalizer = HeaderNormalizer()
            aliases = AliasRepository(root / "aliases.sqlite")
            aliases.upsert_alias(
                schema_name=CUSTOMER_IMPORT_SCHEMA.name,
                canonical_field="customer_name",
                alias="Nome Cliente",
                normalized_alias=normalizer.normalize("Nome Cliente"),
                confidence=1.0,
                source="test",
            )
            aliases.upsert_alias(
                schema_name=CUSTOMER_IMPORT_SCHEMA.name,
                canonical_field="email",
                alias="E-mail",
                normalized_alias=normalizer.normalize("E-mail"),
                confidence=1.0,
                source="test",
            )

            pipeline = ImportPipeline(
                reader_factory=ReaderFactory(),
                mapper=DynamicMapper(
                    alias_repository=aliases,
                    normalizer=normalizer,
                    similarity=JaroWinklerSimilarity(),
                ),
                validator=ValidationLayer(),
            )

            result = pipeline.import_file(
                file_path=csv_path,
                schema=CUSTOMER_IMPORT_SCHEMA,
            )

            self.assertEqual(result.records[0]["customer_name"], "Ana")
            self.assertEqual(result.records[0]["email"], "ana@example.com")
            self.assertEqual(result.validation_issues, [])

    def test_similarity_requires_confirmation_for_close_typo(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            normalizer = HeaderNormalizer()
            mapper = DynamicMapper(
                alias_repository=AliasRepository(Path(temp_dir) / "aliases.sqlite"),
                normalizer=normalizer,
                similarity=JaroWinklerSimilarity(),
            )

            plan = mapper.build_mapping_plan(
                headers=["Custumer name"],
                schema=CUSTOMER_IMPORT_SCHEMA,
            )

            self.assertEqual(plan.matches[0].canonical_field, "customer_name")


if __name__ == "__main__":
    unittest.main()
