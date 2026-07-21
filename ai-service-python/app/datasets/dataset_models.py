from typing import Any

from pydantic import BaseModel, Field


class DatasetProfileRequest(BaseModel):
    # Duong dan file da duoc backend .NET upload vao storage.
    # Python chi doc file theo path, khong nhan upload truc tiep o milestone nay.
    documentId: str
    filePath: str
    fileName: str
    fileReferenceType: str = "local_path"
    fileReferenceValue: str = ""
    extension: str


class DatasetColumnProfile(BaseModel):
    # Ten cot goc trong CSV/XLSX.
    name: str
    # Ten cot sau khi normalize nhe, vi du bo khoang trang/lowercase.
    normalizedName: str
    # Kieu du lieu suy luan: string, number, datetime, boolean, mixed, empty.
    dataType: str
    nonNullCount: int = 0
    nullCount: int = 0


class DatasetTableProfile(BaseModel):
    # CSV co the dung sheetName = "default".
    # XLSX dung ten sheet that.
    sheetName: str
    tableIndex: int = 0
    rowCount: int = 0
    columnCount: int = 0
    columns: list[DatasetColumnProfile] = Field(default_factory=list)
    sampleRows: list[dict[str, Any]] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class DatasetProfileResponse(BaseModel):
    documentId: str
    success: bool
    profiles: list[DatasetTableProfile] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    errorMessage: str | None = None


class DatasetAnalysisRequest(BaseModel):
    documentId: str
    filePath: str
    fileName: str
    fileReferenceType: str = "local_path"
    fileReferenceValue: str = ""
    extension: str
    # Operation ban dau:
    # preview, list_columns, count, sum, average, group_by, top_n
    operation: str
    sheetName: str | None = None
    valueColumn: str | None = None
    groupByColumn: str | None = None
    topN: int = 10


class DatasetAnalysisResponse(BaseModel):
    documentId: str
    success: bool
    operation: str
    result: Any | None = None
    rowCount: int | None = None
    warnings: list[str] = Field(default_factory=list)
    errorMessage: str | None = None
