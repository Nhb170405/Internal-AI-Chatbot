from app.ingestion.parsed_document import ParsedDocument
from app.ingestion.text_cleanup import cleanup_extracted_text


def parse_txt(file_path: str, file_name: str) -> ParsedDocument:
    # Muc tieu:
    # 1. Doc file txt voi nhieu encoding de ho tro tieng Viet/tieng Anh.
    # 2. Thu lan luot: utf-8, utf-8-sig, cp1258, latin-1.
    # 3. Cleanup nhe bang cleanup_extracted_text.
    # 4. Return ParsedDocument.
    #
    # Ket qua text nen co header:
    # Document: <file_name>
    # File type: TXT
    #
    # Sau do la noi dung file.
    encodings = ["utf-8", "utf-8-sig", "cp1258", "latin-1"]
    
    for encoding in encodings:
        try:
            with open(file_path, "r", encoding=encoding) as file:
                raw_text = file.read()
            if not raw_text.strip():
                raise ValueError("TXT file is empty.")
            extracted_text = f"""Document: {file_name}
File type: TXT
Encoding: {encoding}

{raw_text}
"""
            cleaned_text = cleanup_extracted_text(extracted_text)
            
            if not cleaned_text :
                raise ValueError("TXT file is empty.")
            
            return ParsedDocument (
                text=cleaned_text,
                parser_name="txt_parser",
                character_count=len(cleaned_text),
                page_count=None,
                metadata={
                    "strategy": "plain_text",
                    "encoding": encoding,
                },
                warnings=[],
            )
        except UnicodeDecodeError:
            continue
        
    raise ValueError("Cannot decode TXT file with supported encodings.")