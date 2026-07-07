from fastapi import APIRouter

from app.datasets.dataset_analysis_service import analyze_dataset
from app.datasets.dataset_models import (
    DatasetAnalysisRequest,
    DatasetAnalysisResponse,
    DatasetProfileRequest,
    DatasetProfileResponse,
)
from app.datasets.dataset_profiler import profile_dataset


router = APIRouter(tags=["Datasets"])


@router.post("/datasets/profile", response_model=DatasetProfileResponse)
def profile_dataset_endpoint(request: DatasetProfileRequest) -> DatasetProfileResponse:
    # Endpoint de test truc tiep Python bang Swagger.
    # .NET se goi endpoint nay qua PythonDatasetClient.
    return profile_dataset(request)


@router.post("/datasets/analyze", response_model=DatasetAnalysisResponse)
def analyze_dataset_endpoint(request: DatasetAnalysisRequest) -> DatasetAnalysisResponse:
    # Endpoint phan tich dataset bang pandas.
    # Operation ban dau: preview/list_columns/count/sum/average/group_by/top_n.
    return analyze_dataset(request)
