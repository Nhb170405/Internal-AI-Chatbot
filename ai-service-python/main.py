import os
import secrets

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.api.health import router as health_router
from app.api.ingestion import router as ingestion_router
from app.api.chunking import router as chunking_router
from app.api.vector import router as vector_router
from app.api.ocr import router as ocr_router
from app.api.datasets import router as datasets_router
from app.api.charts import router as charts_router


app = FastAPI(
    title="Factory Chatbot AI Service",
    version="0.1.0",
)


@app.middleware("http")
async def require_internal_api_key(request: Request, call_next):
    if request.url.path == "/health":
        return await call_next(request)

    expected_api_key = os.environ.get("PYTHON_SERVICE_API_KEY", "").strip()
    provided_api_key = request.headers.get("X-Internal-Api-Key", "")

    if not expected_api_key:
        return JSONResponse(
            status_code=503,
            content={"detail": "Python service API key is not configured."},
        )

    if not secrets.compare_digest(provided_api_key, expected_api_key):
        return JSONResponse(
            status_code=401,
            content={"detail": "Invalid internal API key."},
        )

    return await call_next(request)


app.include_router(health_router)
app.include_router(ingestion_router)
app.include_router(chunking_router)
app.include_router(vector_router)
app.include_router(ocr_router)
app.include_router(datasets_router)
app.include_router(charts_router)
