from datetime import date, datetime
import pandas as pd
import math
import numpy as np
import os


from app.datasets.dataset_models import DatasetAnalysisRequest, DatasetAnalysisResponse


def analyze_dataset(request: DatasetAnalysisRequest) -> DatasetAnalysisResponse:
    # Bai tap Milestone 11:
    # 1. Validate filePath ton tai va extension hop le.
    # 2. Doc file thanh pandas DataFrame:
    #    - CSV: read_csv.
    #    - XLSX/XLS: read_excel voi sheetName neu co, neu khong lay sheet dau tien.
    # 3. Normalize operation ve lowercase.
    # 4. Xu ly cac operation ban dau:
    #    - preview: tra df.head(topN) dang list dict.
    #    - list_columns: tra danh sach cot.
    #    - count: tra so dong.
    #    - sum: can valueColumn, cot phai numeric.
    #    - average: can valueColumn, cot phai numeric.
    #    - group_by: can groupByColumn va valueColumn, tinh sum theo group.
    #    - top_n: can valueColumn, sort desc lay topN.
    # 5. Neu thieu cot/operation sai thi return success=false voi errorMessage ro.
    # 6. Khong goi OpenAI trong file nay. Pandas la noi tinh toan.
    #
    # Goi y:
    # df[request.valueColumn].sum()
    # df.groupby(request.groupByColumn)[request.valueColumn].sum()
    try: 
        _validate_response = _validate_basic_request(request)
        if _validate_response is not None:
            return _validate_response
        
        extension = request.extension.lower().strip()
        if (extension in [".xlsx", ".xls"]) :
            df = pd.read_excel(request.filePath, sheet_name=request.sheetName if request.sheetName else 0)
        elif (extension == ".csv"):
            df = pd.read_csv(request.filePath)
        
        value_column = _find_column(df, request.valueColumn)
        group_by_column = _find_column(df, request.groupByColumn)
        requested_value_column = request.valueColumn.strip() if request.valueColumn else None
        requested_group_by_column = request.groupByColumn.strip() if request.groupByColumn else None
        series = pd.to_numeric(df[value_column], errors="coerce") if value_column is not None else pd.Series(dtype=float)
        operation = request.operation.lower().strip()
        if operation == "preview":
            result = dataframe_sample_rows(df, limit=_normalize_top_n(request.topN))
        elif operation == "list_columns":
            result = [str(column) for column in df.columns]
        elif operation == "count":  
            result = len(df)
        elif operation == "sum":
            if value_column is None:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Column '{requested_value_column}' does not exist in the dataset.")
            if series.notna().sum() == 0:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Column '{value_column}' is not numeric.")
            result = float(series.sum())
        elif operation == "average":
            if value_column is None:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Column '{requested_value_column}' does not exist in the dataset.")
            if series.notna().sum() == 0:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Column '{value_column}' is not numeric.")
            result = float(series.mean())
        elif operation == "group_by":
            if group_by_column is None:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Group by column '{requested_group_by_column}' does not exist in the dataset.")
            if value_column is None:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Value column '{requested_value_column}' does not exist in the dataset.")
            if series.notna().sum() == 0:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Value column '{value_column}' is not numeric.")
            working_df = df[[group_by_column]].copy()
            working_df[value_column] = series
            grouped_df = working_df.groupby(group_by_column, dropna=False)[value_column].sum().reset_index().sort_values(group_by_column, ascending=True)
            result = dataframe_sample_rows(grouped_df, limit=_normalize_top_n(request.topN))
        elif operation == "top_n":
            if value_column is None:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Value column '{requested_value_column}' does not exist in the dataset.")
            if series.notna().sum() == 0:
                return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Value column '{value_column}' is not numeric.")
            working_df = df.copy()
            working_df["_numeric_value"] = series
            top_n_df = working_df.sort_values("_numeric_value", ascending=False).head(_normalize_top_n(request.topN))
            top_n_df = top_n_df.drop(columns=["_numeric_value"])
            result = dataframe_sample_rows(top_n_df, limit=_normalize_top_n(request.topN))
        else:
            return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=f"Invalid operation '{request.operation}'.")
        
        return DatasetAnalysisResponse(documentId=request.documentId, success=True, operation=request.operation, result=result,rowCount=int(len(df)),warnings = [], errorMessage=None)
    except Exception as e:
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage=str(e))

# Helper functions
def _validate_basic_request(request):
    if (request.filePath is None or request.filePath == ""):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="filePath is required")
    
    value_column = request.valueColumn.strip() if request.valueColumn else None
    group_by_column = request.groupByColumn.strip() if request.groupByColumn else None

    if not os.path.exists(request.filePath):
        return DatasetAnalysisResponse(
            documentId=request.documentId,
            success=False,
            operation=request.operation or "",
            result=None,
            rowCount=None,
            warnings=[],
            errorMessage="File path does not exist."
        )
    if (request.operation is None or request.operation == ""):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="operation is required")
    
    if (request.operation.lower().strip() in ["preview", "top_n"] and (request.topN is None or request.topN <= 0)):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="topN must be a positive integer for preview and top_n operations.")
    
    if (request.operation.lower().strip() in ["sum", "average", "group_by", "top_n"] and (value_column  is None or value_column == "")):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="valueColumn is required for sum, average, group_by, and top_n operations.")
    
    if (request.operation.lower().strip() == "group_by" and (group_by_column is None or group_by_column == "")):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="groupByColumn is required for group_by operation.")
    
    extension = (request.extension or "").lower().strip()
    if (extension not in [".csv", ".xlsx", ".xls"]):
        return DatasetAnalysisResponse(documentId=request.documentId, success=False, operation=request.operation, result=None,rowCount=None,warnings = [], errorMessage="Invalid file extension. Only .csv, .xlsx, .xls are allowed.")
    
    return None

def _find_column(df, requested_column: str | None):
    if requested_column is None or requested_column.strip() == "":
        return None

    normalized_request = requested_column.strip().lower()

    for column in df.columns:
        column_name = str(column).strip()
        if column_name.lower() == normalized_request:
            return column

    return None

def to_json_safe_value(value):
    if pd.isna(value):
        return None

    if isinstance(value, (datetime, date)):
        return value.isoformat()

    if isinstance(value, np.integer):
        return int(value)

    if isinstance(value, np.floating):
        if math.isnan(float(value)):
            return None
        return float(value)

    if isinstance(value, np.bool_):
        return bool(value)

    return value

def dataframe_sample_rows(df, limit: int = 5):
    records = df.head(limit).to_dict(orient="records")

    safe_records = []
    for row in records:
        safe_row = {}
        for key, value in row.items():
            safe_row[str(key)] = to_json_safe_value(value)
        safe_records.append(safe_row)

    return safe_records

def _normalize_top_n(top_n: int) -> int:
    if top_n is None or top_n <= 0:
        return 10
    return min(top_n, 100)
