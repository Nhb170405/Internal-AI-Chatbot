using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Documents;
using backend_dotnet.Contracts.Python;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Infrastructure.Storage;

namespace backend_dotnet.Modules.Documents;

public sealed class DocumentIngestionService
{
    private readonly AppDbContext _db;
    private readonly PythonIngestionClient _pythonIngestionClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileStorageService _fileStorageService;

    public DocumentIngestionService(
        AppDbContext db,
        PythonIngestionClient pythonIngestionClient,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        IFileStorageService fileStorageService)
    {
        _db = db;
        _pythonIngestionClient = pythonIngestionClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _fileStorageService = fileStorageService;
    }

    public async Task<DocumentIngestResponse> IngestAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // API wrapper:
        // 1. Check current user tu HttpContext.
        // 2. Check role/permission.
        // 3. Ghi audit theo user request.
        // 4. Goi IngestSystemAsync de xu ly ingest that.
        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var role = GetRole(principal);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);

        if (role != UserRole.Admin && role != UserRole.Employee)
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }

        if (userId == null)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var query = _db.Documents
            .Where(document => document.Id == documentId)
            .Where(document => document.Status != DocumentStatus.Deleted);

        if (role == UserRole.Employee)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Employee || document.AccessLevel == DocumentAccessLevel.Guest);
        }

        var document = await query.FirstOrDefaultAsync(cancellationToken);
        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = documentId.ToString(),
            Action = "document_ingest_requested",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                extension = document.Extension,
                contentType = document.ContentType,
                accessLevel = document.AccessLevel
            })
        }, cancellationToken);

        var response = await IngestSystemAsync(documentId, cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = documentId.ToString(),
            Action = response.Success ? "document_ingest_completed" : "document_ingest_failed",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                extension = document.Extension,
                parserName = response.ParserName,
                characterCount = response.CharacterCount,
                pageCount = response.PageCount,
                status = response.Status,
                errorMessage = response.ErrorMessage
            })
        }, cancellationToken);

        return response;
    }

    public async Task<DocumentIngestResponse> IngestSystemAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 15:
        // Ham nay danh cho background job/internal pipeline, khong danh cho controller goi truc tiep.
        //
        // Khac voi IngestAsync:
        // - Khong doc HttpContext.
        // - Khong doc cookie/claims/current user.
        // - Khong check role tai day, vi quyen da duoc check luc enqueue job.
        //
        // Logic can tach/di chuyen tu IngestAsync sang day:
        // 1. Query document theo documentId va Status != Deleted.
        // 2. Neu khong thay thi throw KeyNotFoundException.
        // 3. Set document.Status = Processing, clear ErrorMessage.
        // 4. Tao PythonIngestRequest tu document.
        // 5. Goi _pythonIngestionClient.IngestAsync.
        // 6. Neu Python loi/success=false:
        //    - set document.Status = Failed.
        //    - set ErrorMessage an toan.
        //    - return DocumentIngestResponse Success=false hoac throw tuy thiet ke handler.
        // 7. Neu success:
        //    - upsert DocumentExtraction theo DocumentId.
        //    - set document.Status = Extracted.
        //    - clear ErrorMessage.
        //    - return DocumentIngestResponse Success=true.
        //
        // Luu y:
        // - Khong log full extracted text.
        // - Co the bo qua AuditLog user-level tai day, vi background job se co job logs rieng.
        // - Sau khi ham nay chay on, IngestAsync co the refactor de check quyen xong goi lai ham nay.
        var document = await _db.Documents
            .Where(document => document.Id == documentId)
            .Where(document => document.Status != DocumentStatus.Deleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var readReference = await _fileStorageService.GetReadReferenceAsync(document, cancellationToken);

        var pythonRequest = new PythonIngestRequest
        {
            DocumentId = document.Id,

            // Legacy field de Python cu van doc duoc trong local mode.
            FilePath = readReference.Value,

            FileReferenceType = readReference.ReferenceType,
            FileReferenceValue = readReference.Value,

            FileName = document.OriginalFileName,
            ContentType = document.ContentType,
            Extension = document.Extension
        };

        PythonIngestResponse pythonResponse;

        try
        {
            pythonResponse = await _pythonIngestionClient.IngestAsync(pythonRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "Python ingestion service error.";
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new DocumentIngestResponse
            {
                DocumentId = document.Id,
                Status = document.Status,
                Success = false,
                ParserName = string.Empty,
                CharacterCount = 0,
                PageCount = null,
                ErrorMessage = document.ErrorMessage
            };
        }

        if (!pythonResponse.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(pythonResponse.ErrorMessage)
                ? "Document ingestion failed."
                : pythonResponse.ErrorMessage;

            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = errorMessage;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new DocumentIngestResponse
            {
                DocumentId = document.Id,
                Status = document.Status,
                Success = false,
                ParserName = pythonResponse.ParserName,
                CharacterCount = 0,
                PageCount = pythonResponse.PageCount,
                ErrorMessage = errorMessage
            };
        }

        var now = DateTimeOffset.UtcNow;

        var extraction = await _db.DocumentExtractions
            .FirstOrDefaultAsync(extraction => extraction.DocumentId == document.Id, cancellationToken);

        if (extraction is null)
        {
            extraction = new DocumentExtraction
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                CreatedAt = now
            };

            _db.DocumentExtractions.Add(extraction);
        }

        extraction.ExtractedText = pythonResponse.ExtractedText;
        extraction.ParserName = pythonResponse.ParserName;
        extraction.CharacterCount = pythonResponse.CharacterCount;
        extraction.PageCount = pythonResponse.PageCount;
        extraction.MetadataJson = JsonSerializer.Serialize(pythonResponse.Metadata);
        extraction.UpdatedAt = now;

        document.Status = DocumentStatus.Extracted;
        document.ErrorMessage = null;
        document.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return new DocumentIngestResponse
        {
            DocumentId = document.Id,
            Status = document.Status,
            Success = true,
            ParserName = pythonResponse.ParserName,
            CharacterCount = pythonResponse.CharacterCount,
            PageCount = pythonResponse.PageCount,
            ErrorMessage = null
        };
    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (role == UserRole.Admin)
        {
            return UserRole.Admin;
        }

        if (role == UserRole.Employee)
        {
            return UserRole.Employee;
        }

        if (role == UserRole.Guest)
        {
            return UserRole.Guest;
        }

        return "anonymous";
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
