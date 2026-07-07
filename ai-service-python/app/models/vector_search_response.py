from typing import Any

from pydantic import BaseModel, Field


class VectorSearchHit(BaseModel):
    # Mot ket qua search tu Qdrant.
    score: float
    document_id: str = Field(alias="documentId")
    chunk_id: str = Field(alias="chunkId")
    chunk_index: int = Field(alias="chunkIndex")
    content: str
    payload: dict[str, Any] = Field(default_factory=dict)

    model_config = {
        "populate_by_name": True,
    }


class VectorSearchResponse(BaseModel):
    success: bool
    hits: list[VectorSearchHit] = Field(default_factory=list)
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }
