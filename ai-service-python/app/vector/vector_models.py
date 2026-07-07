from dataclasses import dataclass, field
from typing import Any


@dataclass
class VectorPointInput:
    # Model noi bo: mot point se upsert vao Qdrant.
    point_id: str
    vector: list[float]
    payload: dict[str, Any] = field(default_factory=dict)


@dataclass
class VectorSearchResult:
    # Model noi bo: mot hit tra ve tu Qdrant.
    score: float
    payload: dict[str, Any] = field(default_factory=dict)
