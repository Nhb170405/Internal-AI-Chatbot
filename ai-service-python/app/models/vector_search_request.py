from pydantic import BaseModel, Field


class VectorSearchRequest(BaseModel):
    # Request search semantic.
    # allowedAccessLevels do C# tinh tu role cua user roi gui sang Python.
    query: str
    top_k: int = Field(default=5, alias="topK")
    allowed_access_levels: list[str] = Field(alias="allowedAccessLevels")

    model_config = {
        "populate_by_name": True,
    }
