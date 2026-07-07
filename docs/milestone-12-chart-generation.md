# Milestone 12: Chart Generation

Trang thai: Completed

Ngay cap nhat: 2026-07-02

## Muc tieu

- Tao chart PNG tu ket qua phan tich dataset CSV/XLSX.
- Giu kien truc de sau nay de nang cap sang multi-file, table rendering, report export.
- Khong dung RAG/Qdrant de ve chart.
- Khong dung OpenAI de tinh so lieu.

## Quyet dinh kien truc

Chot dung Cach A:

```text
Dataset Analysis truoc
 -> tao data JSON chuan
 -> Chart Renderer chi nhan data JSON va ve chart
```

Ly do:

- Tach ro tinh toan va hien thi.
- Sau nay multi-file analysis chi can nang cap analysis layer.
- Chart/table/report co the dung lai cung mot analysis result.
- Giam trung logic doc file/tinh toan trong chart service.
- De test va debug hon.

Khong chon Cach B:

```text
Chart service tu doc file va tu analyze
```

Vi cach B de nhanh luc dau, nhung ve sau chart service se om qua nhieu trach nhiem:

- doc file,
- tinh toan,
- merge data,
- ve chart,
- luu chart.

## Scope Milestone 12

Lam trong milestone nay:

- Render chart tu mot `DatasetAnalysisResponse`.
- Ho tro chart type ban dau:
  - `bar`
  - `line`
  - `pie`
- Luu chart PNG vao thu muc generated chart.
- C# endpoint tien dung:

```text
POST /api/documents/{documentId}/dataset/chart
```

Ben trong endpoint nay van theo Cach A:

```text
C# ChartService
 -> goi DatasetAnalysisService.AnalyzeAsync
 -> lay result data
 -> goi PythonChartClient.RenderAsync
 -> return chart response
```

Chua lam trong milestone nay:

- multi-file chart.
- natural language planner.
- auto-find documents.
- interactive chart.
- dashboard BI.
- export PDF/PowerPoint.
- cleanup chart theo chat session/chart ownership.

## Kien thuc can hoc

- Matplotlib.
- Bar chart, line chart, pie chart.
- Luu PNG bang `savefig`.
- JSON data shape cho chart.
- Static/generated file path.
- C# HttpClient goi Python.
- Cach tach analysis result va visualization.

## Python module

```text
ai-service-python/
  app/charts/
    __init__.py
    chart_models.py
    chart_service.py
  app/api/charts.py
```

### `chart_models.py`

Contract Python:

```text
ChartRenderRequest
ChartRenderResponse
```

Request nhan:

- `chartType`
- `title`
- `data`
- `xField`
- `yField`

Response tra:

- `success`
- `chartType`
- `chartPath`
- `data`
- `warnings`
- `errorMessage`

### `chart_service.py`

Ham chinh:

```text
render_chart(request)
```

Trach nhiem:

- validate chartType.
- validate data khong rong.
- chon x/y field khong phan biet hoa/thuong.
- ve chart bang matplotlib.
- luu PNG.
- tra response.

Luu y:

- File nay khong doc CSV/XLSX.
- File nay khong tinh sum/group/top_n.
- File nay chi render data da co.
- Chart duoc luu vao:

```text
ai-service-python/generated/charts
```

### `app/api/charts.py`

Endpoint Python:

```text
POST /charts/render
```

## C# module

```text
backend-dotnet/
  Contracts/Charts/
    ChartRequest.cs
    ChartResponse.cs

  Contracts/Python/
    PythonChartRenderRequest.cs
    PythonChartRenderResponse.cs

  Infrastructure/Python/
    PythonChartClient.cs

  Modules/Charts/
    ChartService.cs
    ChartsController.cs
```

## Flow C# endpoint

```text
POST /api/documents/{documentId}/dataset/chart
 -> ChartsController nhan request
 -> ChartService.CreateChartAsync
 -> DatasetAnalysisService.AnalyzeAsync
 -> PythonChartClient.RenderAsync
 -> Python /charts/render
 -> luu chart PNG
 -> return ChartResponse
 -> audit dataset_chart_create
```

## Request du kien

```json
{
  "chartType": "bar",
  "operation": "group_by",
  "sheetName": null,
  "valueColumn": "DoanhThu",
  "groupByColumn": "Thang",
  "topN": 12,
  "title": "Doanh thu theo thang",
  "xField": "Thang",
  "yField": "DoanhThu"
}
```

## Response du kien

```json
{
  "documentId": "...",
  "success": true,
  "chartType": "bar",
  "chartPath": "generated/charts/....png",
  "data": [
    { "Thang": 1, "DoanhThu": 1000000 },
    { "Thang": 2, "DoanhThu": 1200000 }
  ],
  "warnings": [],
  "errorMessage": null
}
```

## Test cases

- Tao bar chart tu `group_by`.
- Tao line chart tu `group_by`.
- Tao pie chart tu data hop le.
- `valueColumn` khac hoa/thuong van ve duoc vi analysis da xu ly.
- `xField`/`yField` khac hoa/thuong van resolve duoc trong chart renderer.
- Sai `chartType` tra loi ro.
- Analysis fail thi chart fail, khong tao file rong.
- Operation tra scalar nhu `sum` khong ve chart truc tiep vi chart can data array.
- File PDF/DOCX bi chan o analysis layer.
- Guest tao chart duoc voi file guest-level.
- Guest khong tao chart duoc voi file employee/admin.

## Dau hieu hoan thanh

- Backend co endpoint tao chart.
- Python co endpoint render chart.
- Chart PNG duoc tao va mo duoc.
- Response co `chartPath` va `data`.
- Logic tinh toan van nam o DatasetAnalysisService.
- Chart renderer khong doc file goc.
- Backend build pass.
- Endpoint backend `/api/documents/{documentId}/dataset/chart` tao duoc chart.

## Ket qua da lam

- Python co endpoint:

```text
POST /charts/render
```

- Backend co endpoint:

```text
POST /api/documents/{documentId}/dataset/chart
```

- `ChartService.CreateChartAsync` di theo Cach A:

```text
ChartRequest
 -> DatasetAnalysisService.AnalyzeAsync
 -> PythonChartClient.RenderAsync
 -> ChartResponse
```

- `PythonChartClient.RenderAsync` goi dung endpoint Python `/charts/render`.
- `chart_service.py` render duoc `bar`, `line`, `pie`.
- Chart renderer chi nhan `data`, khong nhan `filePath`.
- `group_by` tu Milestone 11 co the dung lam data dau vao cho chart.
- Chart file dang duoc giu lai tren disk; cleanup theo session de sau.

## Mo rong sau nay

- Multi-file analysis:
  - analysis layer nhan nhieu document/file.
  - chart layer van nhan data JSON nhu cu.
- Table rendering:
  - dung cung data JSON.
  - output HTML/PNG/table response.
- Report generation:
  - gom data + chart + summary.
- Cleanup chart:
  - sau nay co the gan chart voi chat session/user/request id.
  - khi session ket thuc hoac chart het han thi xoa file.
