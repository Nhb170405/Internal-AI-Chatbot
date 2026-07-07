from dataclasses import dataclass, field
from typing import Any


@dataclass
class TextChunk:
    # Model noi bo cua Python chunker.
    # API response se map tu model nay sang ChunkItemResponse.
    chunk_index: int
    content: str
    start_offset: int | None = None
    end_offset: int | None = None
    metadata: dict[str, Any] = field(default_factory=dict)