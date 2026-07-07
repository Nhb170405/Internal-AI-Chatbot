from typing import Any


def preprocess_for_ocr(image: Any) -> Any:
    # Bai tap Milestone 9:
    # Ham nay nhan PIL Image va tra ve PIL Image da xu ly nhe.
    #
    # Ban dau nen lam rat nhe:
    # 1. Convert image sang grayscale: image.convert("L").
    # 2. Return image.
    #
    # Chua nen lam:
    # - threshold manh.
    # - deskew.
    # - denoise phuc tap.
    # - sharpen qua tay.
    #
    # Ly do:
    # - Preprocess qua manh co the lam mat dau tieng Viet hoac lam hong anh scan tot.
    # - Milestone 9 uu tien pipeline OCR chay duoc truoc.
    
    return image.convert("L")