from pydantic import BaseModel, Field


class VectorDeleteRequest(BaseModel):
    # Request xoa vector theo documentId.
    # ASP.NET Core goi endpoint nay khi purge document da bi soft delete qua retention.
    document_id: str = Field(alias="documentId")

    model_config = {
        "populate_by_name": True,
    }
