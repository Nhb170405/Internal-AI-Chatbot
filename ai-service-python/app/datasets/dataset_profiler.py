from app.datasets.dataset_models import DatasetColumnProfile, DatasetProfileRequest, DatasetProfileResponse
import pandas as pd

from datetime import date, datetime
import math
import numpy as np

from pathlib import Path

from app.ingestion.file_reference_resolver import (
    cleanup_resolved_file,
    resolve_file_reference,
)

def profile_dataset(request: DatasetProfileRequest) -> DatasetProfileResponse:
    # Bai tap Milestone 11:
    # 1. Validate filePath ton tai.
    # 2. Validate extension chi chap nhan .csv, .xlsx, .xls.
    # 3. Neu .csv:
    #    - dung pandas.read_csv.
    #    - tao mot DatasetTableProfile voi sheetName = "default".
    # 4. Neu .xlsx/.xls:
    #    - dung pandas.ExcelFile de lay danh sach sheet.
    #    - doc tung sheet bang pandas.read_excel.
    #    - tao mot DatasetTableProfile cho moi sheet.
    # 5. Voi moi DataFrame:
    #    - rowCount = so dong.
    #    - columnCount = so cot.
    #    - columns = danh sach DatasetColumnProfile.
    #    - sampleRows = df.head(5) sau khi convert ve JSON-safe dict.
    #    - warnings neu cot rong/duplicate/mixed type.
    # 6. Khong luu file, khong ghi database trong Python.
    # 7. Return DatasetProfileResponse.
    #
    # Goi y implement:
    # import os
    # import pandas as pd
    # if not os.path.exists(request.filePath): return success=false
    # df = pd.read_csv(request.filePath)
    # df.shape -> (row_count, column_count)
    # df.dtypes -> kieu du lieu pandas
    extension = request.extension.lower().strip()

    try:
        resolved = resolve_file_reference(
            file_reference_type=request.fileReferenceType,
            file_reference_value=request.fileReferenceValue,
            legacy_file_path=request.filePath,
            extension=request.extension,
        )
    except ValueError as error:
        return DatasetProfileResponse(
            documentId=request.documentId,
            success=False,
            profiles=[],
            warnings=[],
            errorMessage=str(error),
        )
    except Exception as error:
        return DatasetProfileResponse(
            documentId=request.documentId,
            success=False,
            profiles=[],
            warnings=[],
            errorMessage=f"File reference could not be resolved: {type(error).__name__}.",
        )

    file_path = Path(resolved.file_path)
        
    try:
        if not file_path.exists():
            return DatasetProfileResponse(
                documentId=request.documentId,
                success=False,
                profiles=[],
                warnings=[],
                errorMessage="File path does not exist.",
            )

        if not file_path.is_file():
            return DatasetProfileResponse(
                documentId=request.documentId,
                success=False,
                profiles=[],
                warnings=[],
                errorMessage="File path is not a file.",
            )

        if extension not in [".csv", ".xlsx", ".xls"]:
            return DatasetProfileResponse(
                documentId=request.documentId,
                success=False,
                profiles=[],
                warnings=[],
                errorMessage="Invalid file extension. Only .csv, .xlsx, .xls are allowed.",
            )
            
        if extension == ".csv":
            df = pd.read_csv(str(file_path))
            dataset_profile_tables = [build_table_profile(df, "default", 0)]
        else:
            dataset_profile_tables = []

            with pd.ExcelFile(str(file_path)) as excel_file:
                for index, sheet_name in enumerate(excel_file.sheet_names):
                    df = pd.read_excel(excel_file, sheet_name=sheet_name)
                    table_profile = build_table_profile(df, sheet_name, index)
                    dataset_profile_tables.append(table_profile)

        return DatasetProfileResponse(
            documentId=request.documentId,
            success=True,
            profiles=dataset_profile_tables,
            warnings=[],
            errorMessage=None,
        )
        
    except Exception as error:
        return DatasetProfileResponse(
            documentId=request.documentId,
            success=False,
            profiles=[],
            warnings=[],
            errorMessage=f"Dataset profiling failed: {str(error)}",
        )
        
    finally:
        cleanup_resolved_file(resolved)
        


# Helper function

def build_table_profile(df, sheet_name: str, table_index: int):
    columns = []
    for col in df.columns:
        columns.append(DatasetColumnProfile(
            name=str(col).strip(),
            normalizedName=str(col).strip().lower(),
            dataType=str(df[col].dtype),
            nonNullCount=int(df[col].count()),
            nullCount=int(df[col].isnull().sum())
        ))

    sample_df = df.head(5)
    table_profile = {
        "sheetName": sheet_name,
        "tableIndex": table_index,
        "rowCount": int(df.shape[0]),
        "columnCount": int(df.shape[1]),
        "columns": columns,
        "sampleRows": dataframe_sample_rows(sample_df),
        "warnings": []
    }
    return table_profile

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
