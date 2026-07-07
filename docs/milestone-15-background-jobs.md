# Milestone 15: Background Jobs

Trang thai: Planned

Ngay cap nhat: 2026-07-03

## Muc tieu

- Upload file khong bi cho lau.
- Xu ly ingestion/OCR/chunk/index async.
- Co retry khi loi.
- Co job status/log de admin theo doi.
- Tu dong purge file vat ly cua document da soft delete sau retention period.
- Chuyen pipeline hien tai tu sync sang async:
  - hien tai frontend upload xong goi lan luot ingest -> chunk -> index -> profile.
  - milestone nay se de backend dieu phoi pipeline bang job.
  - frontend chi can upload va theo doi status.

## Ly do lam milestone nay

- Parse PDF/OCR/embedding co the mat lau va gay timeout HTTP.
- Background jobs giup user upload xong nhanh, xu ly sau.
- Retry/log can thiet de van hanh production.
- Pipeline hien tai da dung duoc nhung de bi cham khi file lon:
  - PDF dai.
  - OCR scan.
  - embedding nhieu chunks.
  - profile file Excel/CSV lon.
- Neu tiep tuc de frontend tu bam/goi tung action thi UX se kho hieu:
  - user khong can biet ingest/chunk/index/profile la gi.
  - user chi can thay document dang Processing, Indexed, Failed.

## Kien thuc can hoc

- Hangfire hoac queue.
- Worker service.
- Retry/backoff.
- Job status.
- Idempotency.
- Observability.
- Polling tu frontend de xem job/document status.
- Transaction boundary: cai gi save truoc, cai gi retry duoc.
- Distributed boundary giua ASP.NET Core va Python service.

## Quyet dinh kien truc de xuat

Cap nhat 2026-07-03:

Milestone 15 chot dung **Hangfire + SQL Server storage** thay vi tu viet SQL polling queue.

Ly do:

- Du an chinh la ASP.NET Core, Hangfire la lua chon rat phu hop voi .NET.
- Hangfire production-friendly hon tu viet queue:
  - co enqueue job.
  - co retry.
  - co job state.
  - co dashboard.
  - co recurring/scheduled jobs.
- Van dung SQL Server nen khong phai cai RabbitMQ/Redis ngay.
- De debug hon RabbitMQ voi nguoi moi.
- Sau nay neu he thong lon hon, co the tach worker sang process rieng hoac doi sang RabbitMQ.

Trade-off:

- Them dependency Hangfire.
- Can hoc cach Hangfire serialize job va resolve dependency injection.
- Hangfire dashboard can bao ve bang auth/admin, khong duoc public.
- Neu scale rat lon/microservice phuc tap, RabbitMQ van la lua chon manh hon.

Khong chon RabbitMQ ngay:

- RabbitMQ rat tot cho microservice va production lon.
- Nhung no them mot service ha tang moi, can hoc exchange/queue/routing/ack/dead-letter.
- Voi internal app hien tai, Hangfire + SQL Server du chuan va de van hanh hon.

Khong chon Redis Queue ngay:

- Redis nhanh, hop rate limit/cache/job ngan.
- Nhung voi document pipeline dai, can retry va dashboard ro rang, Hangfire phu hop hon.

Vai tro cua SQL Server trong phuong an moi:

- Hangfire tu tao cac bang rieng de luu job queue/state.
- Ung dung van co the tao bang rieng `DocumentProcessingJobs` de hien thi status than thien cho UI/admin.
- Khong nen de frontend doc truc tiep bang Hangfire; frontend nen doc API cua minh.

## Job types can co

```text
document_process
document_ingest
document_chunk
document_index
document_profile
document_purge
```

Milestone 15 nen bat dau voi `document_process` truoc.

`document_process` la job tong:

```text
document_process
 -> ingest neu document chua extracted
 -> chunk neu document chua chunked
 -> index neu document chua indexed
 -> profile neu extension la csv/xlsx/xls
 -> set status cuoi cung
```

Ly do bat dau bang job tong:

- Frontend don gian.
- User khong can thay tung action ky thuat.
- De hoan thanh milestone nhanh.

Sau nay neu can retry rieng tung buoc, co the tach thanh nhieu job con:

```text
document_ingest -> document_chunk -> document_index -> document_profile
```

## Module du kien

```text
backend-dotnet/Modules/Jobs/IngestionJob.cs
backend-dotnet/Modules/Jobs/JobLog.cs
backend-dotnet/Modules/Jobs/DocumentProcessingWorker.cs
backend-dotnet/Modules/Jobs/JobService.cs
backend-dotnet/Modules/Documents/DeletedDocumentPurgeJob.cs
```

De phu hop code hien tai va Hangfire, nen thiet ke theo module:

```text
backend-dotnet/Modules/BackgroundJobs/
  DocumentProcessingJob.cs
  DocumentProcessingJobStatus.cs
  DocumentProcessingJobType.cs
  DocumentProcessingJobService.cs
  DocumentProcessingJobRunner.cs
  DocumentProcessingJobHandler.cs
  DeletedDocumentPurgeJobHandler.cs
  BackgroundJobsController.cs

backend-dotnet/Contracts/BackgroundJobs/
  DocumentProcessingJobResponse.cs
  EnqueueDocumentProcessingResponse.cs
```

Khong nen dat qua nhieu logic trong Controller.

- Controller: nhan request, tra response.
- DocumentProcessingJobService: tao app-level job record, enqueue Hangfire job, list job, retry job.
- DocumentProcessingJobRunner: method public duoc Hangfire goi.
- DocumentProcessingJobHandler: goi DocumentIngestionService, DocumentChunkingService, DocumentVectorService, DatasetProfileService.
- DeletedDocumentPurgeJobHandler: xu ly purge deleted documents.

## Database du kien

```text
Hangfire tables
- Hangfire tu tao bang rieng trong SQL Server khi cau hinh storage.
- Khong can tu tao bang queue neu dung Hangfire.

DocumentProcessingJobs
- Id
- DocumentId
- HangfireJobId
- JobType
- Status
- AttemptCount
- MaxAttempts
- LastError
- CreatedAt
- StartedAt
- CompletedAt
- UpdatedAt

DocumentProcessingJobLogs
- Id
- DocumentProcessingJobId
- JobType
- DocumentId
- Step
- Status
- Attempt
- ErrorMessage
- CreatedAt
- StartedAt
- CompletedAt
```

Ghi chu:

- Hangfire job arguments chi nen luu thong tin an toan:
  - documentProcessingJobId
  - documentId
- Khong luu file content, API key, cookie, token vao payload/log.
- Khong can tu viet `LockedAt` neu da dung Hangfire, vi Hangfire quan ly worker/job state.
- Bang app-level job dung de UI/admin xem trang thai theo document, khong thay the Hangfire queue.

## Flow async upload

```text
User upload
 -> Document status Uploaded
 -> tao DocumentProcessingJob record
 -> enqueue Hangfire job
 -> return response nhanh
 -> Hangfire worker pick job
 -> set Processing
 -> call Python ingestion
 -> save DocumentExtraction
 -> call Python chunk
 -> save DocumentChunks
 -> call Python vector index
 -> save/update Indexed status
 -> if CSV/XLSX/XLS then run profile
 -> set Indexed/Profiled indicator
 -> write DocumentProcessingJobLogs
 -> if success: Status = completed
 -> if failed retryable: Hangfire retry theo retry policy
 -> if failed final: Status = failed, Document.Status = failed
```

## Flow frontend sau Milestone 15

```text
DocumentsPage
 -> user upload file
 -> POST /api/documents/upload
 -> backend tao Document + enqueue processing job
 -> frontend hien document status Uploaded/Queued
 -> frontend poll GET /api/documents hoac GET /api/background-jobs/document/{documentId}
 -> khi job chay: Processing
 -> khi xong: Indexed/Profiled
 -> khi loi: Failed + error summary
```

Sau milestone nay, frontend khong nen hien cac nut:

- ingest
- chunk
- index
- profile

Frontend chi nen hien:

- upload
- delete
- restore/recycle bin sau nay
- retry processing neu job failed

## Flow purge deleted documents

```text
Scheduler/Worker
 -> Hangfire recurring job chay theo lich
 -> query Documents Status = Deleted va DeletedAt < retention
 -> xoa file vat ly neu con ton tai
 -> xoa/inactive DocumentChunks
 -> xoa/inactive Qdrant vectors
 -> audit document_purge
```

## Flow retry

```text
Job failed
 -> Hangfire ghi failed state
 -> Hangfire retry theo retry policy
 -> DocumentProcessingJob.AttemptCount += 1
 -> LastError = short safe message
 -> neu retry thanh cong:
      Status = completed
 -> neu het retry:
      Status = failed
      Document.Status = failed
      Audit log job_failed
```

Backoff de xuat:

```text
attempt 1: retry sau 30 giay
attempt 2: retry sau 2 phut
attempt 3: retry sau 10 phut
```

Khong nen retry vo han vi co the ton OpenAI token/OCR CPU.

## Idempotency rules

Job handler phai chay lai duoc ma khong tao du lieu trung qua muc.

Document processing:

- Neu da co extraction moi nhat hop le thi co the bo qua ingest hoac replace co kiem soat.
- Neu chunk lai thi xoa/replace chunks cu cua document truoc khi insert chunks moi.
- Neu index lai thi upsert Qdrant theo chunkId de khong tao duplicate vectors.
- Profile file bang nen upsert `DocumentTableProfile`.

Purge:

- Neu file vat ly da khong con, job van co the completed va log warning.
- Neu Qdrant vector da xoa, purge khong duoc fail chi vi vector khong ton tai.

## API endpoints du kien

```http
POST /api/documents/{documentId}/process
GET  /api/background-jobs
GET  /api/background-jobs/{jobId}
GET  /api/background-jobs/document/{documentId}
POST /api/background-jobs/{jobId}/retry
```

Trong MVP co the chi can:

```http
GET /api/background-jobs/document/{documentId}
POST /api/background-jobs/{jobId}/retry
```

Upload endpoint co the tu enqueue job, nen user khong can goi `/process` thu cong.

## Admin UI du kien

Admin page nen co:

- Job status summary:
  - pending
  - running
  - completed
  - failed
- Recent failed jobs.
- Retry failed job.
- Xem error message an toan.
- Xem document lien quan.

Employee UI:

- Chi can thay document dang Processing/Failed.
- Neu failed, co nut retry neu document trong access scope.

Guest UI:

- Khong upload nen khong can job UI.

## Test cases

- Upload file lon khong timeout.
- Status chuyen Uploaded -> Processing -> Indexed.
- Tat Python service, job failed/retry.
- File loi co log ro rang.
- Admin xem job status.
- Document soft delete qua retention bi purge file vat ly.
- Upload CSV/XLSX tu dong profile sau khi process.
- Worker restart khong lam mat pending jobs.
- Hai worker khong xu ly cung mot job.
- Retry khong tao duplicate chunks/vectors/profile.
- Employee khong retry job cua admin-level document.

## Dau hieu hoan thanh

- Upload khong bi block boi ingestion.
- Retry co ban hoat dong.
- Admin biet file nao dang xu ly/loi.
- Storage khong bi day vo han boi file deleted.
- Frontend khong con can user bam ingest/chunk/index/profile.
- Job log du de debug loi Python/OpenAI/Qdrant.
- Loi duoc luu an toan, khong leak file content/API key/cookie/token.

## Khong nen lam voi Milestone 15

- Khong can distributed queue phuc tap ngay.
- Khong can realtime websocket progress.
- Khong can dashboard realtime.
- Khong can auto-scaling worker.
- Khong can workflow engine day du.
- Khong can xoa vinh vien bang UI neu Recycle Bin chua lam.

## Thu tu lam de de hoc

1. Cai Hangfire packages:
   - `Hangfire.AspNetCore`
   - `Hangfire.SqlServer`
2. Cau hinh Hangfire trong `Program.cs`.
3. Bao ve Hangfire dashboard chi development/admin noi bo.
4. Tao entity `DocumentProcessingJob`, `DocumentProcessingJobLog`, enum status/type.
5. Them `DbSet` vao `AppDbContext`.
6. Tao migration.
7. Tao `DocumentProcessingJobService.EnqueueDocumentProcessingAsync`.
8. Sua upload flow de enqueue job sau khi tao document.
9. Tao endpoint/list job don gian de debug.
10. Tao `DocumentProcessingJobRunner`.
11. Tao `DocumentProcessingJobHandler`.
12. Chuyen logic pipeline tu frontend/manual action vao handler:
   - ingest
   - chunk
   - index
   - profile neu file bang
13. Them retry policy cua Hangfire.
14. Them job log.
15. Sua frontend DocumentsPage:
   - upload xong chi refresh/list status.
   - bo auto call ingest/chunk/index/profile o frontend.
   - hien Queued/Processing/Failed.
16. Them admin job view co ban.
17. Lam recurring purge job cho deleted documents.
18. Test file nho, file lon, Python service down, retry.

## Cap nhat thuc te sau khi trien khai

Ngay cap nhat: 2026-07-06.

Milestone 15 da duoc trien khai theo huong **Hangfire + SQL Server + app-level job table**.

Muc tieu da dat:

- Upload file khong con bat frontend goi tung API `ingest`, `chunk`, `index`, `profile`.
- Sau khi upload, backend tu tao background job.
- Hangfire chay pipeline trong nen.
- Co bang rieng de theo doi job va logs cua tung step.
- Co retry job failed.
- Frontend DocumentsPage polling job status sau upload.
- File bang `.csv`, `.xlsx`, `.xls` duoc profile tu dong sau khi index.
- File text/PDF/DOCX ket thuc o status `indexed`; day la trang thai san sang cho RAG.

## Files/modules da tao hoac sua

Backend:

- `Modules/BackgroundJobs/DocumentProcessingJob.cs`
  - Entity luu job tong cho mot document.
  - Moi job gan voi mot `DocumentId`.
  - Luu status, attempt, HangfireJobId, LastError, timestamps.

- `Modules/BackgroundJobs/DocumentProcessingJobLog.cs`
  - Entity luu log theo tung buoc cua job.
  - Dung de debug flow: enqueue, running, ingest, chunk, index, profile, complete, fail, retry.

- `Modules/BackgroundJobs/DocumentProcessingJobStatus.cs`
  - Hang so status:
    - `queued`
    - `running`
    - `completed`
    - `failed`

- `Modules/BackgroundJobs/DocumentProcessingStep.cs`
  - Hang so step:
    - `enqueue`
    - `running`
    - `ingest`
    - `chunk`
    - `index`
    - `profile`
    - `complete`
    - `fail`
    - `retry`

- `Modules/BackgroundJobs/DocumentProcessingJobService.cs`
  - Service dieu phoi job records va Hangfire enqueue/retry/status/log.

- `Modules/BackgroundJobs/DocumentProcessingJobRunner.cs`
  - Method public duoc Hangfire goi.
  - Khong doc HttpContext.
  - Chi quan ly lifecycle: running -> handler -> completed/failed.

- `Modules/BackgroundJobs/DocumentProcessingJobHandler.cs`
  - Chay pipeline thuc te:
    - ingest
    - chunk
    - index
    - profile neu file bang
  - Ghi log thanh cong/that bai cho tung step.

- `Modules/BackgroundJobs/BackgroundJobsController.cs`
  - API xem job status/logs/retry.

- `Modules/Documents/DocumentsController.cs`
  - Upload xong tu dong goi `EnqueueDocumentProcessingAsync`.

- `Contracts/Documents/DocumentUploadResponse.cs`
  - Them `ProcessingJobId`, `ProcessingJobStatus`.

- `Infrastructure/Persistence/AppDbContext.cs`
  - Them `DbSet<DocumentProcessingJob>`.
  - Them `DbSet<DocumentProcessingJobLog>`.
  - Cau hinh indexes/relationships.

- `Program.cs`
  - Dang ky Hangfire.
  - Dang ky background job services.
  - Map Hangfire dashboard.

System/internal service methods:

- `DocumentIngestionService.IngestSystemAsync`
- `DocumentChunkingService.ChunkSystemAsync`
- `DocumentIndexingService.IndexSystemAsync`
- `DatasetProfileService.ProfileSystemAsync`

Ly do co cac ham `SystemAsync`:

- API methods nhu `IngestAsync`, `ChunkAsync`, `IndexAsync`, `ProfileAsync` doc HttpContext, check current user, check role, ghi audit.
- Background job khong co HTTP request/current user.
- `SystemAsync` la logic loi cho pipeline noi bo, khong phu thuoc cookie/request hien tai.
- Wrapper API van giu de test/debug thu cong neu can.

Frontend:

- `src/api/backgroundJobsApi.ts`
  - `getLatestJobByDocument(documentId)`
  - `listJobLogs(jobId)`
  - `retryJob(jobId)`

- `src/types/backgroundJobs.ts`
  - Type cho job/log response.

- `src/features/documents/DocumentsPage.tsx`
  - Sau upload khong goi manual pipeline nua.
  - Luu documentId vao `trackedDocumentIds`.
  - Poll job status moi 3 giay.
  - Khi job completed/failed thi refresh document list va hien message.

## Database da them

`DocumentProcessingJobs`:

- `Id`
- `DocumentId`
- `HangfireJobId`
- `JobType`
- `Status`
- `AttemptCount`
- `MaxAttempts`
- `LastError`
- `CreatedAt`
- `UpdatedAt`
- `StartedAt`
- `CompletedAt`

`DocumentProcessingJobLogs`:

- `Id`
- `DocumentProcessingJobId`
- `DocumentId`
- `JobType`
- `Step`
- `Status`
- `Attempt`
- `ErrorMessage`
- `CreatedAt`
- `StartedAt`
- `CompletedAt`

Hangfire cung tu tao cac bang rieng cua no trong SQL Server de luu queue/state.

## Flow thuc te khi upload document

```text
Frontend DocumentsPage
 -> POST /api/documents/upload
 -> DocumentsController.Upload
 -> DocumentService.UploadAsync
    -> validate user/file/accessLevel
    -> save physical file
    -> insert Documents
    -> create default DocumentMetadata
    -> audit document_upload
 -> DocumentProcessingJobService.EnqueueDocumentProcessingAsync(documentId)
    -> check document exists and not deleted
    -> check active queued/running job de tranh duplicate
    -> insert DocumentProcessingJob status=queued
    -> insert DocumentProcessingJobLog step=enqueue
    -> Hangfire enqueue DocumentProcessingJobRunner.ProcessDocumentAsync(job.Id)
    -> update HangfireJobId
 -> return DocumentUploadResponse
```

Frontend sau do polling:

```text
DocumentsPage trackedDocumentIds
 -> every 3s GET /api/background-jobs/document/{documentId}
 -> queued/running: tiep tuc doi
 -> completed: refresh document list
 -> failed: hien LastError
```

## Flow thuc te trong Hangfire job

Hangfire goi:

```text
DocumentProcessingJobRunner.ProcessDocumentAsync(jobId)
```

Runner lam:

```text
MarkRunningAsync(jobId)
 -> set job.Status = running
 -> AttemptCount += 1
 -> StartedAt = now
 -> add log step=running

DocumentProcessingJobHandler.HandleAsync(jobId)
 -> chay pipeline thuc te

Neu handler success:
 -> MarkCompletedAsync(jobId)
 -> set job.Status = completed
 -> CompletedAt = now
 -> add log step=complete

Neu handler throw:
 -> MarkFailedAsync(jobId, safeMessage)
 -> set job.Status = failed
 -> LastError = safeMessage
 -> add log step=fail
 -> throw lai cho Hangfire biet job failed
```

## Flow thuc te trong DocumentProcessingJobHandler

```text
HandleAsync(jobId)
 -> query DocumentProcessingJob
 -> lay job.DocumentId
 -> query Document, Status != deleted

 -> IngestSystemAsync(document.Id)
    -> goi Python ingestion
    -> luu DocumentExtractions
    -> document.Status = extracted
    -> return DocumentIngestResponse
 -> log ingest completed/failed

 -> ChunkSystemAsync(document.Id)
    -> lay extracted text
    -> goi Python chunker
    -> replace DocumentChunks theo documentId
    -> document.Status = chunked
    -> return DocumentChunkingResponse
 -> log chunk completed/failed

 -> IndexSystemAsync(document.Id)
    -> lay chunks
    -> goi Python vector index
    -> OpenAI embedding + Qdrant upsert
    -> document.Status = indexed
    -> return DocumentIndexResponse
 -> log index completed/failed

 -> neu extension la .csv/.xlsx/.xls:
    -> ProfileSystemAsync(document.Id)
       -> goi Python dataset profile
       -> replace DocumentTableProfiles
       -> update DocumentMetadata.DetectedColumnsJson
       -> update DocumentMetadata.SheetNamesJson
       -> return DatasetProfileResponse
    -> log profile completed/failed
```

Ket qua:

- PDF/DOCX/TXT:
  - status cuoi: `indexed`.
  - khong co badge `profiled`.
  - day la binh thuong.

- CSV/XLSX/XLS:
  - status cuoi: `indexed`.
  - co them `hasTableProfile = true` neu profile thanh cong.
  - frontend hien badge `profiled`.

## API job status da co

```http
GET /api/background-jobs?status={status}
```

Lay toi da 100 jobs, co the filter theo status.

```http
GET /api/background-jobs/document/{documentId}
```

Lay job moi nhat cua mot document.

```http
GET /api/background-jobs/{jobId}/logs
```

Lay logs theo job id.

```http
POST /api/background-jobs/{jobId}/retry
```

Retry job failed.

Luu y:

- `{jobId}` la `id` cua `DocumentProcessingJob`.
- Khong dung `documentId` de retry.
- Neu truyen nham id, controller nen tra `404 job_not_found` thay vi 500.

## Hieu nang hien tai

File vai tram KB xu ly 10-20 giay la chap nhan duoc voi pipeline hien tai, vi job gom nhieu viec:

- extract text.
- chunk text.
- goi OpenAI embedding qua network.
- upsert Qdrant.
- profile bang voi pandas neu la CSV/XLSX.
- OCR co the cham hon nhieu neu PDF scan.

Chua can toi uu som, vi background job da giai quyet van de timeout/request cho user.

## Huong toi uu sau nay

Performance:

- Batch embedding nhieu chunks trong mot request thay vi goi tung chunk.
- Gioi han/chuan hoa chunk size de tranh qua nhieu chunks.
- Skip lai ingest/chunk/index neu file/hash khong doi.
- Dung file hash de detect duplicate uploads.
- Parallel worker co gioi han, vi khong nen spam OpenAI/Qdrant.
- Cau hinh Hangfire worker count rieng theo moi truong.
- Tach worker service rieng neu backend web bi qua tai.
- Dung queue rieng cho OCR neu OCR cham.

Reliability:

- Them retry policy ro hon theo step.
- Phan biet loi retryable va non-retryable.
- Them progress percentage hoac `CurrentStep`.
- Neu fail o index, retry co the bat dau lai tu index neu extraction/chunks con hop le.
- Them idempotency version/hash cho DocumentExtraction, DocumentChunk, Qdrant vectors.

Security/permission:

- Bao ve Hangfire dashboard chi admin/internal network.
- `GET /api/background-jobs` chi admin.
- Employee chi xem/retry jobs trong access scope employee/guest.
- Guest/anonymous khong xem job status ngoai document guest neu sau nay can.
- Khong log extracted text, chunk content, API key, cookie, token vao job logs.

UX/frontend:

- Hien status `Processing...` ngay tren document row.
- Hien current step: ingesting/chunking/indexing/profiling.
- Hien nut retry khi job failed.
- Hien job logs o admin page.
- Neu user reload page, frontend co the doc latest job status cho moi document dang queued/running.
- Sau nay co the dung SignalR/SSE thay polling neu can realtime.

Operations:

- Them dashboard job summary:
  - queued
  - running
  - completed
  - failed
- Alert neu nhieu job failed lien tiep.
- Theo doi duration trung binh theo file type.
- Theo doi token/embedding usage rieng cho indexing.

Delete/purge:

- Lam recurring purge job cho deleted documents sau retention period.
- Admin/employee restore/delete permanently theo access scope da chot:
  - admin restore/permanent delete tat ca.
  - employee restore/permanent delete document cap employee/guest.
  - guest/anonymous khong restore/delete.

Cost control:

- Them rate limit/queue limit cho guest upload neu sau nay guest duoc upload.
- Them token budget cho embedding/indexing theo ngay/thang.
- Gioi han kich thuoc file va so file upload dong thoi.

## Dieu can nho khi phong van

Co the giai thich ngan gon:

"Ban dau pipeline document duoc goi truc tiep qua HTTP, user hoac frontend phai kich tung buoc ingest/chunk/index/profile. Cach nay de timeout, kho retry va kho debug. Em tach pipeline sang Hangfire background job. Khi upload, backend chi luu file va enqueue job. Hangfire worker xu ly ingest, chunk, index, profile trong nen. Em luu app-level job va job logs trong SQL Server de frontend/admin xem status, retry job failed, va debug fail o step nao. Frontend chi polling job status thay vi chay pipeline truc tiep."
