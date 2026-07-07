from fastapi import FastAPI

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

app.include_router(health_router)
app.include_router(ingestion_router)
app.include_router(chunking_router)
app.include_router(vector_router)
app.include_router(ocr_router)
app.include_router(datasets_router)
app.include_router(charts_router)
