from pydantic import BaseModel, Field


class ChunkRequest(BaseModel):
    # Request do ASP.NET Core gui sang Python de chia extracted text thanh chunks.
    # Khac voi /ingest: endpoint nay khong doc file tu disk nua, ma nhan text da extract.
    document_id: str = Field(alias="documentId")
    text: str
    chunk_size: int = Field(default=1200, alias="chunkSize")
    chunk_overlap: int = Field(default=150, alias="chunkOverlap")

    model_config = {
        "populate_by_name": True,
    }
