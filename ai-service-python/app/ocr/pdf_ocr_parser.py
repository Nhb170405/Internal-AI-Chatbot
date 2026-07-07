from pathlib import Path
from tempfile import TemporaryDirectory

from app.ingestion.parsed_document import ParsedDocument
from app.ocr.ocr_service import ocr_image_file
from app.ocr.pdf_to_images import render_pdf_pages_to_images
from app.ocr.text_cleanup import cleanup_ocr_text


def parse_pdf_with_ocr(file_path: str, file_name: str, language: str = "vie+eng") -> ParsedDocument:
    # Bai tap Milestone 9:
    # Ham nay dung khi PDF text layer khong co hoac qua it.
    #
    # Flow:
    # 1. Tao TemporaryDirectory de chua anh render tu PDF.
    # 2. Goi render_pdf_pages_to_images(file_path, temp_dir).
    # 3. Voi moi image path:
    #    - goi ocr_image_file(image_path, page_number, language).
    #    - neu page co text thi append vao output voi marker Page N.
    # 4. Ghep text:
    #    Document: <file_name>
    #    File type: PDF
    #    Strategy: pdf ocr fallback
    #    PDF pages: <n>
    #
    #    Page 1:
    #    ...
    # 5. Cleanup full text.
    # 6. Neu text rong thi raise ValueError("OCR returned empty text.")
    # 7. Return ParsedDocument parser_name="pdf_ocr_parser".
    #
    # Luu y:
    # - TemporaryDirectory tu xoa anh sau khi parse xong.
    # - OCR cham hon parse text layer rat nhieu.
    # - OCR tieng Viet can language data "vie".
    path = Path(file_path)

    if not path.exists():
        raise ValueError("PDF path does not exist.")

    with TemporaryDirectory() as temp_dir:
        image_paths = render_pdf_pages_to_images(file_path, temp_dir)

        if not image_paths:
            raise ValueError("PDF render returned no images.")

        lines = [
            f"Document: {file_name}",
            "File type: PDF",
            "Strategy: pdf ocr fallback",
            f"PDF pages: {len(image_paths)}",
            "",
        ]

        warnings: list[str] = []
        pages_with_text = 0

        for page_index, image_path in enumerate(image_paths, start=1):
            page_result = ocr_image_file(
                image_path=image_path,
                page_number=page_index,
                language=language,
            )

            warnings.extend(page_result.warnings)

            if page_result.text.strip():
                lines.append(f"Page {page_index}:")
                lines.append(page_result.text)
                lines.append("")
                pages_with_text += 1
            else:
                warnings.append(f"Page {page_index} OCR returned empty text.")

        raw_document_text = "\n".join(lines)
        cleaned_text = cleanup_ocr_text(raw_document_text)

        if not cleaned_text.strip() or pages_with_text == 0:
            raise ValueError("OCR returned empty text.")

        return ParsedDocument(
            text=cleaned_text,
            parser_name="pdf_ocr_parser",
            character_count=len(cleaned_text),
            page_count=len(image_paths),
            metadata={
                "strategy": "pdf_ocr_fallback",
                "pageCount": len(image_paths),
                "pagesWithText": pages_with_text,
                "language": language,
            },
            warnings=warnings,
        )