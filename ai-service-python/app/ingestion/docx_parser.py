import re
from app.ingestion.parsed_document import ParsedDocument
from app.ingestion.text_cleanup import cleanup_extracted_text

from docx import Document
from docx.document import Document as DocxDocument
from docx.oxml.table import CT_Tbl
from docx.oxml.text.paragraph import CT_P
from docx.table import Table
from docx.text.paragraph import Paragraph

def parse_docx(file_path: str, file_name: str) -> ParsedDocument:
    # Muc tieu:
    # 1. Doc DOCX theo dung thu tu block trong document body.
    # 2. Paragraph va table phai giu dung thu tu xuat hien.
    # 3. Khong doc tat ca paragraphs truoc roi tables sau.
    #
    # Goi y:
    # - Dung python-docx.
    # - Viet helper iter_block_items(document) de yield paragraph/table theo order.
    # - Paragraph output:
    #      Paragraph:
    #      <text>
    # - Table output:
    #      Table N:
    #      Row R:
    #      Column C: <cell text>
    # - metadata nen co paragraphCount, tableCount, strategy.
    document = Document(file_path)
    
    lines = [
        f"Document: {file_name}",
        "File type: DOCX",
        "Strategy: docx block order",
        "",
    ]
    
    paragraph_count = 0
    table_count = 0
    non_empty_block_count = 0
    
    for block in iter_block_items(document):
        if isinstance(block, Paragraph) :
            text = block.text.strip()
            
            if not text:
                continue
            
            lines.append("Paragraph:")
            lines.append(text)
            lines.append("")
            
            paragraph_count += 1
            non_empty_block_count += 1

        elif isinstance(block, Table):
            table_lines = format_table(block,table_count + 1)

            if not table_lines:
                continue
            lines.extend(table_lines)
            table_count += 1
            non_empty_block_count += 1
            
    if non_empty_block_count == 0:
        raise ValueError("DOCX file is empty.")

    extracted_text = "\n".join(lines)
    cleaned_text = cleanup_extracted_text(extracted_text)

    if not cleaned_text:
        raise ValueError("DOCX file is empty.")

    return ParsedDocument(
        text=cleaned_text,
        parser_name="docx_parser",
        character_count=len(cleaned_text),
        page_count=None,
        metadata={
            "strategy": "docx_block_order",
            "paragraphCount": paragraph_count,
            "tableCount": table_count,
            "nonEmptyBlockCount": non_empty_block_count,
        },
        warnings=[],
    )
    
def iter_block_items(document: DocxDocument) :
    body = document.element.body

    for child in body.iterchildren():
        if isinstance(child, CT_P):
            yield Paragraph(child, document)
        elif isinstance(child, CT_Tbl):
            yield Table(child, document)

def format_table(table: Table, table_number: int) -> list[str]:
    lines = [f"Table {table_number}:"]
    row_count = 0

    for row in table.rows:
        cleaned_cells = []

        for cell in row.cells:
            cleaned_cells.append(normalize_cell_text(cell.text))

        if not any(cleaned_cells):
            continue

        row_count += 1
        lines.append(f"Row {row_count}:")

        for column_index, cell_text in enumerate(cleaned_cells, start=1):
            if cell_text:
                lines.append(f"Column {column_index}: {cell_text}")

        lines.append("")

    if row_count == 0:
        return []

    return lines

def normalize_cell_text(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()