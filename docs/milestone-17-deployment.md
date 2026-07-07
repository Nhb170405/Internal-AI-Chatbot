# Milestone 17: Azure Deployment

Trang thai: Planned

Ngay cap nhat: 2026-07-07

## Muc tieu

- Deploy Factory Chatbot V2 len cloud Azure de co URL that.
- Dung Azure SQL thay SQL Server local.
- Dung Azure Blob Storage thay local file storage.
- Deploy duoc backend ASP.NET Core, frontend React, Python FastAPI, Qdrant va Hangfire pipeline.
- Secrets khong hard-code trong source code.
- Sau khi restart service, database/file/vector/job data van con.
- Co tai lieu huong dan de nguoi khac clone repo, cau hinh va deploy lai duoc.

## Pham vi milestone

Milestone nay khong them tinh nang AI moi. Muc tieu chinh la dua he thong hien tai tu local sang moi truong gan production.

Can deploy/cau hinh:

- ASP.NET Core backend.
- React frontend.
- Python FastAPI service.
- Azure SQL Database.
- Azure Blob Storage.
- Qdrant.
- Hangfire background jobs.
- OpenAI API config.
- Health checks va smoke test.

## Quyet dinh kien truc

Quyet dinh ngay 2026-07-07:

- Uu tien deploy theo huong Azure-first.
- Frontend nen build thanh static files va serve chung domain voi ASP.NET Core backend trong giai doan dau.
- Ly do:
  - Cookie-session de chay hon khi frontend va backend cung domain.
  - Giam do phuc tap CORS/cross-site cookie.
  - Backend co the expose API `/api/*` va fallback ve React app.
- Sau nay co the tach frontend sang Azure Static Web Apps neu can.
- File upload se chuyen tu local storage sang Azure Blob Storage.
- Python service khong nen doc duong dan local cua backend tren cloud.
- Huong production cho ingestion:
  - Backend upload file vao Azure Blob.
  - Backend tao SAS URL ngan han cho file.
  - Backend gui SAS URL sang Python service.
  - Python tai file tam thoi ve container de parse/OCR.
- Qdrant:
  - De production gon nhat: dung Qdrant Cloud neu co the.
  - Neu muon tu host: dung container + persistent volume tren Azure, nhung van hanh kho hon.

## Text diagram

```text
User Browser
  |
  | HTTPS
  v
ASP.NET Core App Service / Container App
  |
  |-- serves React static files
  |-- exposes /api/*
  |-- Hangfire Dashboard /hangfire
  |
  |----> Azure SQL Database
  |        - app data
  |        - Hangfire tables
  |        - document metadata
  |        - chat history
  |        - audit logs
  |
  |----> Azure Blob Storage
  |        - uploaded raw files
  |        - optional generated files later
  |
  |----> Python FastAPI Container App
  |        - ingestion
  |        - OCR
  |        - chunking
  |        - embedding/index/search helper
  |        - dataset profiling/analysis
  |        - chart rendering
  |
  |----> Qdrant Cloud / Qdrant Container
  |        - vector points
  |        - payload filter by accessLevel
  |
  |----> OpenAI API
           - chat
           - embedding
```

## Domain

Khong bat buoc mua domain rieng cho demo/do an.

Co the dung domain mac dinh mien phi cua Azure:

- App Service: `https://your-app.azurewebsites.net`
- Azure Static Web Apps: `https://xxx.azurestaticapps.net`
- Azure Container Apps: `https://xxx.azurecontainerapps.io`
- Azure Storage static website: `https://xxx.web.core.windows.net`

Custom domain rieng nhu `factorychatbot.com` thuong phai mua. App Service custom domain cung yeu cau App Service plan phu hop, khong nen dua vao free tier de lam production demo.

Quyet dinh hien tai:

- Giai doan dau dung domain mac dinh cua Azure.
- Sau khi he thong on dinh moi tinh den custom domain.

## Kien thuc can hoc

- Azure App Service hoac Azure Container Apps.
- Azure SQL Database.
- Azure Blob Storage.
- App Settings / environment variables tren Azure.
- Dockerfile cho Python service.
- Health checks.
- Production connection string.
- Cookie auth khi chay HTTPS.
- CORS khi frontend/backend tach domain.
- Hangfire khi chay tren cloud.
- Secret management.
- Log va troubleshooting tren Azure.

## Services can deploy

### 1. ASP.NET Core backend

Trach nhiem:

- Auth cookie-session.
- API gateway cho frontend.
- Document upload/list/delete/restore.
- Chat/RAG/assistant routing.
- Dataset/chart/admin API.
- Hangfire job enqueue va worker runner.
- Ket noi Azure SQL, Azure Blob, Python, Qdrant/OpenAI.

Can cau hinh:

- Connection string den Azure SQL.
- OpenAI API key.
- Python service base URL.
- Qdrant URL/API key neu co.
- Azure Blob Storage config.
- Cookie secure settings.
- Rate limit/security headers.

### 2. React frontend

Trach nhiem:

- Login page.
- Chat workspace.
- Documents page.
- Datasets page.
- Charts page.
- Admin dashboard co ban.

Deploy option giai doan dau:

- Build frontend bang `npm run build`.
- Copy output `dist` vao `backend-dotnet/wwwroot`.
- Backend serve static files va fallback ve `index.html`.

Ly do:

- Cung domain voi backend.
- Cookie auth don gian hon.
- It phai cau hinh CORS hon.

### 3. Python FastAPI service

Trach nhiem:

- Parse TXT/PDF/DOCX/CSV/XLSX.
- OCR PDF scan/image.
- Chunk text.
- Embedding/Qdrant helper.
- Dataset profiling/analysis.
- Chart render.

Can Dockerfile vi:

- Python dependency can dong goi on dinh.
- OCR can system package `tesseract-ocr`.
- Can language data `vie` va `eng`.
- Cloud host khong co san cac package nay nhu may local.

### 4. Azure SQL Database

Trach nhiem:

- Luu user/session/audit/chat/document/job/usage.
- Luu Hangfire tables.

Can lam:

- Tao Azure SQL Server.
- Tao Azure SQL Database.
- Mo firewall cho Azure services hoac IP dev khi migrate.
- Chay EF Core migrations.
- Seed admin/employee dev/prod co kiem soat.

### 5. Azure Blob Storage

Trach nhiem:

- Luu file raw upload.
- Sau nay co the luu generated chart/export files.

Thay doi so voi local:

- Khong luu file vao `storage/uploads` tren App Service nua.
- Document metadata nen luu `BlobContainer`, `BlobName`, `StoredFileName`, `ContentType`, `SizeBytes`.
- Backend tao SAS URL ngan han cho Python doc file khi can ingest.

### 6. Qdrant

Trach nhiem:

- Luu vector points cua document chunks.
- Search semantic theo query embedding.
- Filter theo `accessLevel`.

Deploy options:

- Qdrant Cloud: de van hanh hon.
- Self-host Qdrant container tren Azure: can persistent volume, backup va monitoring rieng.

Khuyen nghi:

- Dung Qdrant Cloud neu muc tieu la demo/deploy on dinh.

### 7. OpenAI API

Trach nhiem:

- Chat completion.
- Embedding.

Can luu y:

- API key chi dat trong Azure App Settings / environment variables.
- Khong log API key, prompt dai, extracted text, chunk content.
- Neu sau nay public guest rong hon, can token budget guard.

## Environment variables du kien

Backend:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=...
OpenAI__ApiKey=...
OpenAI__BaseUrl=https://api.openai.com/v1
OpenAI__ChatModel=...
OpenAI__EmbeddingModel=text-embedding-3-small
PythonService__BaseUrl=https://your-python-service...
Qdrant__Url=...
Qdrant__ApiKey=...
Qdrant__Collection=internal_documents
AzureBlobStorage__ConnectionString=...
AzureBlobStorage__ContainerName=uploaded-documents
FileValidation__MaxFileSizeBytes=...
```

Python:

```text
OPENAI_API_KEY=...
OPENAI_EMBEDDING_MODEL=text-embedding-3-small
QDRANT_URL=...
QDRANT_API_KEY=...
QDRANT_COLLECTION=internal_documents
TEMP_FILE_ROOT=/tmp/factory-chatbot
TESSERACT_LANG=vie+eng
```

Frontend build:

```text
VITE_API_BASE_URL=
```

Neu frontend serve chung backend thi `VITE_API_BASE_URL` co the de rong hoac dung relative URL.

## File/module can tao hoac sua

Backend storage abstraction:

```text
backend-dotnet/Infrastructure/Storage/IFileStorageService.cs
backend-dotnet/Infrastructure/Storage/LocalFileStorageService.cs
backend-dotnet/Infrastructure/Storage/AzureBlobStorageService.cs
backend-dotnet/Infrastructure/Storage/AzureBlobStorageOptions.cs
```

Backend deployment:

```text
backend-dotnet/Program.cs
backend-dotnet/appsettings.Production.json
backend-dotnet/Dockerfile
backend-dotnet/wwwroot/
```

Python deployment:

```text
ai-service-python/Dockerfile
ai-service-python/requirements.txt
ai-service-python/app/config/settings.py
ai-service-python/app/main.py
```

Docs/config:

```text
.env.example
docs/deployment-azure.md
docs/milestone-17-deployment.md
README.md
```

## Thu tu thuc hien de an toan

### Buoc 1: Chuan hoa config production

Muc tieu:

- Tat ca config quan trong doc tu environment variables.
- Local dev van chay duoc bang user-secrets/appsettings.Development.

Can test:

- Backend run local binh thuong.
- Python run local binh thuong.
- Sai/missing config thi bao loi ro rang.

### Buoc 2: Them storage abstraction

Muc tieu:

- Controller/service khong phu thuoc truc tiep vao local file system.
- Co the thay `LocalFileStorageService` bang `AzureBlobStorageService`.

Flow moi:

```text
DocumentService.UploadAsync
 -> validate file
 -> IFileStorageService.SaveAsync
 -> save document metadata
 -> enqueue background job
```

### Buoc 3: Implement Azure Blob Storage

Muc tieu:

- Upload file len Blob.
- Luu metadata vao SQL.
- Tao SAS URL ngan han cho Python khi can ingest.

Can test:

- Upload PDF/XLSX len Blob.
- File ton tai trong container.
- Document list van hien dung.
- Delete soft khong xoa Blob ngay.

### Buoc 4: Sua ingestion pipeline dung Blob/SAS URL

Muc tieu:

- Python khong can doc path local cua backend.
- Backend gui `fileUrl` hoac Python request co the tai file tam thoi tu Blob.

Flow:

```text
Hangfire job
 -> Backend tao SAS URL ngan han
 -> Python /ingest nhan fileUrl
 -> Python download file vao temp
 -> parser/OCR
 -> tra extracted text
 -> Backend luu DocumentExtraction
```

### Buoc 5: Dockerize Python service

Muc tieu:

- Python service chay duoc trong container.
- Co Tesseract OCR va ngon ngu `vie+eng`.

Can test:

- `/health` OK.
- Parse TXT/DOCX/PDF.
- OCR PDF scan.
- Dataset profile/analyze.
- Chart render.

### Buoc 6: Deploy Azure SQL

Muc tieu:

- Database production co schema day du.
- Hangfire tables tao duoc.

Can test:

- `dotnet ef database update` thanh cong.
- Backend ket noi Azure SQL.
- Login admin/employee duoc.

### Buoc 7: Deploy Python service

Muc tieu:

- Python co public/private URL de backend goi.
- Secrets doc tu environment variables.

Can test:

- Backend goi Python health.
- Python ingest/chunk/index/profile duoc.

### Buoc 8: Deploy Qdrant

Muc tieu:

- Collection `internal_documents` ton tai.
- Upsert/search vector duoc.

Can test:

- Index document.
- Search query tra chunk lien quan.
- Filter accessLevel dung.

### Buoc 9: Deploy backend + frontend

Muc tieu:

- Truy cap duoc app bang Azure default domain.
- Login/chat/upload/admin dashboard hoat dong.

Can test:

- `/api/health` OK.
- Frontend load OK.
- Cookie login OK.
- Logout OK.
- Protected API OK.

### Buoc 10: End-to-end smoke test

Checklist:

- Login admin.
- Upload PDF text.
- Hangfire auto ingest/chunk/index.
- Hoi RAG co citation.
- Upload PDF scan.
- OCR chay.
- Upload XLSX.
- Auto profile.
- Dataset analyze.
- Chart generate.
- Admin usage xem duoc.
- Audit log khong lo secret.
- Restart backend/Python khong mat data.

## Nhung thu chua nen lam voi vang

- Kubernetes.
- Multi-region deployment.
- Auto-scale phuc tap.
- Full CI/CD pipeline neu chua deploy thu cong on dinh.
- Custom domain rieng.
- Azure Key Vault bat buoc ngay tu dau.
- SignalR real-time job progress neu polling dang du dung.
- Full observability stack nhu Application Insights dashboard phuc tap.

## Rui ro chinh

### 1. Local path khong dung tren cloud

Van de:

- Local code dang dung `storage/uploads`.
- Python dang quen nhan `filePath`.

Huong xu ly:

- Dung Azure Blob + SAS URL.
- Khong dua absolute local path vao Python tren production.

### 2. Cookie auth va CORS

Van de:

- Neu frontend/backend khac domain, cookie co the khong gui.

Huong xu ly:

- Giai doan dau serve frontend chung backend.
- Neu tach domain thi can `SameSite=None`, `Secure`, CORS allow origin cu the, `AllowCredentials`.

### 3. OCR dependencies

Van de:

- Local cai Tesseract rieng.
- Cloud container khong co san Tesseract/ngon ngu.

Huong xu ly:

- Dockerfile cai `tesseract-ocr`, `tesseract-ocr-vie`, `tesseract-ocr-eng`.
- Smoke test OCR trong container.

### 4. Hangfire worker bi sleep

Van de:

- Free tier co the sleep hoac gioi han thoi gian chay.

Huong xu ly:

- Dung plan phu hop cho backend worker.
- Kiem tra job queue sau khi deploy.

### 5. Qdrant persistence

Van de:

- Self-host container neu khong gan volume thi mat vector khi restart.

Huong xu ly:

- Uu tien Qdrant Cloud.
- Neu self-host thi can persistent volume va backup.

### 6. Secret leakage

Van de:

- Deploy de lo API key neu de trong source/log.

Huong xu ly:

- Dung Azure App Settings.
- Khong commit `.env`.
- Audit sanitizer da co baseline.

## Dau hieu hoan thanh

- Co URL Azure truy cap duoc frontend.
- Login admin/employee/guest thanh cong.
- Upload document thanh cong.
- File nam trong Azure Blob.
- Metadata nam trong Azure SQL.
- Hangfire job chay duoc tren cloud.
- Python service parse/OCR/chunk/profile duoc tren cloud.
- Qdrant search duoc.
- RAG answer co citation.
- Dataset analyze va chart chay duoc.
- Restart service khong mat database/file/vector.
- README/deployment docs du de nguoi khac lam lai.

## Checklist sau khi hoan thanh

- Cap nhat URL demo vao README.
- Ghi lai service nao dang dung free/default domain.
- Ghi lai monthly cost uoc tinh.
- Ghi lai cach rotate OpenAI key.
- Ghi lai cach backup Azure SQL.
- Ghi lai cach backup Blob/Qdrant.
- Ghi lai cac known limitations.

