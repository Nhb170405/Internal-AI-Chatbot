# Milestone 10: Document Metadata Routing

Trang thai: Completed

Ngay cap nhat: 2026-06-27

## Muc tieu

- Them kha nang tim dung document/file bang metadata trong SQL truoc khi search Qdrant.
- Giam viec search toan bo chunk khi user da hoi ro thang/nam/loai bao cao/phong ban/keyword.
- Chuan bi nen tang cho dataset profiling, pandas analysis va chart generation o Milestone 11-12.

## Ly do lam milestone nay

Truoc milestone nay, document search/RAG flow chinh la:

```text
query user
 -> embed query
 -> search Qdrant theo accessLevel
 -> lay top-k chunks
```

Flow do dung cho semantic search, nhung chua tot khi user hoi co metadata ro:

```text
"Bao cao doanh thu thang 4/2026"
"Tai lieu HR cua phong nhan su"
"File excel sales nam 2026"
```

Milestone 10 them lop metadata routing:

```text
query/filter user
 -> SQL search DocumentMetadata theo permission
 -> lay candidate documentIds
 -> sau nay Qdrant/RAG chi search trong cac documentIds nay
```

## Nguyen tac thiet ke

Tach metadata thanh 2 nhom:

```text
Documents
 -> metadata ky thuat chac chan co khi upload:
    OriginalFileName
    StoredFileName
    StoragePath
    Extension
    ContentType
    SizeBytes
    AccessLevel
    UploadedByUserId
    Status

DocumentMetadata
 -> metadata nghiep vu/tim kiem:
    Title
    Description
    ReportType
    ReportDate
    ReportMonth
    ReportYear
    Department
    SourceSystem
    Language
    KeywordsJson
    TagsJson
    DetectedColumnsJson
    SheetNamesJson
```

Khong lap lai `OriginalFileName`, `Extension`, `ContentType`, `SizeBytes` trong `DocumentMetadata`.
Khi API tra response, backend join `Documents` + `DocumentMetadata` de frontend nhin duoc ca 2 nhom thong tin.

## Database

Da them entity/table:

```text
DocumentMetadata
```

Schema chinh:

```text
Id
DocumentId
Title
Description
ReportType
ReportDate
ReportMonth
ReportYear
Department
SourceSystem
Language
KeywordsJson
TagsJson
DetectedColumnsJson
SheetNamesJson
CreatedAt
UpdatedAt
```

Quan he:

```text
Document 1 - 0..1 DocumentMetadata
```

Trong `AppDbContext`:

- `DbSet<DocumentMetadata> DocumentMetadatas`
- unique index tren `DocumentId`
- index tren `ReportYear, ReportMonth`
- index tren `ReportType`
- index tren `Department`
- cascade delete theo `Document`

## File/module da tao

```text
backend-dotnet/Modules/Documents/DocumentMetadata.cs
backend-dotnet/Modules/Documents/DocumentMetadataService.cs
backend-dotnet/Modules/Documents/DocumentMetadataRoutingService.cs
backend-dotnet/Modules/Documents/DocumentMetadataController.cs

backend-dotnet/Contracts/Documents/DocumentMetadataResponse.cs
backend-dotnet/Contracts/Documents/UpdateDocumentMetadataRequest.cs
backend-dotnet/Contracts/Documents/MetadataSearchRequest.cs
backend-dotnet/Contracts/Documents/MetadataSearchResponse.cs
```

Da cap nhat:

```text
backend-dotnet/Infrastructure/Persistence/AppDbContext.cs
backend-dotnet/Program.cs
backend-dotnet/Modules/Documents/DocumentService.cs
```

## API da co

```http
GET /api/documents/{documentId}/metadata
PUT /api/documents/{documentId}/metadata
GET /api/documents/metadata/search
```

## DTO

### UpdateDocumentMetadataRequest

DTO request khi admin/employee sua metadata.

Dung de nhan input tu client, khong dung truc tiep entity database de tranh client set cac field nguy hiem nhu:

```text
Id
DocumentId
CreatedAt
UpdatedAt
DetectedColumnsJson
SheetNamesJson
```

Client gui list binh thuong:

```json
"keywords": ["doanh thu", "sales"]
```

Service se serialize thanh `KeywordsJson`.

### DocumentMetadataResponse

Response gom ca:

- metadata ky thuat tu `Documents`
- metadata nghiep vu tu `DocumentMetadata`

Vi du:

```json
{
  "documentId": "...",
  "originalFileName": "Toan_lop_6_6_7_2026-.pdf",
  "extension": ".pdf",
  "contentType": "application/pdf",
  "sizeBytes": 860777,
  "accessLevel": "employee",
  "title": "Toan_lop_6_6_7_2026-",
  "description": null,
  "reportType": null,
  "reportMonth": 7,
  "reportYear": 2026,
  "keywords": [],
  "tags": []
}
```

## DocumentMetadataService

### CreateDefaultMetadataForUploadAsync

Duoc goi tu `DocumentService.UploadAsync` sau khi `Document` duoc luu vao SQL.

Flow:

```text
Input: Document document
 -> validate document.Id
 -> check DocumentMetadata da ton tai chua
 -> tao Title tu OriginalFileName bo extension
 -> parse ReportMonth/ReportYear tu filename neu co pattern don gian
 -> tao KeywordsJson = []
 -> tao TagsJson = []
 -> Language = unknown
 -> save DocumentMetadata
 -> audit document_metadata_create_default
```

Pattern parse thang/nam dang ho tro baseline:

```text
2026-04
04-2026
thang-4-2026
```

Luu y: baseline parser co the doan sai voi ten file co nhieu so, vi du `Toan_lop_6_6_7_2026`.
Do do metadata sau upload chi la goi y mac dinh, admin/employee co the sua bang API `PUT`.

### GetAsync

Dung cho:

```http
GET /api/documents/{documentId}/metadata
```

Flow:

```text
 -> lay current principal
 -> check authenticated
 -> lay role
 -> query Document theo documentId va Status != Deleted
 -> check quyen doc:
      admin: doc tat ca
      employee: doc employee/guest
      guest: doc guest
 -> query DocumentMetadata theo DocumentId
 -> map Document + DocumentMetadata sang response
```

Neu document khong ton tai hoac user khong co quyen doc:

```text
throw KeyNotFoundException
```

Controller map thanh `404 document_not_found` de khong lo document co that hay khong.

### UpdateAsync

Dung cho:

```http
PUT /api/documents/{documentId}/metadata
```

Flow:

```text
 -> validate request
 -> lay current principal
 -> check authenticated
 -> lay role
 -> query Document
 -> check quyen sua:
      admin: sua moi document
      employee: sua employee/guest
      guest: khong duoc sua
 -> validate month/year/text length/list
 -> query DocumentMetadata
 -> neu metadata chua co thi tao moi
 -> update Title/Description/ReportType/ReportDate/ReportMonth/ReportYear/Department/SourceSystem/Language
 -> serialize Keywords va Tags thanh JSON
 -> save
 -> audit document_metadata_update
 -> return response
```

Gioi han validate:

```text
ReportMonth: 1..12
ReportYear: 2000..2100
Title: max 500 chars
Description: max 2000 chars
ReportType/Department/SourceSystem: max 100 chars
Language: max 20 chars
Keywords/Tags: max 30 items, moi item max 100 chars
```

## DocumentMetadataRoutingService

### SearchAsync

Dung cho:

```http
GET /api/documents/metadata/search
```

Flow:

```text
 -> request null thi tao request rong
 -> lay current principal
 -> lay role
 -> normalize limit, toi da 50
 -> left join Documents voi DocumentMetadatas
 -> filter Document.Status != Deleted
 -> filter access level theo role
 -> filter reportType neu co
 -> filter reportMonth/reportYear neu co
 -> filter department neu co
 -> filter keyword trong KeywordsJson neu co
 -> filter tag trong TagsJson neu co
 -> filter query text trong OriginalFileName/Title/Description/KeywordsJson/TagsJson
 -> sort CreatedAt desc
 -> take limit
 -> map response
```

Quyen search:

```text
admin: thay admin/employee/guest
employee: thay employee/guest
guest: thay guest
anonymous: 401
```

### FindCandidateDocumentIdsAsync

Ham noi bo cho cac milestone sau.

Muc dich:

```text
MetadataSearchRequest
 -> SearchAsync
 -> lay danh sach DocumentId
```

Sau nay RAG co the dung:

```text
query user
 -> metadata routing tim candidate documentIds
 -> Qdrant search chi trong cac documentIds nay
```

## Controller

`DocumentMetadataController` bat loi:

```text
UnauthorizedAccessException -> 401 unauthorized
KeyNotFoundException -> 404 document_not_found
ArgumentException -> 400 invalid_metadata hoac invalid_metadata_search
```

## Test da pass

### Upload tu tao metadata

- Upload file moi.
- SQL co row moi trong `DocumentMetadatas`.
- `Title` lay tu ten file.
- `KeywordsJson = []`.
- `TagsJson = []`.
- `Language = unknown`.
- Neu filename co pattern thang/nam don gian thi parse duoc `ReportMonth`, `ReportYear`.

### GET metadata

```http
GET /api/documents/{documentId}/metadata
```

Ket qua:

- Tra 200 voi data gom `Documents` + `DocumentMetadata`.
- `keywords/tags/detectedColumns/sheetNames` tra list rong neu null/empty.

### PUT metadata

```http
PUT /api/documents/{documentId}/metadata
```

Da test update:

- title
- description
- reportType
- reportMonth/reportYear
- department
- sourceSystem
- language
- keywords
- tags

Sau update, GET lai thay du lieu moi.

### Search metadata

Da test:

```http
GET /api/documents/metadata/search
GET /api/documents/metadata/search?query=toan
GET /api/documents/metadata/search?reportMonth=7&reportYear=2026
GET /api/documents/metadata/search?reportType=education
GET /api/documents/metadata/search?department=training
GET /api/documents/metadata/search?keyword=toan
GET /api/documents/metadata/search?tag=pdf
GET /api/documents/metadata/search?limit=1
GET /api/documents/metadata/search?reportMonth=13
```

Ky vong:

- Query dung tra result.
- Query sai tra `count = 0`.
- Month sai tra `400 invalid_metadata_search`.
- Anonymous tra `401`.
- Role khong thay document vuot quyen.

## Han che hien tai

- `KeywordsJson.Contains(...)` va `TagsJson.Contains(...)` chi la baseline string search, chua phai JSON query/full-text search chuan.
- Search text dang dung `Contains`, chua co full-text index.
- Chua co AI tu dong dien `Description`, `ReportType`, `Department`.
- Chua co pandas profile nen `DetectedColumnsJson`, `SheetNamesJson` van null.
- Chua tich hop metadata routing vao `RagService`.
- Chua filter Qdrant theo `documentIds`.

## Viec de danh

Milestone 11:

- pandas doc CSV/XLSX.
- tu dien `DetectedColumnsJson`, `SheetNamesJson`.
- tao `DocumentTableProfile`.

Milestone 12:

- tao chart tu dataset/profile/analysis result.

Milestone 14:

- permission nang cao theo department/user-specific.

Milestone 16:

- toi uu security/cost/rate limit.
- co the nang keyword/tag search len full-text/JSON query.

## Dau hieu hoan thanh

- Co table `DocumentMetadatas`.
- Upload tao metadata mac dinh.
- Co the doc metadata qua API.
- Co the sua metadata qua API.
- Co the search metadata qua API.
- Search co phan quyen theo role/accessLevel.
- Build pass.
- Test Swagger pass cac testcase chinh.
