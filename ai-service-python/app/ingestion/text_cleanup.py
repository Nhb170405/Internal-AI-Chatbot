import re


def cleanup_extracted_text(text: str) -> str:
    # Muc tieu:
    # 1. Cleanup nhe de text gon hon nhung khong lam mat y nghia.
    # 2. Giu dau tieng Viet, punctuation, paragraph/page markers.
    # 3. Khong noi tat ca dong thanh mot dong.
    #
    # Goi y logic:
    # - doi \r\n va \r thanh \n.
    # - trim tung line.
    # - giam nhieu hon 2 dong trong lien tiep thanh 2 dong trong.
    # - strip dau/cuoi.
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    lines = [line.strip() for line in normalized.split("\n")]
    compacted = "\n".join(lines)
    compacted = re.sub(r"\n{3,}", "\n\n", compacted)
    return compacted.strip()
