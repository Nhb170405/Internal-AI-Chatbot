from pydantic import BaseModel, Field


class VectorIndexResponse(BaseModel):
    # Response cho ASP.NET Core biet index thanh cong hay that bai.
    document_id: str = Field(alias="documentId")
    success: bool
    collection_name: str = Field(alias="collectionName")
    indexed_count: int = Field(alias="indexedCount")
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }
