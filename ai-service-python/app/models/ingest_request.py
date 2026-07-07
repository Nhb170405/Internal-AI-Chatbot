from pydantic import BaseModel, Field


class IngestRequest(BaseModel):
    # Request do ASP.NET Core gui sang Python.
    # Dung alias camelCase de JSON contract giong C# DTO.
    document_id: str = Field(alias="documentId")
    file_path: str = Field(alias="filePath")
    file_name: str = Field(alias="fileName")
    content_type: str | None = Field(default=None, alias="contentType")
    extension: str

    model_config = {
        "populate_by_name": True,
    }
