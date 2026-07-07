from dataclasses import dataclass, field


@dataclass
class OcrPageResult:
    # Ket qua OCR cua mot page/anh.
    page_number: int
    text: str
    character_count: int
    warnings: list[str] = field(default_factory=list)


@dataclass
class OcrDocumentResult:
    # Ket qua OCR cua ca document.
    text: str
    page_count: int
    pages_with_text: int
    character_count: int
    warnings: list[str] = field(default_factory=list)
    metadata: dict = field(default_factory=dict)
