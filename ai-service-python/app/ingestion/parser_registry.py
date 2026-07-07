from app.ingestion.csv_parser import parse_csv
from app.ingestion.docx_parser import parse_docx
from app.ingestion.pdf_parser import parse_pdf_auto
from app.ingestion.txt_parser import parse_txt
from app.ingestion.xlsx_parser import parse_xlsx
from app.ingestion.parsed_document import ParsedDocument


def parse_by_extension(file_path: str, file_name: str, extension: str) -> ParsedDocument:
    # Muc tieu:
    # 1. Normalize extension ve lowercase.
    # 2. Chon parser phu hop.
    # 3. Neu extension chua support thi throw ValueError.
    normalized = extension.strip().lower()

    if normalized == ".txt":
        return parse_txt(file_path, file_name)

    if normalized == ".csv":
        return parse_csv(file_path, file_name)

    if normalized == ".xlsx":
        return parse_xlsx(file_path, file_name)

    if normalized == ".docx":
        return parse_docx(file_path, file_name)

    if normalized == ".pdf":
        return parse_pdf_auto(file_path, file_name)

    raise ValueError(f"Unsupported extension: {extension}")
