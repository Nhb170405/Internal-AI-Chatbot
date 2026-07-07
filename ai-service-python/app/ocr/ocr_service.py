from pathlib import Path

from app.ocr.image_preprocess import preprocess_for_ocr
from app.ocr.ocr_models import OcrPageResult
from app.ocr.text_cleanup import cleanup_ocr_text


def ocr_image_file(image_path: str, page_number: int = 1, language: str = "vie+eng") -> OcrPageResult:
    # Bai tap Milestone 9:
    # 1. Validate image_path ton tai va la file.
    # 2. Import PIL.Image va pytesseract trong ham nay.
    #    Ly do: neu package chua cai, app van import duoc module skeleton.
    # 3. Mo image bang Image.open(image_path).
    # 4. Goi preprocess_for_ocr(image).
    # 5. Goi pytesseract.image_to_string(processed_image, lang=language).
    # 6. Cleanup text bang cleanup_ocr_text.
    # 7. Return OcrPageResult.
    #
    # Goi y syntax:
    # from PIL import Image
    # import pytesseract
    # with Image.open(path) as image:
    #     processed = preprocess_for_ocr(image)
    #     raw_text = pytesseract.image_to_string(processed, lang=language)
    #
    # Luu y:
    # - Neu may chua co language data "vie", Tesseract se bao loi.
    # - Khi do co the test tam voi language="eng".
    
    from PIL import Image
    import pytesseract
    
    path = Path(image_path)

    if not path.exists():
        raise ValueError("Image path does not exist.")

    if not path.is_file():
        raise ValueError("Image path is not a file.")

    with Image.open(path) as image:
        processed_image = preprocess_for_ocr(image)
        raw_text = pytesseract.image_to_string(processed_image, lang=language)

    cleaned_text = cleanup_ocr_text(raw_text)

    return OcrPageResult(
        page_number=page_number,
        text=cleaned_text,
        character_count=len(cleaned_text),
        warnings=[],
    )