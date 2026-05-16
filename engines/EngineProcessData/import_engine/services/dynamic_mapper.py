from __future__ import annotations

from dataclasses import dataclass

from import_engine.domain.canonical import CanonicalSchema
from import_engine.domain.import_result import ColumnMatch, MappingPlan, MatchSource
from import_engine.infrastructure.alias_repository import AliasRepository
from import_engine.services.normalizer import HeaderNormalizer
from import_engine.services.similarity import JaroWinklerSimilarity


@dataclass(frozen=True)
class MapperSettings:
    auto_accept_threshold: float = 0.94
    confirmation_threshold: float = 0.86


class DynamicMapper:
    def __init__(
        self,
        *,
        alias_repository: AliasRepository,
        normalizer: HeaderNormalizer,
        similarity: JaroWinklerSimilarity,
        settings: MapperSettings | None = None,
    ) -> None:
        self.alias_repository = alias_repository
        self.normalizer = normalizer
        self.similarity = similarity
        self.settings = settings or MapperSettings()

    def build_mapping_plan(
        self,
        *,
        headers: list[str],
        schema: CanonicalSchema,
    ) -> MappingPlan:
        alias_index = self.alias_repository.list_aliases(schema.name)
        matches = [self._match_header(header, schema, alias_index) for header in headers]
        return MappingPlan(matches=matches)

    def apply_user_confirmations(
        self,
        *,
        schema: CanonicalSchema,
        mapping_plan: MappingPlan,
        confirmations: dict[str, str],
    ) -> MappingPlan:
        updated_matches: list[ColumnMatch] = []
        for match in mapping_plan.matches:
            confirmed_field = confirmations.get(match.source_column)
            if confirmed_field:
                if confirmed_field not in schema.field_names():
                    raise ValueError(f"Unknown canonical field: {confirmed_field}")
                self.alias_repository.upsert_alias(
                    schema_name=schema.name,
                    canonical_field=confirmed_field,
                    alias=match.source_column,
                    normalized_alias=match.normalized_source,
                    confidence=1.0,
                    source="user",
                )
                updated_matches.append(
                    ColumnMatch(
                        source_column=match.source_column,
                        normalized_source=match.normalized_source,
                        canonical_field=confirmed_field,
                        score=1.0,
                        source=MatchSource.USER_CONFIRMED,
                        requires_confirmation=False,
                    )
                )
                continue
            updated_matches.append(match)
        return MappingPlan(matches=updated_matches)

    def _match_header(
        self,
        header: str,
        schema: CanonicalSchema,
        alias_index: dict[str, list[str]],
    ) -> ColumnMatch:
        normalized_header = self.normalizer.normalize(header)
        alias_match = self.alias_repository.find_by_normalized_alias(
            schema.name, normalized_header
        )
        if alias_match:
            return ColumnMatch(
                source_column=header,
                normalized_source=normalized_header,
                canonical_field=alias_match,
                score=1.0,
                source=MatchSource.ALIAS,
            )

        best_field: str | None = None
        best_score = 0.0

        candidates = self._build_candidates(schema, alias_index)
        for canonical_field, candidate_values in candidates.items():
            score = max(
                self.similarity.score(normalized_header, candidate)
                for candidate in candidate_values
            )
            if score > best_score:
                best_field = canonical_field
                best_score = score

        if best_field and best_score >= self.settings.auto_accept_threshold:
            self.alias_repository.upsert_alias(
                schema_name=schema.name,
                canonical_field=best_field,
                alias=header,
                normalized_alias=normalized_header,
                confidence=best_score,
                source="similarity_auto",
            )
            return ColumnMatch(
                source_column=header,
                normalized_source=normalized_header,
                canonical_field=best_field,
                score=best_score,
                source=MatchSource.SIMILARITY,
            )

        if best_field and best_score >= self.settings.confirmation_threshold:
            return ColumnMatch(
                source_column=header,
                normalized_source=normalized_header,
                canonical_field=best_field,
                score=best_score,
                source=MatchSource.SIMILARITY,
                requires_confirmation=True,
            )

        return ColumnMatch(
            source_column=header,
            normalized_source=normalized_header,
            canonical_field=None,
            score=best_score,
            source=MatchSource.UNMAPPED,
        )

    def _build_candidates(
        self,
        schema: CanonicalSchema,
        alias_index: dict[str, list[str]],
    ) -> dict[str, list[str]]:
        candidates: dict[str, list[str]] = {}
        for field in schema.fields:
            values = {
                self.normalizer.normalize(field.name),
                self.normalizer.normalize(field.label),
                *alias_index.get(field.name, []),
            }
            candidates[field.name] = [value for value in values if value]
        return candidates

