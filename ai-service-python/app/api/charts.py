from fastapi import APIRouter

from app.charts.chart_models import ChartRenderRequest, ChartRenderResponse
from app.charts.chart_service import render_chart


router = APIRouter(tags=["Charts"])


@router.post("/charts/render", response_model=ChartRenderResponse)
def render_chart_endpoint(request: ChartRenderRequest) -> ChartRenderResponse:
    # Endpoint de backend .NET goi sau khi da co analysis result.
    return render_chart(request)
