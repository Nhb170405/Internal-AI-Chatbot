from typing import Any

from pydantic import BaseModel, Field


class ChartRenderRequest(BaseModel):
    # Milestone 12 - Cach A:
    # Python chart service KHONG doc file goc va KHONG tinh toan dataset.
    # No chi nhan data da duoc DatasetAnalysisService tao ra va render thanh chart.
    chartType: str
    title: str | None = None
    data: list[dict[str, Any]] = Field(default_factory=list)
    xField: str | None = None
    yField: str | None = None


class ChartRenderResponse(BaseModel):
    success: bool
    chartType: str
    chartPath: str | None = None
    data: list[dict[str, Any]] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    errorMessage: str | None = None
