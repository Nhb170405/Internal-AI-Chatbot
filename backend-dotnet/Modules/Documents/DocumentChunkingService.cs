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

namespace backend_dotnet.Modules.Documents;

public sealed class DocumentChunkingService
{
    private readonly AppDbContext _db;
    private readonly PythonChunkingClient _pythonChunkingClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentChunkingService(
        AppDbContext db,
        PythonChunkingClient pythonChunkingClient,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _pythonChunkingClient = pythonChunkingClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DocumentChunkingResponse> ChunkAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // API wrapper: check user/permission, audit request, roi goi ChunkSystemAsync.

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

        var queryDocument = _db.Documents.Where(document => document.Id == documentId).Where(document => document.Status != DocumentStatus.Deleted);

        if (role == UserRole.Employee)
        {
            queryDocument = queryDocument.Where(document => document.AccessLevel == DocumentAccessLevel.Employee || document.AccessLevel == DocumentAccessLevel.Guest);
        }

        var document = await queryDocument.FirstOrDefaultAsync(cancellationToken);
        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = "document_chunk_requested",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                accessLevel = document.AccessLevel
            })
        }, cancellationToken);

        var response = await ChunkSystemAsync(documentId, cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = response.Success ? "document_chunk_completed" : "document_chunk_failed",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                accessLevel = document.AccessLevel,
                chunkCount = response.ChunkCount,
                status = response.Status,
                errorMessage = response.ErrorMessage
            })
        }, cancellationToken);

        return response;
    }

    public async Task<List<DocumentChunkResponse>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 6:
        // 1. Check authenticated.
        // 2. Check role:
        //    - admin xem chunks moi document.
        //    - employee xem chunks document accessLevel employee/guest.
        //    - guest co the xem chunks guest-level neu muon debug, nhung ban dau nen chi admin/employee.
        // 3. Query document theo permission giong ChunkAsync.
        // 4. Neu khong thay document thi throw KeyNotFoundException.
        // 5. Query _db.DocumentChunks theo DocumentId.
        // 6. OrderBy ChunkIndex.
        // 7. Select sang DocumentChunkResponse.
        // 8. Return list.

        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var role = GetRole(principal);
        var queryDocument = _db.Documents.Where(document => document.Id == documentId).Where(document => document.Status != DocumentStatus.Deleted);

        if (role == UserRole.Admin)
        {
        }
        else if (role == UserRole.Employee)
        {
            queryDocument = queryDocument.Where(document => document.AccessLevel == DocumentAccessLevel.Employee || document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else if (role == UserRole.Guest)
        {
            queryDocument = queryDocument.Where(document => document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");

        }

        var document = await queryDocument.FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        return await _db.DocumentChunks.Where(chunk => chunk.DocumentId == document.Id).OrderBy(chunk => chunk.ChunkIndex).Select(chunk => new DocumentChunkResponse
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content,
            CharacterCount = chunk.CharacterCount,
            StartOffset = chunk.StartOffset,
            EndOffset = chunk.EndOffset,
            CreatedAt = chunk.CreatedAt
        }).ToListAsync(cancellationToken);
    }

    public async Task<DocumentChunkingResponse> ChunkSystemAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 15:
        // Ham nay danh cho background job/internal pipeline.
        //
        // Khac voi ChunkAsync:
        // - Khong doc HttpContext.
        // - Khong check role/current user.
        // - Chi xu ly chunk document da duoc phep xu ly tu luc enqueue job.
        //
        // Logic can tach/di chuyen tu ChunkAsync sang day:
        // 1. Query document theo documentId va Status != Deleted.
        // 2. Validate document.Status la Extracted hoac Chunked.
        // 3. Lay DocumentExtraction.
        // 4. Neu extracted text rong thi throw/return failed.
        // 5. Set document.Status = Processing, clear ErrorMessage.
        // 6. Tao PythonChunkRequest.
        // 7. Goi _pythonChunkingClient.ChunkAsync.
        // 8. Neu fail:
        //    - set document.Status = Failed.
        //    - set ErrorMessage an toan.
        // 9. Neu success:
        //    - xoa chunks cu theo DocumentId.
        //    - add chunks moi.
        //    - set document.Status = Chunked.
        //
        // Luu y idempotency:
        // - Re-chunk phai replace chunks cu, khong append duplicate.
        var document = await _db.Documents
            .Where(document => document.Id == documentId)
            .Where(document => document.Status != DocumentStatus.Deleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        if (document.Status != DocumentStatus.Extracted && document.Status != DocumentStatus.Chunked)
        {
            throw new ValidationApiException("invalid_document_state", "Document must be extracted before chunking.");
        }

        var documentExtraction = await _db.DocumentExtractions
            .FirstOrDefaultAsync(extraction => extraction.DocumentId == document.Id, cancellationToken);

        if (documentExtraction == null || string.IsNullOrWhiteSpace(documentExtraction.ExtractedText))
        {
            throw new ValidationApiException("invalid_document_state", "Document extraction is empty.");
        }

        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var pythonChunkRequest = new PythonChunkRequest
        {
            DocumentId = document.Id,
            Text = documentExtraction.ExtractedText,
            ChunkSize = 1200,
            ChunkOverlap = 150
        };

        PythonChunkResponse pythonChunkResponse;

        try
        {
            pythonChunkResponse = await _pythonChunkingClient.ChunkAsync(pythonChunkRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "Python chunking service error.";
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new DocumentChunkingResponse
            {
                DocumentId = document.Id,
                Status = document.Status,
                Success = false,
                ChunkCount = 0,
                ErrorMessage = document.ErrorMessage
            };
        }

        if (!pythonChunkResponse.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(pythonChunkResponse.ErrorMessage)
                ? "Document chunking failed."
                : pythonChunkResponse.ErrorMessage;

            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = errorMessage;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new DocumentChunkingResponse
            {
                DocumentId = document.Id,
                Status = document.Status,
                Success = false,
                ChunkCount = 0,
                ErrorMessage = errorMessage
            };
        }

        var existingChunks = await _db.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        _db.DocumentChunks.RemoveRange(existingChunks);

        var now = DateTimeOffset.UtcNow;

        foreach (var pythonChunk in pythonChunkResponse.Chunks)
        {
            _db.DocumentChunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = pythonChunk.ChunkIndex,
                Content = pythonChunk.Content,
                CharacterCount = pythonChunk.CharacterCount,
                StartOffset = pythonChunk.StartOffset,
                EndOffset = pythonChunk.EndOffset,
                MetadataJson = JsonSerializer.Serialize(pythonChunk.Metadata),
                CreatedAt = now
            });
        }

        document.Status = DocumentStatus.Chunked;
        document.ErrorMessage = null;
        document.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return new DocumentChunkingResponse
        {
            DocumentId = document.Id,
            Status = document.Status,
            Success = true,
            ChunkCount = pythonChunkResponse.ChunkCount,
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
