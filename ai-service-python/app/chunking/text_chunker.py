from app.chunking.chunk_models import TextChunk


def chunk_text(text: str, chunk_size: int, chunk_overlap: int) -> list[TextChunk]:
    # Bai tap Milestone 6:
    # 1. Validate:
    #    - text khong rong sau khi strip.
    #    - chunk_size > 0.
    #    - chunk_overlap >= 0.
    #    - chunk_overlap < chunk_size.
    # 2. Normalize line endings: \r\n va \r thanh \n.
    # 3. Tach text thanh paragraph dua tren dong trong.
    # 4. Gom paragraph vao current chunk cho den khi gan chunk_size.
    # 5. Neu mot paragraph qua dai:
    #    - cat mem theo dau cau hoac khoang trang gan chunk_size.
    #    - tranh cat giua tu neu co the.
    # 6. Neu chunk_overlap > 0:
    #    - lay phan cuoi chunk truoc lam overlap cho chunk sau.
    #    - dung overlap vua du, khong de chunk sau chi toan overlap.
    # 7. Tao TextChunk:
    #    - chunk_index bat dau tu 0.
    #    - content khong rong.
    #    - start_offset/end_offset neu ban tinh duoc.
    #    - metadata co the luu strategy = "paragraph_character".
    # 8. Return list[TextChunk].
    #
    # Goi y cho nguoi moi:
    # - Lam ban don gian truoc: paragraph-aware, character-based.
    # - Chua can token-based chunking.
    # - Chua can AI semantic chunking.
    parts = []

    _validate_input(text.strip(), chunk_size, chunk_overlap)
    
    paragraphs = _split_paragraphs(text.strip())
    
    for paragraph in paragraphs:
        if len(paragraph) <= chunk_size:
            parts.append(paragraph)
        else:
            parts.extend(_split_long_paragraph(paragraph, chunk_size))
            
    return _build_chunks(parts, chunk_size, chunk_overlap)



"""""""""""""""""""""""""""""""""
             Helper
"""""""""""""""""""""""""""""""""

def _validate_input(text: str, chunk_size: int, chunk_overlap: int) -> None:
    if not text.strip() or chunk_size <= 0 or chunk_overlap < 0 or chunk_overlap >= chunk_size:
        raise ValueError("Text is empty.")
    
def _split_paragraphs(text: str) -> list[str]:
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    raw_parts = normalized.split("\n\n")
    
    paragraphs = []

    for part in raw_parts:
        cleaned = part.strip()
        if cleaned:
            paragraphs.append(cleaned)

    return paragraphs
    
def _split_long_paragraph(paragraph: str, chunk_size: int) -> list[str]:
    start = 0
    paragraphs = []
    
    while start < len(paragraph):
        end = start + chunk_size
        if end > len(paragraph):
            paragraphs.append(paragraph[start:len(paragraph)].strip())
            break

        cut = _find_cut_position(paragraph, start, end)
        if cut <= start : 
            cut = end
            
        paragraphs.append(paragraph[start:cut].strip())
        start = cut
        
        while start < len(paragraph) and paragraph[start].isspace():
            start += 1
        
    return paragraphs
                
    
def _build_chunks(parts: list[str], chunk_size: int, chunk_overlap: int) -> list[TextChunk]:
    chunks: list[TextChunk] = []
    current_parts: list[str] = []
    current_length = 0
    
    for part in parts:
        cleaned_part = part.strip()
        if not cleaned_part:
            continue
        
        separator_length = 2 if current_parts else 0
        
        next_length = current_length + separator_length + len(cleaned_part)

        if current_parts and next_length > chunk_size:
            content = "\n\n".join(current_parts).strip()

            chunks.append(TextChunk(
                chunk_index=len(chunks),
                content=content,
                start_offset=None,
                end_offset=None,
                metadata={
                    "strategy": "paragraph_character"
                }
            ))

            overlap_text = _get_overlap_text(content, chunk_overlap)

            current_parts = []
            current_length = 0

            if overlap_text:
                current_parts.append(overlap_text)
                current_length = len(overlap_text)

        separator_length = 2 if current_parts else 0
        current_parts.append(cleaned_part)
        current_length = current_length + separator_length + len(cleaned_part)

    if current_parts:
        content = "\n\n".join(current_parts).strip()

        if content:
            chunks.append(TextChunk(
                chunk_index=len(chunks),
                content=content,
                start_offset=None,
                end_offset=None,
                metadata={
                    "strategy": "paragraph_character"
                }
            ))

    return chunks
    
    
        
def _get_overlap_text(content: str, chunk_overlap: int) -> str:
    if chunk_overlap <= 0:
        return ""

    cleaned = content.strip()
    if not cleaned:
        return ""

    if len(cleaned) <= chunk_overlap:
        return cleaned

    start = len(cleaned) - chunk_overlap
    overlap = cleaned[start:].strip()

    newline_index = overlap.find("\n")
    if newline_index != -1:
        overlap = overlap[newline_index + 1:].strip()

    space_index = overlap.find(" ")
    if space_index != -1:
        first_word = overlap[:space_index].strip()
        if len(first_word) <= 2:
            overlap = overlap[space_index + 1:].strip()

    if len(overlap) < 10:
        return ""

    return overlap
    

def _find_cut_position(text: str, start: int, end: int) -> int:
    punctuation_marks = [".", "!", "?", ":", ";"]

    best_cut = -1

    for mark in punctuation_marks:
        position = text.rfind(mark, start, end)
        if position > best_cut:
            best_cut = position

    if best_cut > start:
        return best_cut + 1

    newline_cut = text.rfind("\n", start, end)
    if newline_cut > start:
        return newline_cut

    space_cut = text.rfind(" ", start, end)
    if space_cut > start:
        return space_cut

    return end