from typing import Any

from pydantic import BaseModel, Field


class VectorChunkInput(BaseModel):
    # Mot chunk do ASP.NET Core doc tu SQL va gui sang Python de tao embedding.
    chunk_id: str = Field(alias="chunkId")
    chunk_index: int = Field(alias="chunkIndex")
    content: str
    metadata: dict[str, Any] = Field(default_factory=dict)

    model_config = {
        "populate_by_name": True,
    }


class VectorIndexRequest(BaseModel):
    # Request index mot document da chunk vao Qdrant.
    document_id: str = Field(alias="documentId")
    original_file_name: str = Field(alias="originalFileName")
    access_level: str = Field(alias="accessLevel")
    document_status: str = Field(alias="documentStatus")
    chunks: list[VectorChunkInput]

    model_config = {
        "populate_by_name": True,
    }
