from typing import Any

from pydantic import BaseModel, Field


class IngestResponse(BaseModel):
    # Response chuan ma moi parser can tra ve cho ASP.NET Core.
    document_id: str = Field(alias="documentId")
    success: bool
    parser_name: str = Field(alias="parserName")
    extracted_text: str = Field(alias="extractedText")
    character_count: int = Field(alias="characterCount")
    page_count: int | None = Field(default=None, alias="pageCount")
    metadata: dict[str, Any] = Field(default_factory=dict)
    warnings: list[str] = Field(default_factory=list)
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }
