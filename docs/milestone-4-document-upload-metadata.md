# Milestone 4: Upload And Manage Document Metadata

Trang thai: Completed

Ngay cap nhat: 2026-06-18

## Muc tieu

- Cho phep employee/admin upload file vao backend.
- Luu file vat ly vao local storage.
- Luu metadata document vao SQL Server.
- Cho phep list/get document theo quyen doc.
- Ho tro access level co ban: admin, employee, guest.
- Ho tro soft delete va restore document.
- Chua parse file, chua OCR, chua chunking, chua embedding, chua RAG.

## Ket qua da hoan thanh

- Upload document bang multipart/form-data.
- Swagger hien input chon file dung qua DTO `DocumentUploadRequest`.
- Validate file null, file rong, size, extension.
- File khong luu bang original filename ma luu bang `documentId + extension`.
- Metadata luu trong bang `Documents`.
- Admin upload duoc document access level: `admin`, `employee`, `guest`.
- Employee upload duoc document access level: `employee`, `guest`.
- Guest khong upload duoc.
- Anonymous khong upload, khong list/get document.
- Guest list/get chi thay document `guest`.
- Employee list/get thay document `employee` va `guest`.
- Admin list/get thay tat ca document chua bi delete.
- Delete la soft delete: set `Status = deleted`, `DeletedAt`, `DeletedByUserId`.
- Restore dua document ve `uploaded`, clear `DeletedAt`, `DeletedByUserId`.
- Delete thanh cong tra HTTP `204 No Content`.
- Guest `/api/documents` da pass test sau khi them migration `AccessLevel`.
- Admin upload/search/delete/restore da pass test.

## Kien thuc da hoc

- `IFormFile` dung de nhan file upload trong ASP.NET Core.
- `multipart/form-data` la content type dung cho upload file.
- `[FromForm]` giup model binding lay file va field tu form-data.
- `[Consumes("multipart/form-data")]` giup Swagger hieu endpoint upload file.
- Entity `Document` la metadata trong SQL, khong phai noi dung file.
- Local storage luu file vat ly tren disk.
- SQL Server luu duong dan, owner, status, access level, size, extension.
- `AccessLevel` khac `UploadedByUserId`:
  - `UploadedByUserId` cho biet ai upload.
  - `AccessLevel` cho biet ai duoc doc.
- Soft delete giup an document khoi UI/API nhung van co the restore.
- HTTP `204` la thanh cong nhung khong co response body.
- Swagger `Server response` la ket qua that, `Example Value` chi la mau schema.

## Module/file chinh

- `backend-dotnet/Modules/Documents/Document.cs`
- `backend-dotnet/Modules/Documents/DocumentStatus.cs`
- `backend-dotnet/Modules/Documents/DocumentAccessLevel.cs`
- `backend-dotnet/Modules/Documents/DocumentService.cs`
- `backend-dotnet/Modules/Documents/DocumentsController.cs`
- `backend-dotnet/Contracts/Documents/DocumentUploadRequest.cs`
- `backend-dotnet/Contracts/Documents/DocumentUploadResponse.cs`
- `backend-dotnet/Contracts/Documents/DocumentResponse.cs`
- `backend-dotnet/Contracts/Documents/DocumentListItemResponse.cs`
- `backend-dotnet/Infrastructure/Storage/LocalFileStorageOptions.cs`
- `backend-dotnet/Infrastructure/Storage/LocalFileStorageService.cs`
- `backend-dotnet/Infrastructure/Retention/DocumentRetentionOptions.cs`
- `backend-dotnet/Infrastructure/Retention/DeletedDocumentPurgeJob.cs`
- `backend-dotnet/Infrastructure/Persistence/AppDbContext.cs`
- `backend-dotnet/Program.cs`
- `backend-dotnet/appsettings.Development.json`

## Database

Bang `Documents`:

- `Id`
- `OriginalFileName`
- `StoredFileName`
- `StoragePath`
- `ContentType`
- `Extension`
- `SizeBytes`
- `Status`
- `AccessLevel`
- `UploadedByUserId`
- `ErrorMessage`
- `CreatedAt`
- `UpdatedAt`
- `DeletedAt`
- `DeletedByUserId`

Migration lien quan:

- `AddDocuments`: tao bang Documents ban dau.
- `AddDocumentSoftDeleteFields`: them/cap nhat field soft delete.
- `AddDocumentAccessLevel`: them cot `AccessLevel`.

Loi da gap:

- `Invalid column name 'AccessLevel'`
- Nguyen nhan: code da dung property `AccessLevel` nhung database chua duoc migration/update.
- Cach sua: tao migration moi va chay `dotnet ef database update`.

## API endpoints

- `POST /api/documents/upload`
  - Upload file.
  - Body: `multipart/form-data`.
  - Field: `file`, `accessLevel`.
  - Thanh cong: `200 OK` + `DocumentUploadResponse`.

- `GET /api/documents`
  - List document theo quyen user hien tai.
  - Thanh cong: `200 OK` + list `DocumentListItemResponse`.
  - Neu khong co document nao hop quyen: tra `[]`.

- `GET /api/documents/{documentId}`
  - Lay chi tiet metadata document.
  - Neu document khong ton tai, da delete, hoac user khong duoc xem: `404`.

- `DELETE /api/documents/{documentId}`
  - Soft delete document.
  - Thanh cong: `204 No Content`.
  - Khong tra response body.

- `POST /api/documents/{documentId}/restore`
  - Restore document da bi soft delete.
  - Thanh cong: `200 OK` + `DocumentResponse`.

## Quyen truy cap

Role doc document:

- `admin`: doc moi document chua bi delete.
- `employee`: doc document `employee` va `guest`, bao gom document do employee khac upload.
- `guest`: doc document `guest`.
- `anonymous`: khong doc.

Role upload document:

- `admin`: upload `admin`, `employee`, `guest`.
- `employee`: upload `employee`, `guest`.
- `guest`: khong upload.
- `anonymous`: khong upload.

Role delete/restore document:

- `admin`: delete/restore moi document.
- `employee`: delete/restore document trong pham vi access level employee tro xuong (`employee`, `guest`).
- `guest`: khong delete/restore.
- `anonymous`: khong delete/restore.

Ly do khong dung `UploadedByUserId` lam quyen doc:

- Admin co the upload document cho toan cong ty xem.
- Employee co the upload document public cho guest xem.
- Neu chi dua vao owner thi document admin upload se chi admin thay, sai yeu cau nghiep vu.
- Vi vay can tach owner va access level.

Ghi chu cap nhat 2026-06-25:

- Read/list/search/ingest/chunk dua tren `AccessLevel`.
- `UploadedByUserId` khong dung de chan employee khac doc/ingest/chunk file employee-level.
- `UploadedByUserId` van dung cho audit/debug, khong phai dieu kien quyen chinh cho delete/restore theo rule moi.

Ghi chu cap nhat 2026-07-03:

- Delete/restore/purge nen di theo access scope de nhat quan voi cach doc/ingest/chunk/index:
  - admin thao tac tat ca.
  - employee thao tac file `employee` va `guest`.
  - guest/anonymous khong thao tac.
- Sau nay nen co Recycle Bin / Deleted Documents page rieng de restore hoac xoa vinh vien.
- Xoa vinh vien khac soft delete: soft delete an document khoi he thong, xoa vinh vien la xoa file vat ly va can audit nghiem ngat hon.

## Flow upload

```text
Swagger/Frontend
 -> POST /api/documents/upload
 -> DocumentsController.Upload
 -> DocumentService.UploadAsync
 -> lay current principal tu HttpContext
 -> check authenticated
 -> chi cho employee/admin upload
 -> validate accessLevel theo role
 -> validate file null/empty/size/extension
 -> tao documentId
 -> LocalFileStorageService.SaveAsync
 -> tao Document metadata
 -> _db.Documents.Add
 -> SaveChangesAsync
 -> AuditLogService.LogAsync document_upload
 -> return DocumentUploadResponse
```

## Flow list

```text
Swagger/Frontend
 -> GET /api/documents
 -> DocumentsController.List
 -> DocumentService.ListAsync
 -> check authenticated
 -> tao query Documents where Status != deleted
 -> filter theo role/accessLevel
 -> order CreatedAt desc
 -> map sang DocumentListItemResponse
 -> return list
```

## Flow get by id

```text
Swagger/Frontend
 -> GET /api/documents/{documentId}
 -> DocumentsController.GetById
 -> DocumentService.GetByIdAsync
 -> check authenticated
 -> query document theo Id va Status != deleted
 -> filter theo role/accessLevel
 -> neu khong thay: 404
 -> map sang DocumentResponse
```

## Flow soft delete

```text
Swagger/Frontend
 -> DELETE /api/documents/{documentId}
 -> DocumentsController.Delete
 -> DocumentService.DeleteAsync
 -> check authenticated
 -> chi admin/employee duoc delete
 -> admin: query document bat ky chua deleted
 -> employee: query document chua deleted va AccessLevel in (employee, guest)
 -> neu khong thay: 404
 -> set Status = deleted
 -> set DeletedAt = now
 -> set DeletedByUserId = current user id
 -> set UpdatedAt = now
 -> SaveChangesAsync
 -> AuditLogService.LogAsync document_delete
 -> return 204 No Content
```

## Flow restore

```text
Swagger/Frontend
 -> POST /api/documents/{documentId}/restore
 -> DocumentsController.Restore
 -> DocumentService.RestoreAsync
 -> check authenticated
 -> chi admin/employee duoc restore
 -> admin: query document Status = deleted
 -> employee: query document Status = deleted va AccessLevel in (employee, guest)
 -> neu khong thay: 404
 -> set Status = uploaded
 -> DeletedAt = null
 -> DeletedByUserId = null
 -> UpdatedAt = now
 -> SaveChangesAsync
 -> AuditLogService.LogAsync document_restore
 -> return DocumentResponse
```

## Local storage

- `LocalFileStorageService` phu trach luu file vat ly.
- File duoc luu bang ten an toan, khong dung original filename.
- Vi du stored filename: `{documentId}.pdf`.
- Original filename chi dung de hien thi.
- Storage path hien tai la local folder trong backend, cau hinh qua `LocalFileStorageOptions`.
- Sau nay co the doi sang network drive, S3, Azure Blob, MinIO bang cach thay storage service/config, khong nen de controller tu quan ly path.

## Validation

Can validate:

- File khong null.
- File length > 0.
- File khong vuot max size.
- Extension hop le.
- Access level hop le.
- Role co quyen upload access level do.
- User da authenticated.

Extension cho phep giai doan nay:

- `.pdf`
- `.docx`
- `.xlsx`
- `.csv`
- `.txt`

## Audit log

Da log cac action:

- `document_upload`
- `document_delete`
- `document_restore`

Metadata an toan nen log:

- original file name
- extension
- size bytes
- content type
- access level
- status/previous status
- deleted/restored timestamp

Khong nen log:

- cookie
- token
- password
- API key
- noi dung file
- full path neu khong can

## Bao mat

- Khong tin vao original filename.
- Khong cho path traversal.
- Khong expose local full path ra response.
- Khong cho anonymous document API.
- Guest chi doc document `guest`.
- Employee khong upload access level `admin`.
- Employee delete/restore duoc document `employee`/`guest`, khong duoc thao tac admin-level document.
- Document deleted bi an khoi list/get/search.
- Khong hard delete file trong HTTP request.

## Test cases da pass

- Anonymous list document bi chan.
- Guest list document thanh cong, chi thay document `guest`.
- Guest upload bi chan.
- Admin upload document thanh cong.
- Admin upload `admin`, `employee`, `guest`.
- Employee upload `employee`, `guest`.
- Employee upload `admin` bi chan.
- Admin list/search thay document hop le.
- Get by id theo dung access level.
- Delete document tra `204 No Content`.
- Document deleted bien mat khoi list.
- Restore document thanh cong.
- Sau restore document hien lai trong list.
- File khong hop le bi reject.
- Migration `AccessLevel` da sua loi SQL invalid column.

## Quyet dinh kien truc

- Chon soft delete truoc, purge sau.
- Delete API khong xoa file vat ly ngay.
- Restore status ve `uploaded` trong version hien tai.
- Chua luu `PreviousStatus`; neu can restore ve status cu thi them sau.
- Hard purge dua sang Milestone 15/16.
- Permission nang cao theo department/user cu the dua sang Milestone 14.
- Recycle Bin / Deleted Documents page dua sang milestone nang cap UI/Admin sau.
- RAG/ingestion se bo qua document `deleted`.

## Chua lam trong Milestone 4

- Parse PDF/DOCX/CSV/TXT.
- Extract text.
- OCR.
- Chunking.
- Embedding.
- Qdrant indexing.
- Download file endpoint.
- Preview file endpoint.
- Pagination/filter nang cao.
- Department permission.
- Virus scanning.
- Background purge job that su chay dinh ky.
- Audit log doc document.

## Anh huong toi milestone sau

- Milestone 5: Python FastAPI se nhan `documentId` va `StoragePath` de extract text.
- Milestone 6: chunk phai co `documentId` de truy ve file goc.
- Milestone 7: Qdrant payload can co `documentId`, `accessLevel`, `status`.
- Milestone 8: RAG search phai filter theo access level va bo qua deleted.
- Milestone 14: se mo rong permission tu access level sang department/user-specific.
- Milestone 15: background job se xu ly processing/indexing/purge.
- Milestone 16: them rate limit, audit security, quota, virus scan/co che file safety.

## Viec can nho khi quay lai

- Neu them property moi vao `Document`, phai tao migration va `database update`.
- Neu Swagger upload khong hien nut chon file, kiem tra:
  - endpoint co `[Consumes("multipart/form-data")]`
  - parameter dung `[FromForm] DocumentUploadRequest`
  - DTO co property `IFormFile File`
- Neu list tra `[]`, doc `Server response`, khong doc `Example Value`.
- Neu delete tra `204`, do la thanh cong.
- Neu SQL bao invalid column, gan nhu chac la thieu migration/update database.
