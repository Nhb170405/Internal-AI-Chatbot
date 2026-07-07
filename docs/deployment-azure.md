# Azure Deployment Guide

Trang thai: Draft

Ngay cap nhat: 2026-07-07

Tai lieu nay la checklist deploy Factory Chatbot V2 len Azure. Muc tieu la co mot ban deploy that su dung duoc, co the debug, co the nang cap, va khong dua secrets vao source code.

## 1. Kien truc deploy du kien

Giai doan dau nen deploy theo huong:

```text
Browser
  |
  | HTTPS
  v
ASP.NET Core Backend
  |-- Serve React static files
  |-- Expose /api/*
  |-- Run Hangfire worker
  |
  |--> Azure SQL Database
  |--> Azure Blob Storage
  |--> Python FastAPI Service
  |--> Qdrant
  |--> OpenAI API
```

Ly do chon cach nay:

- Frontend va backend cung domain nen cookie-session de cau hinh hon.
- Giam loi CORS/cross-site cookie.
- Deploy MVP gon hon so voi tach frontend sang mot domain rieng.

Sau nay neu can scale frontend rieng, co the tach sang Azure Static Web Apps.

## 2. Domain

Khong can mua domain rieng o giai doan dau.

Co the dung domain mac dinh cua Azure:

- Backend App Service: `https://<app-name>.azurewebsites.net`
- Container Apps: `https://<app-name>.<region>.azurecontainerapps.io`
- Static Web Apps neu tach frontend: `https://<app-name>.azurestaticapps.net`

Custom domain dep hon nhung thuong phai mua. Nen lam sau khi he thong chay on dinh.

## 3. Bien moi truong

Khong commit file `.env` that.

Repo chi nen commit:

- Root `.env.example`
- `frontend/.env.example`
- `ai-service-python/.env.example`
- `backend-dotnet/appsettings.Production.json` voi placeholder rong, khong co secrets.

### Backend ASP.NET Core

Cau hinh production nen nam trong Azure App Settings hoac secret store:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<azure-sql-connection-string>
OpenAI__ApiKey=<openai-api-key>
OpenAI__BaseUrl=https://api.openai.com/v1
OpenAI__ChatModel=gpt-4.1-mini
OpenAI__EmbeddingModel=text-embedding-3-small
PythonService__BaseUrl=<python-service-url>
PythonService__TimeoutSeconds=180
Qdrant__Url=<qdrant-url>
Qdrant__ApiKey=<qdrant-api-key-if-any>
Qdrant__Collection=internal_documents
AzureBlobStorage__ConnectionString=<azure-blob-connection-string>
AzureBlobStorage__ContainerName=uploaded-documents
DocumentRetention__DeletedFileRetentionDays=30
```

### Python FastAPI

Python service doc config tu environment variables:

```text
OPENAI_API_KEY=<openai-api-key>
OPENAI_EMBEDDING_MODEL=text-embedding-3-small
QDRANT_URL=<qdrant-url>
QDRANT_API_KEY=<qdrant-api-key-if-any>
QDRANT_COLLECTION=internal_documents
TESSERACT_LANG=vie+eng
TEMP_FILE_ROOT=/tmp/factory-chatbot
CHART_OUTPUT_DIR=generated/charts
```

Luu y: hien tai Python code moi doc truc tiep `OPENAI_API_KEY`, `OPENAI_EMBEDDING_MODEL`, `QDRANT_URL`, `QDRANT_COLLECTION`. Cac bien OCR/temp/chart la baseline cho Docker/production va co the can duoc noi vao code sau.

### Frontend

File hien tai:

```text
VITE_API_BASE_URL=http://localhost:5055
```

Neu frontend chay rieng trong dev, giu URL backend local.

Neu frontend duoc build va serve chung domain voi backend, co the cau hinh:

```text
VITE_API_BASE_URL=
```

Khi do frontend se goi API bang relative path nhu `/api/auth/me`. Cach nay tot cho cookie-session vi frontend/backend cung origin.

## 4. Frontend build chung backend

Huong MVP:

1. Build frontend:

```powershell
cd frontend
npm install
npm run build
```

2. Copy output `frontend/dist` vao `backend-dotnet/wwwroot`.

3. Backend can cau hinh:

- Serve static files.
- Map fallback ve `index.html` cho React Router.
- Giu `/api/*` cho API.

Can kiem tra sau:

- Truy cap root URL thay login page.
- Login thanh cong bang cookie.
- Refresh trang `/chat`, `/documents`, `/datasets`, `/charts` khong bi 404.

## 5. Azure SQL

Can tao:

- Azure SQL Server.
- Azure SQL Database.
- Firewall rule cho may dev neu can chay migration tu local.

Checklist:

1. Lay connection string tu Azure Portal.
2. Dua vao App Settings: `ConnectionStrings__DefaultConnection`.
3. Chay migration:

```powershell
cd backend-dotnet
dotnet ef database update
```

4. Kiem tra cac bang:

- Users / sessions / audit logs.
- Documents / metadata / extractions / chunks.
- Hangfire tables.
- Background job tables.

Luu y production:

- Khong nen seed password mac dinh yeu.
- Nen doi seed dev-only thanh seed co dieu kien ro rang.

## 6. Azure Blob Storage

Hien tai local dang luu file trong `backend-dotnet/storage/uploads`.

Tren Azure khong nen dua vao local disk vi:

- App Service/container co the restart.
- Scale nhieu instance se khong share cung disk.
- Python service khong doc duoc path local cua backend.

Huong production:

```text
Upload file
  -> Backend validate file
  -> Backend upload raw file vao Azure Blob
  -> SQL luu metadata + blob name/container
  -> Background job tao SAS URL ngan han
  -> Python download file tam ve /tmp
  -> Python parse/OCR/chunk/index
```

Can refactor:

- Tao interface storage, vi du `IFileStorageService`.
- Local implementation giu cho dev.
- Azure Blob implementation dung cho production.
- Document nen co thong tin du de truy xuat blob.

## 7. Python FastAPI deploy

Nen deploy Python bang Docker vi OCR can system dependencies.

Docker image can co:

- Python runtime.
- `requirements.txt`.
- Tesseract binary.
- Tesseract language data `vie` va `eng`.

Can test trong container:

```bash
tesseract --version
python -c "import fitz; import pytesseract; from PIL import Image; print('ocr deps ok')"
uvicorn main:app --host 0.0.0.0 --port 8000
```

Health check:

```text
GET /health
```

## 8. Qdrant

Khuyen nghi production:

- Uu tien Qdrant Cloud de giam van hanh.
- Neu tu host, can persistent volume, backup, auth/API key, network security.

Can dam bao:

- Collection name: `internal_documents`.
- Vector size dung voi embedding model.
- Payload co `accessLevel`, `documentId`, `chunkId`, `chunkIndex`, `originalFileName`.
- Search co filter theo role/access level.

## 9. Hangfire

Hien tai Hangfire dung backend va SQL Server.

Can kiem tra production:

- Hangfire tables da tao trong Azure SQL.
- Background worker chay trong backend instance.
- Dashboard `/hangfire` khong public vo dieu kien.
- Job retry khong lam duplicate du lieu qua muc.

Neu scale backend nhieu instance:

- Hangfire co the co nhieu server cung doc queue.
- Can dam bao pipeline idempotent hon, vi du xoa/recreate chunks/vectors truoc khi index lai.

## 10. Security checklist

Truoc khi public:

- Bat HTTPS.
- Cookie `Secure`, `HttpOnly`, `SameSite` phu hop.
- CORS chi cho phep domain frontend neu frontend tach rieng.
- Khong expose Swagger/Hangfire public trong production, hoac bao ve bang admin auth.
- Khong log password, token, API key, cookie.
- Upload validation da bat: extension, size, filename, content type baseline.
- Rate limit da bat cho login/chat/upload/job endpoints.
- Security headers baseline da co.

## 11. Smoke test sau deploy

Test theo thu tu:

1. `GET /api/health` tra 200.
2. Login admin/employee.
3. `GET /api/auth/me` tra dung user.
4. Upload `.txt` nho.
5. Job tu dong chay den indexed.
6. Upload `.xlsx`, job chay den indexed + profiled.
7. Hoi assistant/chat mot cau basic.
8. Hoi RAG cau co trong tai lieu da index.
9. Vao Documents thay status dung.
10. Vao Datasets profile/analyze duoc file bang.
11. Tao chart va xem duoc chart qua API an toan.
12. Xem Admin usage/audit co du lieu.

## 12. Thu tu thuc hien de it loi

1. Chuan hoa config va `.env.example`.
2. Commit Git sach.
3. Tao Azure SQL.
4. Chay migration len Azure SQL.
5. Tao Azure Blob Storage.
6. Refactor file storage sang Local/Azure abstraction.
7. Dockerize Python service.
8. Deploy Python service len Azure.
9. Cau hinh Qdrant production.
10. Deploy backend.
11. Build frontend va serve chung backend.
12. Chay smoke test.
13. Khoa Swagger/Hangfire neu public.
14. Viet README deploy ngan gon.

## 13. Cac viec chua nen lam voi ban deploy dau

- Chua can custom domain.
- Chua can scale nhieu backend instance.
- Chua can Kubernetes.
- Chua can Redis/RabbitMQ neu Hangfire SQL van du.
- Chua can CI/CD phuc tap truoc khi deploy thu cong thanh cong.
- Chua can token budget phuc tap neu dung noi bo va co audit.

## 14. Viec can lam tiep trong repo

- Them Dockerfile cho Python service.
- Them Dockerfile hoac publish script cho backend.
- Them script build frontend vao backend `wwwroot`.
- Refactor local file storage sang Azure Blob Storage.
- Them config production cho cookie/CORS/Swagger/Hangfire dashboard.
- Cap nhat README chay local va deploy.
