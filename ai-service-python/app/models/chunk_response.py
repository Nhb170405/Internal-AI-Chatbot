from typing import Any

from pydantic import BaseModel, Field


class ChunkItemResponse(BaseModel):
    # Mot chunk nho duoc tao tu extracted text.
    # chunkIndex phai tang deu va on dinh de ve sau mapping citation.
    chunk_index: int = Field(alias="chunkIndex")
    content: str
    character_count: int = Field(alias="characterCount")
    start_offset: int | None = Field(default=None, alias="startOffset")
    end_offset: int | None = Field(default=None, alias="endOffset")
    metadata: dict[str, Any] = Field(default_factory=dict)

    model_config = {
        "populate_by_name": True,
    }


class ChunkResponse(BaseModel):
    # Response chuan cho ASP.NET Core.
    # success=false dung cho loi nghiep vu co kiem soat, vi du text rong/config sai.
    document_id: str = Field(alias="documentId")
    success: bool
    chunk_count: int = Field(alias="chunkCount")
    chunks: list[ChunkItemResponse] = Field(default_factory=list)
    metadata: dict[str, Any] = Field(default_factory=dict)
    warnings: list[str] = Field(default_factory=list)
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }
