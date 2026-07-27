# Cloud migration change log

Date: 2026-07-27

Target architecture:

- Amazon CloudFront provides the public HTTPS `*.cloudfront.net` URL.
- One Amazon EC2 instance runs the ASP.NET Core and Python containers.
- React is built into the ASP.NET Core image and uses the same origin as the API.
- Amazon RDS for SQL Server Express stores relational and Hangfire data.
- A private Cloudflare R2 bucket stores uploaded documents and generated charts.
- Qdrant Cloud remains the vector database.

## Source changes

### Removed Azure runtime dependencies

- Removed the `Azure.Storage.Blobs` NuGet package.
- Removed `AzureBlobFileStorageService` and `AzureBlobStorageOptions`.
- Removed the Azure Blob provider from runtime dependency injection.
- Removed the Azure Static Web Apps GitHub Actions workflow.
- Removed Azure production origins and production settings.
- Kept `docs/deployment-azure.md` only as historical documentation.

### Added Cloudflare R2 storage

- Added `AWSSDK.S3`, which is compatible with the R2 S3 API.
- Added `R2StorageOptions`.
- Added `R2ObjectStorageService` for put, metadata, delete, and presigned GET operations.
- Added `R2FileStorageService` as the production `IFileStorageService`.
- Added the `r2` storage provider.
- Added the generic `presigned_url` file-reference type.
- Python accepts the provider-neutral `presigned_url` reference.
- R2 buckets stay private; short-lived URLs are created only when Python or an authenticated chart request needs a file.

### Removed shared chart volumes in production

- Python now renders a PNG in memory and returns base64 to ASP.NET Core.
- ASP.NET Core validates the base64 payload and limits it to 10 MiB.
- Production charts are uploaded under `charts/chart_<id>.png` in R2.
- The authenticated chart endpoint redirects to a short-lived R2 URL.
- Local development still writes charts to the configured local chart directory.

### Protected ASP.NET Core to Python calls

- Added `PythonService:ApiKey`.
- Added `PythonServiceAuthHandler`; every typed Python HTTP client sends `X-Internal-Api-Key`.
- Added Python middleware that uses a timing-safe comparison.
- `/health` remains unauthenticated for Docker health checks.
- All other Python endpoints return `401` without the internal key.
- The production Docker Compose file does not publish Python port 8000.

### Served React and ASP.NET Core on one origin

- Added a root multi-stage `Dockerfile`.
- The Dockerfile builds React, publishes ASP.NET Core, and copies `dist` into `wwwroot`.
- ASP.NET Core now serves default/static files and falls back to `index.html` for React routes.
- Production frontend API calls use relative `/api` URLs.
- Production cookies use `SameSite=Lax` and `Secure`.
- Production no longer requires a CORS origin; localhost origins remain for Vite development.

### Added domainless AWS deployment files

- Added `docker-compose.production.yml`.
- Added `.env.production.example`.
- Added `.dockerignore`.
- Added configurable forwarded-protocol header support for CloudFront.
- Added the detailed operator guide in `docs/deployment-aws-domainless.md`.
- Added the Vietnamese operator guide in
  `docs/huong-dan-trien-khai-aws-khong-domain.md`.

## Important behavior changes

- Production must set `FileStorage__Provider=r2`.
- `R2Storage` credentials are required when document or chart storage is first used.
- Production must set the same random secret in `PythonService__ApiKey` and `PYTHON_SERVICE_API_KEY`.
- The public URL is the CloudFront URL, not the EC2 hostname.
- Do not expose Python port 8000 or RDS port 1433 to the Internet.
