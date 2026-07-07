
import csv 

from app.ingestion.parsed_document import ParsedDocument
from app.ingestion.text_cleanup import cleanup_extracted_text

def parse_csv(file_path: str, file_name: str) -> ParsedDocument:
    # Muc tieu:
    # 1. Doc CSV theo huong an toan: raw row/cell matrix.
    # 2. Khong mac dinh dong dau la header.
    # 3. Ho tro encoding: utf-8, utf-8-sig, cp1258, latin-1.
    # 4. Moi row output dang:
    #    Row N:
    #    Column 1: ...
    #    Column 2: ...
    #
    # Goi y:
    # - Co the dung csv module de doc raw rows.
    # - Bo qua row rong hoac ghi row rong thanh separator tuy ban chon.
    # - metadata nen co rowCount, maxColumnCount, encoding, strategy.
    #
    # Khong can detect header/table regions trong Milestone 5.
    
    encodings = ["utf-8", "utf-8-sig", "cp1258", "latin-1"]
    
    for encoding in encodings:
        try:
            with open(file_path, "r", encoding=encoding,newline="") as file:
                reader = csv.reader(file)
                rows = list(reader)
            
            lines = [
                f"Document: {file_name}",
                "File type: CSV",
                "Strategy: raw row/cell matrix",
                f"Encoding: {encoding}",
                "",]
            row_count = 0
            max_column_count = 0
            
            for row_index, row in enumerate(rows,start=1):
                cleaned_cells = [cell.strip() for cell in row]
                if not any(cleaned_cells):
                    continue
                lines.append(f"Row {row_index}:")
                row_count += 1
                max_column_count = max(max_column_count,len(cleaned_cells))

                for column_index, cell in enumerate(cleaned_cells, start=1):
                    if cell:
                        lines.append(f"Column {column_index}: {cell}")

                lines.append("")                
                
                if row_count == 0:
                    raise ValueError("CSV file is empty.")
                
                extracted_text = "\n".join(lines)
                cleaned_text = cleanup_extracted_text(extracted_text)

                if not cleaned_text:
                    raise ValueError("CSV file is empty")
                
            return ParsedDocument(
            text=cleaned_text,
            parser_name="csv_parser",
            character_count=len(cleaned_text),                
            page_count=None,
            metadata={
                "strategy": "raw_row_cell_matrix",
                "rowCount": row_count,
                "maxColumnCount": max_column_count,
                "encoding": encoding,
            },
            warnings=[],
            )
        except UnicodeDecodeError:
            continue
    raise ValueError("Cannot decode CSV file with supported encodings.")