from fastapi import APIRouter
from pydantic import BaseModel, Field

from app.ocr.ocr_service import ocr_image_file

router = APIRouter(tags=["OCR"])


class OcrImageRequest(BaseModel):
    image_path: str = Field(alias="imagePath")
    language: str = "vie+eng"

    model_config = {
        "populate_by_name": True,
    }


class OcrImageResponse(BaseModel):
    success: bool
    text: str
    character_count: int = Field(alias="characterCount")
    warnings: list[str] = []
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }


@router.post("/ocr/image", response_model=OcrImageResponse)
def ocr_image(request: OcrImageRequest) -> OcrImageResponse:
    # Endpoint test nhanh OCR anh don.
    # Milestone 9 nen test endpoint nay truoc khi OCR PDF scan.
    try:
        result = ocr_image_file(
            image_path=request.image_path,
            language=request.language,
        )

        return OcrImageResponse(
            success=True,
            text=result.text,
            characterCount=result.character_count,
            warnings=result.warnings,
            errorMessage=None,
        )
    except Exception as error:
        return OcrImageResponse(
            success=False,
            text="",
            characterCount=0,
            warnings=[],
            errorMessage=str(error),
        )
