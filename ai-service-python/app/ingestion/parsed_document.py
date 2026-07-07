from dataclasses import dataclass, field
from typing import Any


@dataclass
class ParsedDocument:
    # Object noi bo sau khi parser doc file xong.
    # API layer se map object nay sang IngestResponse.
    text: str
    parser_name: str
    character_count: int
    page_count: int | None = None
    metadata: dict[str, Any] = field(default_factory=dict)
    warnings: list[str] = field(default_factory=list)
