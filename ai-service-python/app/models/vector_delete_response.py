from pydantic import BaseModel, Field


class VectorDeleteResponse(BaseModel):
    # Response cho C# biet viec xoa vector trong Qdrant co thanh cong khong.
    document_id: str = Field(alias="documentId")
    success: bool
    collection_name: str = Field(alias="collectionName")
    deleted_count: int = Field(alias="deletedCount")
    error_message: str | None = Field(default=None, alias="errorMessage")

    model_config = {
        "populate_by_name": True,
    }
