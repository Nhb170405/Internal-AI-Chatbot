import os
from dataclasses import dataclass


@dataclass(frozen=True)
class AppSettings:
    # Cau hinh doc tu environment variables.
    # Khong hard-code API key vao source code.
    openai_api_key: str
    openai_embedding_model: str
    qdrant_url: str
    qdrant_collection: str
    qdrant_api_key: str

def get_settings() -> AppSettings:
    # Bai tap Milestone 7:
    # 1. Doc OPENAI_API_KEY tu os.environ.
    # 2. Doc OPENAI_EMBEDDING_MODEL, default "text-embedding-3-small".
    # 3. Doc QDRANT_URL, default "http://localhost:6333".
    # 4. Doc QDRANT_COLLECTION, default "internal_documents".
    # 5. Neu OPENAI_API_KEY rong thi raise ValueError voi message ro rang.
    #
    # Goi y:
    # api_key = os.environ.get("OPENAI_API_KEY", "").strip()
    # model = os.environ.get("OPENAI_EMBEDDING_MODEL", "text-embedding-3-small").strip()
    api_key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not api_key:
        raise ValueError("Missing OPENAI_API_KEY.")
    
    model = os.environ.get("OPENAI_EMBEDDING_MODEL", "text-embedding-3-small").strip()
    if not model:
        raise ValueError("missing model.")
    
    qdrant_url = os.environ.get("QDRANT_URL" , "http://localhost:6333")
    if not qdrant_url:
        raise ValueError("Missing QDRANT_URL.")
    
    qdrant_collection = os.environ.get("QDRANT_COLLECTION" , "internal_documents")
    if not qdrant_collection:
        raise ValueError("Missing QDRANT_COLLECTION.")
    
    qdrant_api_key = os.environ.get("QDRANT_API_KEY", "").strip()
    
    
    return AppSettings(
    openai_api_key=api_key,
    openai_embedding_model=model,
    qdrant_url=qdrant_url,
    qdrant_collection=qdrant_collection,
    qdrant_api_key=qdrant_api_key
)
