# ADR-002: Dataset Metadata, Profiling, And Analysis Strategy

Trang thai: Accepted

Ngay cap nhat: 2026-06-27

## Boi canh

He thong hien tai xu ly file CSV/XLSX theo huong:

```text
CSV/XLSX
 -> parse thanh text row/cell
 -> chunk
 -> embedding
 -> RAG
```

Cach nay phu hop cho:

- tom tat file.
- tim noi dung co trong file.
- hoi file co nhung thong tin gi.
- tra loi dua tren noi dung text.

Nhung cach nay khong phu hop cho cau hoi can tinh toan chinh xac:

- tong doanh thu thang 4 la bao nhieu.
- top 5 san pham loi nhieu nhat.
- trung binh san luong theo ca.
- group by phong ban/thang.
- filter rows theo dieu kien.

Neu de LLM/RAG tu doc text bang va tu tinh toan, rui ro:

- cong sai.
- doc nham cot.
- bo sot rows.
- bi nhieu context.
- khong giai thich duoc ket qua mot cach kiem chung.

## Quyet dinh

Khong chon rieng metadata-first retrieval hoac pandas analysis.

Chon huong hybrid:

```text
Metadata-first retrieval
+ Dataset profiling
+ Pandas/SQL analysis tool khi can tinh toan
```

## 1. Metadata-first retrieval

SQL se luu metadata de tim dung file/dataset nhanh:

- reportDate.
- reportMonth.
- reportYear.
- reportType.
- department.
- title.
- keywords.
- sheet names.
- detected columns.
- accessLevel.

Muc tieu:

```text
User hoi ve ngay/thang/loai bao cao
 -> SQL metadata tim dung file truoc
 -> chi search/RAG/analyze trong pham vi file do
```

Vi du:

```text
User: Doanh thu thang 4/2026 la bao nhieu?
 -> tim documents co reportMonth=4, reportYear=2026, keywords contains doanh thu/revenue
```

## 2. Dataset profiling

Khi ingest CSV/XLSX, Python pandas se doc file va tao profile:

- sheetName.
- rowCount.
- columnCount.
- detectedColumns.
- inferred data types.
- sample rows.
- warnings neu file co nhieu header/table khong ro.

Profile duoc luu vao SQL dang metadata/JSON, khong ep schema chuan ngay.

Ly do:

- Excel/CSV noi bo thuong khong co form chuan.
- Moi file co the co header khac nhau.
- Co file co nhieu dong tieu de.
- Co file co nhieu bang trong mot sheet.
- Tao table dong trong SQL qua som se phuc tap va de loi.

## 3. Pandas/SQL analysis tool

Khi user hoi cau can tinh toan:

```text
Tong doanh thu thang 4 la bao nhieu?
```

Flow du kien:

```text
1. SQL metadata tim file/dataset phu hop.
2. Dataset profile giup biet sheet/cot nao kha nghi.
3. Python pandas doc file goc hoac dataset cached.
4. Pandas tinh sum/avg/count/groupby/filter.
5. Tra JSON ket qua.
6. OpenAI Chat chi dien giai ket qua, khong tu tinh.
```

## Schema du kien sau nay

### DocumentMetadata

```text
Id
DocumentId
ReportType
ReportDate
ReportMonth
ReportYear
Department
Title
KeywordsJson
DetectedColumnsJson
SheetNamesJson
CreatedAt
UpdatedAt
```

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

### DatasetRows optional sau nay

Chi them neu can query nhanh/lap lai nhieu:

```text
Id
DatasetId
RowIndex
DataJson
```

## Khong lam ngay

- Khong tao table dong cho moi CSV/XLSX trong giai doan dau.
- Khong co gang chuan hoa moi file Excel ve mot schema duy nhat.
- Khong de LLM tu tinh toan tren text neu cau hoi can con so chinh xac.
- Khong dua toan bo file Excel vao prompt.

## Milestone anh huong

Da dua vao roadmap 17 milestone sau Milestone 9:

```text
Milestone 10: Document Metadata Routing
Milestone 11: Dataset Profiling And Analysis
Milestone 12: Chart Generation
```

Neu khong tach milestone, noi dung nay co the nam trong:

- Milestone 13 Admin Dashboard: admin xem metadata/profile.
- Milestone 14 Permission: filter dataset theo access/department.
- Milestone 16 Security/Cost: rate limit tool analysis, audit query.

## Ket luan

Huong dung cho du lieu bang cua doanh nghiep:

```text
SQL metadata de tim dung file
+ pandas profiling de hieu file
+ pandas/SQL tool de tinh toan
+ RAG de giai thich/tom tat/noi dung text
```

RAG khong thay the pandas/SQL cho bai toan tinh toan chinh xac.
