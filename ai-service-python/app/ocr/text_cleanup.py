import re


def cleanup_ocr_text(text: str) -> str:
    # Bai tap Milestone 9:
    # 1. Neu text rong thi return "".
    # 2. Chuan hoa newline: \r\n, \r -> \n.
    # 3. Chuan hoa nhieu space/tab thanh 1 space.
    # 4. Giam nhieu dong trong lien tiep thanh toi da 2 newline.
    # 5. Return text.strip().
    #
    # Luu y:
    # - Khong sua chinh ta tieng Viet bang rule manh trong Milestone 9.
    # - Khong gom tat ca dong thanh mot dong, vi chunking can cau truc doan.
    if not text:
        return ""

    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    normalized = re.sub(r"[^\S\n]+", " ", normalized)
    normalized = re.sub(r"\n{3,}", "\n\n", normalized)

    return normalized.strip()
