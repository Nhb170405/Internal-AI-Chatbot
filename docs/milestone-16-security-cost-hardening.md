# Milestone 16: Security And Cost Hardening

Trang thai: Completed

Ngay cap nhat: 2026-07-07

## Muc tieu

- Chuan bi gan production.
- Kiem soat spam, token usage, secret, file upload, audit.
- Giam rui ro prompt injection va data leakage.

## Ly do lam milestone nay

- AI chatbot co the ton chi phi nhanh neu bi spam.
- Upload file va RAG co rui ro bao mat.
- Internal data can audit trail va permission ro rang.

## Kien thuc can hoc

- Rate limiting.
- Secret management.
- Audit logging.
- Token usage/quota.
- Input validation.
- Prompt injection.

## Pham vi nen lam

- Basic rate limit theo user/guest/ip.
- Token quota theo role/user.
- Guest chat limit:
  - gioi han so message theo guest session.
  - gioi han token/context size rieng cho guest.
  - gioi han tan suat request theo guest session va IP.
  - neu guest vuot limit thi tra loi ro rang, khong goi OpenAI nua.
- Secret config chuan cho deploy.
- File validation hardening.
- Audit log action quan trong.
- Prompt injection guard co ban.
- Document retention policy va purge policy.
- Recycle Bin security:
  - admin restore/purge tat ca deleted documents.
  - employee restore/purge deleted documents trong access scope employee/guest.
  - guest/anonymous khong restore/purge.
  - permanent delete phai co audit log va confirm ro rang.

Quyet dinh ngay 2026-07-07:

- Chua lam Token Budget Guard trong giai doan nay.
- Ly do: he thong dang la internal app, employee/admin cung mot loi ich nghiep vu, da co audit log va token usage de truy vet.
- Guest tam thoi duoc bao ve bang rate limit. Sau nay neu public guest rong hon thi moi them quota theo guest session/ngay.
- TokenUsage van tiep tuc duoc luu de quan sat va lam nen cho quota sau nay.

## Security checklist

- API key khong log.
- Cookie HttpOnly.
- Secure cookie production.
- CSRF strategy neu dung browser frontend.
- File extension + MIME + size validation.
- Path traversal blocked.
- User khong doc duoc document/session cua user khac.
- Qdrant filter permission.
- Audit log khong chua secret.
- Deleted document bi an khoi list/search/RAG.
- CORS khong dung `AllowAnyOrigin` khi dung cookie.
- Security headers baseline can co truoc production.

## Cost/token checklist

- TokenUsage duoc luu.
- Quota theo user/role. Trang thai: deferred.
- Rate limit chat/RAG request.
- Rate limit guest chat theo guest session/ip de tranh spam token.
- Limit topK/context size.
- Limit upload/page count/OCR page.
- Limit dataset analysis/chart size.

## Test cases

- User spam request bi limit.
- Guest spam chat bi limit va response khong goi OpenAI.
- API key khong lo trong logs.
- File sai type bi chan.
- User vuot quota bi tu choi.
- Prompt yeu cau bo qua rule khong lam bot leak data ngoai context.
- Document da Deleted khong xuat hien trong RAG.
- Employee khong restore/purge admin-level deleted document.
- Admin restore/purge duoc moi deleted document.

## Dau hieu hoan thanh

- Co kiem soat token usage.
- Co audit trail.
- Co chinh sach bao mat ro rang.
- Test abuse co ban pass.

## Cach lam trong du an nay

Milestone 16 se lam cham va uu tien hoc pattern nen tang. Khong copy mot luc nhieu logic vao project.

Quy tac trien khai:

- Tao skeleton truoc: namespace, class, method, comment giai thich.
- Chua wire middleware/rate limit vao `Program.cs` neu logic chua xong.
- Moi lan chi implement mot module nho.
- Sau moi module phai build va test endpoint bi anh huong.
- Khong de code security moi lam hong cac flow da chay on dinh.

Thu tu thuc hien cham:

1. Global error handling.
2. Rate limiting.
3. Upload hardening.
4. Security headers / CORS review.
5. Security headers baseline.
6. Prompt/RAG safety baseline. Trang thai: deferred.
7. Audit log cleanup. Trang thai: implemented.
8. Admin usage API. Trang thai: implemented.
9. Security test matrix. Trang thai: deferred.

## Skeleton files da tao cho Milestone 16

Backend skeleton:

- `Infrastructure/Errors/ApiErrorResponse.cs`
  - DTO chung cho error response.
  - Muc tieu sau nay moi loi API tra ve dang `{ code, message, traceId }`.

- `Infrastructure/Errors/GlobalExceptionMiddleware.cs`
  - Middleware bat exception o mot noi.
  - Chua duoc gan vao `Program.cs` cho den khi implement xong.

- `Modules/Documents/FileValidationOptions.cs`
  - Config cho file upload: max size, allowed extensions, blocked extensions.

- `Modules/Documents/FileValidationService.cs`
  - Service validate file upload.
  - Sau nay `DocumentService.UploadAsync` se goi service nay thay vi scattered validation.

- `Modules/Usage/TokenBudgetOptions.cs`
  - Config quota token theo role.

- `Modules/Usage/TokenBudgetService.cs`
  - Service check token usage truoc khi goi OpenAI.

- `Modules/Safety/PromptSafetyResult.cs`
  - Result object cho prompt safety check.

- `Modules/Safety/PromptSafetyService.cs`
  - Rule-based baseline de detect prompt injection ro rang.

Chua register vao DI/pipeline:

- `TokenBudgetService`
- `PromptSafetyService`

Ly do:

- `GlobalExceptionMiddleware` va `FileValidationService` da duoc implement va wire vao he thong.
- `TokenBudgetService` va `PromptSafetyService` chua dung trong runtime.
- Milestone nay co nhieu pattern moi, nen moi phan se duoc wire vao he thong sau khi da hieu va test rieng.

## Trang thai implement hien tai

### 1. Global error handling

Da lam:

- Tao custom exception base va cac exception cu the:
  - `ApiException`
  - `NotFoundApiException`
  - `ValidationApiException`
  - `ForbiddenApiException`
  - `ConflictApiException`
  - `ExternalServiceApiException`
  - `UnauthorizedApiException`
- Tao `ApiErrorResponse` gom:
  - `Code`
  - `Message`
  - `TraceId`
- Tao `GlobalExceptionMiddleware` va gan vao `Program.cs`.
- Refactor nhieu controller de bot try/catch lap lai.
- Service throw custom exception voi error code ro rang.

Flow:

```text
Controller/Service throw ApiException
-> GlobalExceptionMiddleware bat exception
-> Map sang HTTP status code
-> Tra JSON { code, message, traceId }
```

Vi du:

```json
{
  "code": "document_not_found",
  "message": "Document not found.",
  "traceId": "..."
}
```

Ly do thiet ke:

- Controller khong can try/catch lap lai.
- Loi co format thong nhat.
- `traceId` giup debug log/request sau nay.
- Moi exception co `code` rieng de frontend hien thi dung thong diep.

### 2. Rate limiting

Da lam:

- Cau hinh `builder.Services.AddRateLimiter(...)` trong `Program.cs`.
- Them middleware `app.UseRateLimiter()`.
- Them `app.UseRouting()` truoc `UseCors/UseAuthentication/UseRateLimiter`.
- Tao 3 policy:
  - `auth-login`: 5 request / 1 phut / IP.
  - `chat`: 30 request / 1 phut / user/guest/ip.
  - `upload`: 10 request / 10 phut / user/guest/ip.
- Gan `[EnableRateLimiting(...)]` vao:
  - `AuthController`: `guest-login`, `login`.
  - `ChatController`: basic chat.
  - `ChatSessionsController`: session message.
  - `RagController`: RAG chat.
  - `AssistantController`: assistant chat.
  - `DocumentsController`: upload.

Flow:

```text
Request vao endpoint co EnableRateLimiting
-> ASP.NET lay policy
-> Lay partition key:
   - auth-login: IP
   - chat/upload: userId, neu khong co thi guestSessionId, neu khong co thi IP
-> Neu con quota thi di tiep
-> Neu vuot quota thi tra 429
```

Response khi vuot limit:

```json
{
  "code": "rate_limit_exceeded",
  "message": "Too many requests. Please try again later.",
  "traceId": "..."
}
```

Ly do thiet ke:

- Login dung IP vi luc login chua chac co user identity.
- Chat/upload dung user/guest/ip de tranh mot user spam anh huong user khac.
- `QueueLimit = 0` de request vuot limit bi tu choi ngay, khong treo cho.

### 3. Upload hardening

Da lam:

- Tao `FileValidationOptions`.
- Tao `FileValidationService`.
- Dang ky DI:
  - `builder.Services.Configure<FileValidationOptions>(...)`
  - `builder.Services.AddScoped<FileValidationService>()`
- `DocumentService.UploadAsync` goi `_fileValidationService.ValidateUpload(file)` truoc khi luu file.
- `DocumentService.UploadAsync` dung:
  - `GetSafeOriginalFileName(file.FileName)`
  - `GetNormalizedExtension(originalFileName)`
- `LocalFileStorageService.SaveAsync` van giu check extension/size phong ve them.

Validation hien tai:

- File bat buoc co gia tri.
- File khong duoc rong.
- File khong vuot `MaxFileSizeBytes`.
- Extension phai nam trong allowlist.
- Extension nguy hiem nam trong blocklist bi chan.
- Content-Type duoc check nhu lop phong ve bo sung.
- Extension va content type mismatch ro rang bi chan.
- Original file name duoc clean bang `Path.GetFileName`.
- Stored file name van dung `documentId + extension`, khong dung ten file user gui len.

Flow:

```text
DocumentsController.Upload
-> DocumentService.UploadAsync
-> FileValidationService.ValidateUpload
   -> EnsureValidSize
   -> GetSafeOriginalFileName
   -> GetNormalizedExtension
   -> EnsureAllowedExtension
   -> EnsureAllowedContentType
   -> EnsureExtensionMatchesContentType
-> LocalFileStorageService.SaveAsync
-> Save Document metadata vao SQL
-> Create default DocumentMetadata
-> Audit document_upload
-> Enqueue background processing job
```

Test da pass:

- Upload file hop le.
- Upload request khong co file field: ASP.NET model binding tra 400 mac dinh.
- Upload file rong that: `invalid_file`.
- Upload extension bi chan: `unsupported_file_type`.
- Upload extension khong ho tro: `unsupported_file_type`.
- Upload file qua lon: `file_too_large`.

Luu y:

- Request khong co field `File` bi ASP.NET model binding chan truoc khi vao service.
- Neu muon response cung format `{ code, message, traceId }`, sau nay can custom `InvalidModelStateResponseFactory`.
- Content-Type do client gui len, khong duoc tin tuyet doi. Muon chat hon can magic-byte sniffing hoac antivirus scan.

### 4. Security headers / CORS review

Trang thai: reviewed.

CORS hien tai trong `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

Danh gia:

- Tot cho development vi frontend Vite chay o `http://localhost:5173`.
- Dung voi cookie auth vi co `AllowCredentials()`.
- Khong dung `AllowAnyOrigin()`, day la diem dung. Cookie auth + allow any origin la rui ro lon.
- Diem can cai thien truoc production:
  - Dua allowed origins vao config thay vi hard-code.
  - Tach policy dev/prod.
  - Chi allow domain frontend noi bo that.

Middleware order hien tai:

```text
GlobalExceptionMiddleware
Swagger/Hangfire dashboard trong Development
UseHttpsRedirection
UseRouting
UseCors("FrontendDev")
UseAuthentication
UseRateLimiter
UseAuthorization
MapControllers
```

Danh gia:

- `UseRouting` truoc `UseCors`/`UseRateLimiter` la dung.
- `UseAuthentication` truoc `UseAuthorization` la dung.
- `UseRateLimiter` sau authentication giup policy chat/upload co the partition theo user/guest.
- Hangfire dashboard chi bat trong Development la tam on, production can admin auth filter.

### 5. Security headers baseline

Trang thai: implemented va test pass.

Da tao:

- `Infrastructure/Security/SecurityHeadersMiddleware.cs`

Da gan vao `Program.cs`:

```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
```

Ly do dat `SecurityHeadersMiddleware` truoc `GlobalExceptionMiddleware`:

- Moi response di qua pipeline se co co hoi nhan security headers.
- Ke ca response loi do global middleware tra ve cung nen co security headers.

Headers hien tai:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

Test da pass bang `GET /api/health`.

Response headers quan sat duoc:

```text
x-content-type-options: nosniff
x-frame-options: DENY
referrer-policy: no-referrer
permissions-policy: camera=(),microphone=(),geolocation=()
```

Luu y quan trong:

- Trong middleware chi duoc goi `await _next(context)` mot lan.
- Neu goi `_next(context)` nhieu lan, request co the bi xu ly lap lai, gay loi nghiem trong nhu ghi DB/upload/enqueue job nhieu lan.
- Chua them `Content-Security-Policy` trong milestone nay vi CSP de lam hong Swagger/frontend dev neu cau hinh sai.

Khuyen nghi:

- Chua can them CSP phuc tap ngay.
- Production nen dung HTTPS bat buoc va cookie secure always.

### 6. Prompt/RAG safety baseline

Trang thai: deferred.

Muc tieu:

- Giam rui ro user yeu cau bot bo qua system prompt, tiet lo API key, tiet lo noi dung ngoai quyen.
- Khong coi rule-based safety la phong ve duy nhat. Phong ve quan trong nhat van la permission filter va chi dua context duoc phep vao prompt.

Du kien lam:

- Review `PromptBuilder`, `RagService`, `AssistantService`.
- Tao/gian tiep dung `PromptSafetyService` de detect cac pattern ro rang:
  - "ignore previous instructions"
  - "show system prompt"
  - "reveal API key"
  - "bo qua rule"
  - "tra loi bang du lieu ngoai tai lieu"
- Neu detect prompt nguy hiem:
  - co the reject request bang `ValidationApiException("unsafe_prompt", "...")`, hoac
  - log warning va van chay voi prompt rule chat hon.
- Audit log chi luu thong tin an toan:
  - actor
  - route
  - message length
  - safety flags
  - khong log full prompt/context dai.

Trade-off:

- Regex/rule-based guard de lam nhung khong chan duoc moi prompt injection.
- LLM classifier chinh xac hon nhung ton token va them latency.
- Milestone nay nen lam baseline don gian truoc.

Quyet dinh ngay 2026-07-07:

- Chua implement prompt safety guard rieng trong runtime.
- Ly do:
  - He thong dang phuc vu noi bo.
  - Lop phong ve quan trong nhat da co: authentication, document access level, Qdrant permission filter, RAG chi dua allowed context vao prompt.
  - Them rule-based guard som co the lam tang do phuc tap va false positive.
- Sau nay neu public guest rong hon hoac co yeu cau compliance cao hon thi moi bat lai `PromptSafetyService`.

### 7. Audit log cleanup

Trang thai: implemented.

Muc tieu:

- Dam bao audit log huu ich cho debug/security nhung khong lam ro ri du lieu noi bo.

Can review:

- `AuditLogService`
- Cac noi goi `LogAsync`
- MetadataJson trong:
  - auth
  - chat
  - RAG
  - assistant
  - document upload/delete/restore
  - background jobs
  - dataset/chart

Quy tac:

- Khong log password, cookie, token, API key.
- Khong log full prompt.
- Khong log full extracted text, chunk content, RAG context.
- Khong log storage path local neu khong can.
- Chi log:
  - ids
  - role
  - action
  - resource type/id
  - status
  - token count
  - message length
  - document/file metadata ngan gon.

Ket qua mong muon:

- MetadataJson ngan, an toan, co ich.
- Co checklist nhung field duoc phep log.
- Cac action quan trong co audit trail.

Da review cac noi goi `AuditLogService.LogAsync`:

- Auth:
  - Guest login chi log `displayNameProvided`.
  - Employee/admin login success/fail log email da mask va reason an toan.
  - Khong log password.
- Chat/RAG/Assistant:
  - Log role, route, message/question length, topK, token usage, citation count.
  - Khong log full user message.
  - Khong log full prompt, RAG context, extracted text, chunk content.
- Documents:
  - Log filename, extension, size, content type, status, access level.
  - Khong log storage path.
  - Khong log noi dung file.
- Ingestion/chunking/indexing:
  - Log parser/chunk/index status, count, collection name, error message ngan.
  - Khong log extracted text/chunk content/vector.
- Dataset/chart:
  - Log operation, sheet/value/group fields, topN, success.
  - Khong log raw rows.

Da nang cap `AuditLogService`:

- Metadata sanitizer chay tap trung truoc khi luu SQL.
- Redact key nhay cam o ca object long nhau:
  - password
  - token
  - cookie
  - secret
  - api key
  - authorization
  - credential
  - connection string
  - session key
  - file/storage/full path
  - prompt/context/query/extracted text/chunk/raw content
- Gioi han chuoi metadata dai:
  - moi string toi da 500 ky tu.
  - array toi da 20 phan tu.
  - metadata JSON toi da 4000 ky tu, neu qua thi chi luu `{ truncated, originalLength }`.

Ly do thiet ke:

- Tung service van phai chu dong khong gui du lieu nhay cam vao audit.
- `AuditLogService` la lop phong ve cuoi cung neu developer sau nay vo tinh truyen metadata xau.
- Lam o mot noi giup module moi sau nay tu dong duoc bao ve.

Luu y:

- `originalFileName` van duoc log vi huu ich cho audit van hanh document.
- Neu cong ty coi ten file la nhay cam, co the doi sang log `fileNameLength` hoac hash filename.
- `errorMessage` duoc giu lai nhung bi truncate. Service ben duoi khong nen nem stack trace/path noi bo vao errorMessage.

### 8. Admin usage API

Trang thai: implemented.

Muc tieu:

- Admin xem duoc usage/token/job/document/chat overview de van hanh he thong.
- Khong can tinh tien thanh chi phi that trong milestone nay; chi can token/request/job/file metrics.

Da co mot phan tu milestone frontend/admin:

- Admin dashboard UI co baseline.
- TokenUsage da duoc luu tu chat/OpenAI response.
- Background job status da co API rieng.

Du kien bo sung/review:

- API tong hop token usage theo ngay/user/role.
- API tong hop so request/chat/RAG/assistant.
- API tong hop document count theo status:
  - uploaded
  - processing
  - indexed
  - failed
  - deleted
- API tong hop failed jobs gan day.

Quy tac bao mat:

- Chi admin duoc xem usage toan he thong.
- Employee neu can thi chi xem usage cua scope rieng, nhung co the defer.
- Response khong tra raw prompt/context.

Da implement trong endpoint co san:

```text
GET /api/admin/usage?from=...&to=...
```

Controller:

- `AdminDashboardController.Usage(...)`

Service:

- `AdminDashboardService.GetUsageAsync(...)`

Response:

- Tong token/request:
  - `totalRequests`
  - `totalPromptTokens`
  - `totalCompletionTokens`
  - `totalTokens`
- Token theo actor:
  - `byActor`
  - gom user, guest session hoac unknown actor.
- Token theo model:
  - `byModel`
- Token theo ngay:
  - `byDay`
  - dung de ve chart usage theo ngay sau nay.
- Audit/action count:
  - `auditActions`
  - dem action/resourceType de admin biet he thong dang duoc dung vao viec gi.
- Document health:
  - `documentStatusCounts`
  - dem uploaded/processing/extracted/chunked/indexed/failed/deleted.
- Background job health:
  - `backgroundJobStatusCounts`
  - dem queued/running/completed/failed.
  - `recentFailedJobs`
  - tra 10 job failed gan nhat de admin debug nhanh.

Ly do thiet ke:

- Khong tao bang moi vi tat ca so lieu can thiet da nam trong:
  - `TokenUsages`
  - `AuditLogs`
  - `Documents`
  - `DocumentProcessingJobs`
- `Admin Usage API` chi query/tong hop, khong ghi du lieu.
- Response khong tra raw prompt, raw answer, extracted text, chunk content hay metadata nhay cam.
- Endpoint da co `[Authorize(Roles = UserRole.Admin)]`, chi admin xem duoc usage toan he thong.

Trade-off:

- `byDay` dang group trong C# sau khi query token rows. Cach nay de doc va on cho MVP vi `TokenUsages` thuong nho hon document/chunk.
- Neu usage tang rat lon, sau nay co the toi uu bang SQL group-by theo date hoac tao bang daily aggregate.

### 9. Test security matrix

Trang thai: deferred.

Muc tieu:

- Tao bang test abuse/security co he thong thay vi test cam tinh.

Nhom test can co:

- Auth:
  - anonymous khong chat/upload/documents private.
  - guest chi doc/chat theo scope guest.
  - employee khong doc admin-level document.
  - admin doc duoc moi scope.
- Upload:
  - file rong.
  - file qua lon.
  - extension bi chan.
  - extension khong ho tro.
  - content type mismatch.
- RAG/search:
  - document deleted khong xuat hien.
  - guest khong retrieve employee/admin chunks.
  - employee khong retrieve admin chunks.
- Rate limit:
  - login spam bi 429.
  - chat spam bi 429.
  - upload spam bi 429.
- Error handling:
  - not found tra custom JSON.
  - validation tra custom JSON.
  - external service fail khong leak stack trace.
- Audit:
  - khong log secret.
  - khong log full document content.
  - action quan trong co log.

Ket qua mong muon:

- Co checklist manual test trong docs.
- Sau nay co the chuyen mot phan thanh integration tests.

Quyet dinh ngay 2026-07-07:

- Khong tao test matrix rieng trong milestone nay.
- Ly do:
  - Moi API/module/tinh nang da duoc test thu cong ngay sau khi hoan thanh trong qua trinh phat trien.
  - Tao lai mot matrix day du vao luc nay se lap lai nhieu cong viec da lam va lam cham tien do.
  - Du an van dang hoc va thay doi nhanh, integration test tu dong nen de sau khi API on dinh hon.
- Huong nang cap sau nay:
  - Tao `docs/security-test-matrix.md` neu chuan bi demo/bao ve/deploy.
  - Viet integration tests cho auth/document/upload/RAG/rate limit.
  - Dung test data rieng cho guest/employee/admin va document access level.

## Ket luan Milestone 16

Milestone 16 duoc coi la hoan thanh o muc baseline hardening.

Da hoan thanh:

- Global error handling.
- Rate limiting cho login/chat/upload.
- Upload hardening.
- Security headers baseline.
- CORS review cho frontend dev.
- Audit log cleanup va sanitizer tap trung.
- Admin usage API.

Tam hoan co chu dich:

- Token Budget Guard:
  - Ly do: internal app, employee/admin cung loi ich, da co audit/token usage de truy vet.
  - Guest hien duoc bao ve bang rate limit.
- Prompt/RAG Safety Guard:
  - Ly do: lop phong ve chinh la permission filter, allowed context, Qdrant access filter.
  - Rule-based guard co the them sau khi public guest rong hon.
- Security Test Matrix rieng:
  - Ly do: da test tung API/module ngay khi lam.
  - Se lam lai khi chuan bi production/demo lon.

Dau hieu hoan thanh hien tai:

- API loi co format thong nhat.
- Upload file co validation ro hon.
- Chat/upload/login co rate limit baseline.
- Audit log an toan hon, khong luu noi dung nhay cam dai.
- Admin co endpoint tong hop usage/health.
- Cac quyet dinh deferred da duoc ghi lai de khong that lac context.

## Huong dan hoc tung module

### 1. Global error handling

Can hoc:

- Middleware la gi.
- `RequestDelegate next` la gi.
- Vi sao controller/service throw exception nhung API van can response JSON dep.
- Mapping exception sang HTTP status code.

Ket qua mong muon:

- `KeyNotFoundException` -> 404.
- `UnauthorizedAccessException` -> 401/403 tuy ngu canh.
- `ArgumentException` -> 400.
- `InvalidOperationException` -> 409.
- Exception khong biet -> 500 message chung.

Chua lam voi production ngay:

- Chua custom logging phuc tap.
- Chua problem details full RFC.

### 2. File validation hardening

Can hoc:

- Khong tin `ContentType` tu browser.
- File extension can normalize.
- Stored filename phai dung GUID, khong dung original filename.
- Path traversal la gi.

Ket qua mong muon:

- File qua lon bi chan.
- Extension nguy hiem bi chan.
- Extension khong nam trong allowlist bi chan.
- Error message ro rang.

### 3. Token budget

Can hoc:

- Token usage da luu o dau.
- Query tong token theo ngay/user/guest.
- Check quota truoc khi goi OpenAI.
- Luu usage sau khi OpenAI tra ve.

Ket qua mong muon:

- Guest vuot quota thi khong goi OpenAI nua.
- Employee/admin co quota cao hon.

### 4. Prompt safety baseline

Can hoc:

- Prompt injection khong the chan tuyet doi bang regex.
- Rule-based guard chi la lop phong ve dau tien.
- Permission filter/RAG context moi la phong ve quan trong nhat.

Ket qua mong muon:

- Detect cac cau yeu cau lo system prompt/API key/bo qua rule ro rang.
- Log warning an toan.
- Khong dua data ngoai quyen vao prompt.
