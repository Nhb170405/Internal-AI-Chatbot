from pypdf import PdfReader

from app.ingestion.parsed_document import ParsedDocument
from app.ingestion.text_cleanup import cleanup_extracted_text
from app.ocr.pdf_ocr_parser import parse_pdf_with_ocr


OCR_REQUIRED_MESSAGE = "No text layer detected. OCR is required."


def parse_pdf_auto(file_path: str, file_name: str) -> ParsedDocument:
    # PDF ingestion strategy:
    # 1. Thu parse text layer truoc vi nhanh va giu text goc tot hon OCR.
    # 2. Neu PDF khong co text layer thi fallback sang OCR.
    # 3. Neu parse text layer loi vi ly do khac thi de loi bubble len ingestion_service.
    #
    # Ly do dat logic nay trong pdf_parser:
    # - ingestion_service giu generic, khong can biet PDF/OCR la gi.
    # - parser_registry chi chon parser theo extension.
    # - sau nay sua heuristic OCR chi can sua quanh PDF parser.
    try:
        return parse_pdf(file_path, file_name)
    except ValueError as error:
        if str(error) != OCR_REQUIRED_MESSAGE:
            raise

        return parse_pdf_with_ocr(file_path, file_name)


def parse_pdf(file_path: str, file_name: str) -> ParsedDocument:
    # Muc tieu:
    # 1. Chi xu ly PDF co text layer trong Milestone 5.
    # 2. Doc tung page va giu marker Page N.
    # 3. Neu khong co text layer thi raise/return loi de C# set failed.
    #
    # Goi y:
    # - Dung pypdf PdfReader.
    # - page.extract_text() cho tung page.
    # - Cleanup nhe tung page, khong noi tat ca thanh mot dong.
    # - Output:
    #      Document: <file_name>
    #      File type: PDF
    #      Strategy: pdf text layer
    #      PDF pages: <n>
    #
    #      Page 1:
    #      ...
    # - metadata nen co pageCount, pagesWithText, emptyPages, likelyScanned.
    #
    # PDF scan/no text layer de Milestone 9 OCR.
    
    reader = PdfReader(file_path)
    page_count = len(reader.pages)
    
    lines = [
        f"Document: {file_name}",
        "File type: PDF",
        "Strategy: pdf text layer",
        f"PDF pages: {page_count}",
        "",
    ]
    pages_with_text = 0
    empty_pages = []
    for page_index, page in enumerate(reader.pages, start = 1):
        raw_text = page.extract_text() or ""
        page_text = cleanup_extracted_text(raw_text)
        
        if page_text :
            lines.append(f"Page {page_index}:")
            lines.append(page_text)
            lines.append("")
            pages_with_text += 1
        else:
            empty_pages.append(page_index)
            
    likely_scanned = pages_with_text == 0
    if likely_scanned:
        raise ValueError(OCR_REQUIRED_MESSAGE)
        
    extracted_text = "\n".join(lines)
    cleaned_text = cleanup_extracted_text(extracted_text)

    if not cleaned_text:
        raise ValueError("PDF file is empty.")
    
    return ParsedDocument(
        text=cleaned_text,
        parser_name="pdf_parser",
        character_count=len(cleaned_text),
        page_count=page_count,
        metadata={
            "strategy": "pdf_text_layer",
            "pageCount": page_count,
            "pagesWithText": pages_with_text,
            "emptyPages": empty_pages,
            "likelyScanned": likely_scanned,
        },
        warnings=[],
    )
