from __future__ import annotations

import re
import unicodedata


class HeaderNormalizer:
    def normalize(self, value: str) -> str:
        normalized = unicodedata.normalize("NFKD", value or "")
        ascii_value = normalized.encode("ascii", "ignore").decode("ascii")
        lowered = ascii_value.lower().strip()
        without_symbols = re.sub(r"[^a-z0-9]+", " ", lowered)
        without_noise = re.sub(
            r"\b(coluna|campo|field|column|the|de|da|do|dos|das)\b",
            " ",
            without_symbols,
        )
        return re.sub(r"\s+", " ", without_noise).strip()

