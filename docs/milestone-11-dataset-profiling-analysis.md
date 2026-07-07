# Milestone 11: Dataset Profiling And Analysis

Trang thai: Completed

Ngay cap nhat: 2026-07-01

## Muc tieu

- Xu ly CSV/XLSX nhu structured datasets, khong chi extract thanh text de RAG.
- Dung pandas de doc sheets, cot, sample rows, data types.
- Cho phep tinh toan co ban: preview, list columns, count, sum, average, group by, top N.
- Giu API analysis o dang ro rang: caller truyen operation/cot/sheet cu the.

## Ly do lam milestone nay

RAG text khong phu hop cho tinh toan chinh xac tren bang.

Vi du khong nen de LLM tu tinh:

- Tong doanh thu thang 4.
- Trung binh san luong theo ca.
- Top 5 san pham loi nhieu nhat.

Can pandas/SQL tool tinh, OpenAI chi dien giai ket qua neu can.

## Kien thuc can hoc

- pandas DataFrame.
- `read_csv`, `read_excel`.
- Sheet handling.
- Column normalization.
- Type inference.
- Data profiling.
- Aggregate/filter/groupby.
- JSON result contract.

## Database schema du kien

### DocumentTableProfile

```text
Id
DocumentId
SheetName
TableIndex
RowCount
ColumnCount
ColumnsJson
SampleRowsJson
WarningsJson
CreatedAt
UpdatedAt
```

Optional sau nay:

```text
DatasetRows
- Id
- DatasetId
- RowIndex
- DataJson
```

Giai doan dau chua luu toan bo rows vao SQL. Khi can phan tich, Python doc lai file goc bang pandas.

## Python module

```text
ai-service-python/
  app/datasets/
    dataset_profiler.py
    dataset_analysis_service.py
    dataset_models.py
  app/api/datasets.py
```

## Python implementation da hoan thanh

### `dataset_profiler.py`

Ham chinh:

```text
profile_dataset(request)
```

Trach nhiem:

- validate `filePath`, extension.
- doc CSV bang `pandas.read_csv`.
- doc XLSX/XLS bang `pandas.ExcelFile` va `read_excel` tung sheet.
- tao profile tung sheet/table:
  - `sheetName`
  - `tableIndex`
  - `rowCount`
  - `columnCount`
  - `columns`
  - `sampleRows`
  - `warnings`

### `dataset_analysis_service.py`

Ham chinh:

```text
analyze_dataset(request)
```

Trach nhiem:

- validate request.
- doc file goc bang pandas.
- tim cot khong phan biet hoa/thuong:
  - `Price`, `price`, ` PRICE ` deu match cung mot cot.
- chay cac operation:
  - `preview`
  - `list_columns`
  - `count`
  - `sum`
  - `average`
  - `group_by`
  - `top_n`
- convert ket qua ve JSON-safe.

Quyet dinh nho:

- Khong doi ten cot goc cua dataframe thanh lowercase.
- Chi dung helper tim cot case-insensitive, sau do van tinh tren ten cot goc.
- `group_by` mac dinh sort theo group key tang dan/de doc hon.
- `top_n` van sort theo value giam dan.

### `app/api/datasets.py`

Endpoint Python:

```text
POST /datasets/profile
POST /datasets/analyze
```

## C# module

```text
backend-dotnet/
  Modules/Datasets/
    DocumentTableProfile.cs
    DatasetProfileService.cs
    DatasetAnalysisService.cs
  Infrastructure/Python/
    PythonDatasetClient.cs
  Contracts/Datasets/*
```

## C# implementation da hoan thanh

### `PythonDatasetClient`

Ham:

```text
ProfileAsync
AnalyzeAsync
```

Trach nhiem:

- doc `PythonService:BaseUrl`.
- set `HttpClient.BaseAddress`.
- set timeout.
- POST JSON sang FastAPI:
  - `/datasets/profile`
  - `/datasets/analyze`
- parse response thanh C# contract.
- khong log request vi co `filePath` noi bo.

### `DatasetProfileService`

Ham:

```text
ProfileAsync(documentId)
ListProfilesAsync(documentId)
```

`ProfileAsync` flow:

```text
Lay current user
 -> check authenticated
 -> loc document theo role/accessLevel
 -> check extension .csv/.xlsx/.xls
 -> goi Python profile
 -> neu fail: audit + return fail
 -> neu success:
      xoa profile cu
      luu profile moi vao DocumentTableProfiles
      cap nhat DocumentMetadata.DetectedColumnsJson
      cap nhat DocumentMetadata.SheetNamesJson
      audit dataset_profile
 -> return DatasetProfileResponse
```

`ListProfilesAsync` flow:

```text
Lay current user
 -> check authenticated
 -> loc document theo role/accessLevel
 -> doc DocumentTableProfiles tu SQL
 -> order by SheetName, TableIndex
 -> deserialize ColumnsJson/SampleRowsJson/WarningsJson
 -> return List<DatasetTableProfileResponse>
```

### `DatasetAnalysisService`

Ham:

```text
AnalyzeAsync(documentId, request)
```

Flow:

```text
Validate request
 -> lay current user
 -> check authenticated
 -> loc document theo role/accessLevel
 -> check extension .csv/.xlsx/.xls
 -> normalize operation
 -> tao PythonDatasetAnalysisRequest
 -> goi Python analyze
 -> audit dataset_analyze
 -> map Python response sang DatasetAnalysisResponse
```

### `DatasetsController`

Endpoint C#:

```text
POST /api/documents/{documentId}/dataset/profile
GET  /api/documents/{documentId}/dataset/profile
POST /api/documents/{documentId}/dataset/analyze
```

Map loi:

```text
ArgumentException -> 400
UnauthorizedAccessException -> 401
KeyNotFoundException -> 404
InvalidOperationException -> 502
```

## Flow profiling

```text
CSV/XLSX uploaded/ingested
 -> Python pandas read file
 -> detect sheets
 -> detect columns
 -> infer basic data types
 -> save profile to SQL
```

POST profile khac GET profile:

```text
POST profile:
  goi Python, doc lai file, tao/cap nhat profile trong SQL.

GET profile:
  khong goi Python, chi doc profile da luu trong SQL.
```

## Flow analysis hien tai

Milestone 11 chi lam analysis engine co cau truc ro rang.
Caller phai truyen operation/cot/sheet cu the:

```json
{
  "operation": "sum",
  "valueColumn": "Revenue",
  "groupByColumn": "Month"
}
```

Flow:

```text
User/API caller chon document va operation
 -> C# check auth + accessLevel
 -> C# goi Python /datasets/analyze
 -> Python pandas doc file goc
 -> tinh preview/list_columns/count/sum/average/group_by/top_n
 -> tra JSON ket qua
 -> C# tra response va ghi audit neu can
```

Vi du top 10 giao dich gia tri nhat trong file thang 5:

```json
{
  "operation": "top_n",
  "sheetName": null,
  "valueColumn": "GiaTriGiaoDich",
  "groupByColumn": null,
  "topN": 10
}
```

Neu file chi gom giao dich thang 5 thi ket qua dung theo thang 5.
Neu file gom nhieu thang thi Milestone 11 chua co filter, nen `top_n` se chay tren toan file.

Ly do giu milestone nay don gian:

- pandas tinh toan chinh xac hon LLM.
- request co cau truc de validate va debug.
- khong them query planner/form UI/multi-file analysis qua som.
- neu can trai nghiem tu nhien hon, dua vao backlog sau khi analysis engine on dinh.

## Quyen han

Chot policy cho Milestone 11:

```text
admin:
  profile/analyze moi file

employee:
  profile/analyze file accessLevel employee/guest

guest:
  profile/analyze file accessLevel guest

anonymous:
  khong duoc profile/analyze
```

Ly do cho phep guest profile/analyze:

- He thong da co co che `AccessLevel`.
- File `guest` duoc xem la file co the cong khai cho guest.
- Neu guest da duoc doc noi dung file guest-level thi viec xem profile/analysis co ban tren file do la hop ly.
- Guest van khong duoc upload/ingest/chunk/index file.

Ghi chu:

- Rate limit/cost hardening se duoc xu ly o Milestone 16 neu can.
- Milestone 11 chua lam multi-file analysis cho guest.
- Milestone 11 chi analyze document ma user hien tai co quyen doc.

## Operations ban dau

- `preview`: tra ve mot so dong dau.
- `list_columns`: tra ve danh sach cot.
- `count`: dem so dong.
- `sum`: tinh tong cot numeric.
- `average`: tinh trung binh cot numeric.
- `group_by`: group by mot cot va sum cot numeric.
- `top_n`: lay N dong co gia tri numeric cao nhat.

## Test cases

- CSV don gian profile duoc cot/row/sample.
- XLSX nhieu sheet profile duoc tung sheet.
- Preview rows thanh cong.
- List columns thanh cong.
- Count rows thanh cong.
- Tinh sum mot cot numeric.
- Average mot cot numeric.
- Group by month/department.
- Top N theo cot numeric.
- Sai file path tra `success=false`.
- Sai extension tra `success=false`.
- Cot khong ton tai tra `success=false`.
- Cot text ma tinh sum/average tra `success=false`.
- User khong co quyen khong phan tich file admin-level.
- Guest profile/analyze duoc file guest-level.
- Guest khong profile/analyze duoc file employee/admin-level.

## Dau hieu hoan thanh

- Co dataset profile trong SQL.
- Python pandas tinh duoc aggregate co ban.
- API tra JSON result kiem chung duoc.
- LLM khong tu tinh so lieu tu text nua.
- C# backend goi duoc Python profile/analyze.
- Profile cap nhat duoc `DocumentTableProfiles`.
- Profile cap nhat duoc `DocumentMetadata.DetectedColumnsJson` va `SheetNamesJson`.
- Analyze ho tro column matching khong phan biet hoa/thuong.

## Chua lam

- Chart generation.
- Complex multi-table join.
- Auto mapping cot bang AI nang cao.
- Natural language query planner.
- Guided analysis UI.
- Multi-file analysis/chart.
- Luu toan bo dataset rows vao SQL/data warehouse.
