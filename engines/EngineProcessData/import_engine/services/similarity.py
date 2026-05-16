from __future__ import annotations


class JaroWinklerSimilarity:
    def score(self, left: str, right: str) -> float:
        jaro = self._jaro(left, right)
        prefix = 0
        for left_char, right_char in zip(left[:4], right[:4]):
            if left_char != right_char:
                break
            prefix += 1
        return jaro + (prefix * 0.1 * (1.0 - jaro))

    def _jaro(self, left: str, right: str) -> float:
        if left == right:
            return 1.0
        if not left or not right:
            return 0.0

        match_distance = max(len(left), len(right)) // 2 - 1
        left_matches = [False] * len(left)
        right_matches = [False] * len(right)

        matches = 0
        transpositions = 0

        for index, left_char in enumerate(left):
            start = max(0, index - match_distance)
            end = min(index + match_distance + 1, len(right))
            for candidate in range(start, end):
                if right_matches[candidate] or left_char != right[candidate]:
                    continue
                left_matches[index] = True
                right_matches[candidate] = True
                matches += 1
                break

        if matches == 0:
            return 0.0

        right_index = 0
        for index, left_char in enumerate(left):
            if not left_matches[index]:
                continue
            while not right_matches[right_index]:
                right_index += 1
            if left_char != right[right_index]:
                transpositions += 1
            right_index += 1

        return (
            (matches / len(left))
            + (matches / len(right))
            + ((matches - transpositions / 2) / matches)
        ) / 3.0

