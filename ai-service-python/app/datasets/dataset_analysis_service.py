from datetime import date, datetime
import math
from pathlib import Path

import numpy as np
import pandas as pd

from app.datasets.dataset_models import DatasetAnalysisRequest, DatasetAnalysisResponse
from app.ingestion.file_reference_resolver import (
    cleanup_resolved_file,
    resolve_file_reference,
)


def analyze_dataset(request: DatasetAnalysisRequest) -> DatasetAnalysisResponse:
    """Run deterministic analysis over an entire CSV/XLS/XLSX dataset.

    The backend may provide either a local path or a short-lived Azure Blob SAS
    URL. The shared resolver converts both forms to a local path that pandas can
    read. Any temporary download is always removed in the finally block.
    """
    validation_response = _validate_basic_request(request)
    if validation_response is not None:
        return validation_response

    try:
        resolved = resolve_file_reference(
            file_reference_type=request.fileReferenceType,
            file_reference_value=request.fileReferenceValue,
            legacy_file_path=request.filePath,
            extension=request.extension,
        )
    except ValueError as error:
        return _error_response(request, str(error))
    except Exception as error:
        # Do not return the original exception text because it may contain a SAS URL.
        return _error_response(
            request,
            f"File reference could not be resolved: {type(error).__name__}.",
        )

    file_path = Path(resolved.file_path)

    try:
        if not file_path.exists():
            return _error_response(request, "Resolved file path does not exist.")

        if not file_path.is_file():
            return _error_response(request, "Resolved file path is not a file.")

        extension = request.extension.lower().strip()
        if extension in [".xlsx", ".xls"]:
            df = pd.read_excel(
                str(file_path),
                sheet_name=request.sheetName if request.sheetName else 0,
            )
        else:
            df = pd.read_csv(str(file_path))

        return _analyze_dataframe(request, df)
    except Exception as error:
        return _error_response(request, f"Dataset analysis failed: {str(error)}")
    finally:
        cleanup_resolved_file(resolved)


def _analyze_dataframe(
    request: DatasetAnalysisRequest,
    df: pd.DataFrame,
) -> DatasetAnalysisResponse:
    """Apply one validated operation to the complete dataframe."""
    operation = request.operation.lower().strip()
    value_column = _find_column(df, request.valueColumn)
    group_by_column = _find_column(df, request.groupByColumn)
    requested_value_column = request.valueColumn.strip() if request.valueColumn else None
    requested_group_by_column = (
        request.groupByColumn.strip() if request.groupByColumn else None
    )
    series = (
        pd.to_numeric(df[value_column], errors="coerce")
        if value_column is not None
        else pd.Series(dtype=float)
    )

    if operation == "preview":
        result = dataframe_sample_rows(df, limit=_normalize_top_n(request.topN))
    elif operation == "list_columns":
        result = [str(column) for column in df.columns]
    elif operation == "count":
        result = len(df)
    elif operation == "sum":
        if value_column is None:
            return _error_response(
                request,
                f"Column '{requested_value_column}' does not exist in the dataset.",
            )
        if series.notna().sum() == 0:
            return _error_response(request, f"Column '{value_column}' is not numeric.")
        result = float(series.sum())
    elif operation == "average":
        if value_column is None:
            return _error_response(
                request,
                f"Column '{requested_value_column}' does not exist in the dataset.",
            )
        if series.notna().sum() == 0:
            return _error_response(request, f"Column '{value_column}' is not numeric.")
        result = float(series.mean())
    elif operation == "group_by":
        if group_by_column is None:
            return _error_response(
                request,
                f"Group by column '{requested_group_by_column}' does not exist in the dataset.",
            )
        if value_column is None:
            return _error_response(
                request,
                f"Value column '{requested_value_column}' does not exist in the dataset.",
            )
        if series.notna().sum() == 0:
            return _error_response(
                request,
                f"Value column '{value_column}' is not numeric.",
            )

        working_df = df[[group_by_column]].copy()
        working_df[value_column] = series
        grouped_df = (
            working_df.groupby(group_by_column, dropna=False)[value_column]
            .sum()
            .reset_index()
            .sort_values(group_by_column, ascending=True)
        )
        result = dataframe_sample_rows(
            grouped_df,
            limit=_normalize_top_n(request.topN),
        )
    elif operation == "top_n":
        if value_column is None:
            return _error_response(
                request,
                f"Value column '{requested_value_column}' does not exist in the dataset.",
            )
        if series.notna().sum() == 0:
            return _error_response(
                request,
                f"Value column '{value_column}' is not numeric.",
            )

        working_df = df.copy()
        working_df["_numeric_value"] = series
        top_n_df = (
            working_df.sort_values("_numeric_value", ascending=False)
            .head(_normalize_top_n(request.topN))
            .drop(columns=["_numeric_value"])
        )
        result = dataframe_sample_rows(
            top_n_df,
            limit=_normalize_top_n(request.topN),
        )
    else:
        return _error_response(
            request,
            f"Invalid operation '{request.operation}'.",
        )

    return DatasetAnalysisResponse(
        documentId=request.documentId,
        success=True,
        operation=operation,
        result=result,
        rowCount=int(len(df)),
        warnings=[],
        errorMessage=None,
    )


def _validate_basic_request(
    request: DatasetAnalysisRequest,
) -> DatasetAnalysisResponse | None:
    """Validate operation arguments without assuming where the file is stored."""
    operation = (request.operation or "").lower().strip()
    value_column = request.valueColumn.strip() if request.valueColumn else None
    group_by_column = request.groupByColumn.strip() if request.groupByColumn else None

    if not operation:
        return _error_response(request, "operation is required")

    supported_operations = {
        "preview",
        "list_columns",
        "count",
        "sum",
        "average",
        "group_by",
        "top_n",
    }
    if operation not in supported_operations:
        return _error_response(
            request,
            f"Invalid operation '{request.operation}'.",
        )

    if operation in ["preview", "top_n"] and (
        request.topN is None or request.topN <= 0
    ):
        return _error_response(
            request,
            "topN must be a positive integer for preview and top_n operations.",
        )

    if operation in ["sum", "average", "group_by", "top_n"] and not value_column:
        return _error_response(
            request,
            "valueColumn is required for sum, average, group_by, and top_n operations.",
        )

    if operation == "group_by" and not group_by_column:
        return _error_response(
            request,
            "groupByColumn is required for group_by operation.",
        )

    extension = (request.extension or "").lower().strip()
    if extension not in [".csv", ".xlsx", ".xls"]:
        return _error_response(
            request,
            "Invalid file extension. Only .csv, .xlsx, .xls are allowed.",
        )

    return None


def _error_response(
    request: DatasetAnalysisRequest,
    message: str,
) -> DatasetAnalysisResponse:
    return DatasetAnalysisResponse(
        documentId=request.documentId,
        success=False,
        operation=request.operation or "",
        result=None,
        rowCount=None,
        warnings=[],
        errorMessage=message,
    )


def _find_column(df: pd.DataFrame, requested_column: str | None):
    if requested_column is None or requested_column.strip() == "":
        return None

    normalized_request = requested_column.strip().lower()
    for column in df.columns:
        if str(column).strip().lower() == normalized_request:
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


def dataframe_sample_rows(df: pd.DataFrame, limit: int = 5):
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
