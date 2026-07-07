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

public sealed class DocumentIndexingService
{
    private readonly AppDbContext _db;
    private readonly PythonVectorClient _pythonVectorClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentIndexingService(
        AppDbContext db,
        PythonVectorClient pythonVectorClient,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _pythonVectorClient = pythonVectorClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DocumentIndexResponse> IndexAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // API wrapper: check user/permission, audit request, roi goi IndexSystemAsync.
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

        var query = _db.Documents.Where(document => document.Id == documentId).Where(document => document.Status != DocumentStatus.Deleted);

        if (role == UserRole.Employee)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Employee || document.AccessLevel == DocumentAccessLevel.Guest);
        }

        var document = await query.FirstOrDefaultAsync(cancellationToken);
        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        var documentChunks = await _db.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .CountAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = "document_index_requested",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                accessLevel = document.AccessLevel,
                chunkCount = documentChunks
            })
        }, cancellationToken);

        var response = await IndexSystemAsync(documentId, cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = response.Success ? "document_index_completed" : "document_index_failed",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                accessLevel = document.AccessLevel,
                chunkCount = documentChunks,
                indexedCount = response.IndexedCount,
                collectionName = response.CollectionName,
                status = response.Status,
                errorMessage = response.ErrorMessage
            })
        }, cancellationToken);

        return response;
    }

    public async Task<DocumentSearchResponse> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 7:
        // 1. Lay principal va check authenticated.
        // 2. Validate query khong rong.
        // 3. Validate topK hop ly, vi du 1..20.
        // 4. Lay role.
        // 5. Build allowedAccessLevels:
        //    - admin: admin, employee, guest.
        //    - employee: employee, guest.
        //    - guest: guest.
        //    - anonymous: Unauthorized.
        // 6. Tao PythonVectorSearchRequest.
        // 7. Goi _pythonVectorClient.SearchAsync.
        // 8. Neu Python success=false:
        //    - return Success=false voi ErrorMessage.
        // 9. Map hits sang DocumentSearchResultItem.
        // 10. Return DocumentSearchResponse.
        //
        // Luu y:
        // - Permission search nam o allowedAccessLevels va Qdrant payload filter.
        // - Chua goi OpenAI chat o Milestone 7.

        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var cleanedQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(cleanedQuery))
        {
            throw new ValidationApiException("invalid_search_request", "Search query is required.");
        }

        if (topK < 1 || topK > 20)
        {
            throw new ValidationApiException("invalid_search_request", "TopK must be between 1 and 20.");
        }

        var role = GetRole(principal);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
        var guestSessionId = TryGetGuidClaim(principal, "guest_session_id");

        var allowedAccessLevels = role switch
        {
            UserRole.Admin => new List<string>
            {
                DocumentAccessLevel.Admin,
                DocumentAccessLevel.Employee,
                DocumentAccessLevel.Guest
            },
            UserRole.Employee => new List<string>
            {
                DocumentAccessLevel.Employee,
                DocumentAccessLevel.Guest
            },
            UserRole.Guest => new List<string>
            {
                DocumentAccessLevel.Guest
            },
            _ => throw new UnauthorizedApiException("unauthorized", "Unauthorized")
        };

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ActorGuestSessionId = guestSessionId,
            Action = "document_vector_search_started",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                role,
                topK,
                queryLength = cleanedQuery.Length,
                allowedAccessLevels
            })
        }, cancellationToken);

        var pythonRequest = new PythonVectorSearchRequest
        {
            Query = cleanedQuery,
            TopK = topK,
            AllowedAccessLevels = allowedAccessLevels
        };

        PythonVectorSearchResponse pythonResponse;

        try
        {
            pythonResponse = await _pythonVectorClient.SearchAsync(pythonRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                ActorUserId = userId,
                ActorGuestSessionId = guestSessionId,
                Action = "document_vector_search_failed",
                ResourceType = "Document",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    role,
                    topK,
                    queryLength = cleanedQuery.Length,
                    errorType = "python_service_error"
                })
            }, cancellationToken);

            throw new ExternalServiceApiException("python_service_error", "Python vector service error.");
        }

        if (!pythonResponse.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(pythonResponse.ErrorMessage)
                ? "Document search failed."
                : pythonResponse.ErrorMessage;

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                ActorUserId = userId,
                ActorGuestSessionId = guestSessionId,
                Action = "document_vector_search_failed",
                ResourceType = "Document",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    role,
                    topK,
                    queryLength = cleanedQuery.Length,
                    errorMessage
                })
            }, cancellationToken);

            return new DocumentSearchResponse
            {
                Query = cleanedQuery,
                Success = false,
                Count = 0,
                Results = [],
                ErrorMessage = errorMessage
            };
        }

        var results = pythonResponse.Hits.Select(hit => new DocumentSearchResultItem
        {
            Score = hit.Score,
            DocumentId = hit.DocumentId,
            ChunkId = hit.ChunkId,
            ChunkIndex = hit.ChunkIndex,
            Content = hit.Content
        }).ToList();

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ActorGuestSessionId = guestSessionId,
            Action = "document_vector_search_completed",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                role,
                topK,
                queryLength = cleanedQuery.Length,
                resultCount = results.Count
            })
        }, cancellationToken);

        return new DocumentSearchResponse
        {
            Query = cleanedQuery,
            Success = true,
            Count = results.Count,
            Results = results,
            ErrorMessage = null
        };
    }

    public async Task<DocumentIndexResponse> IndexSystemAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 15:
        // Ham nay danh cho background job/internal pipeline.
        //
        // Khac voi IndexAsync:
        // - Khong doc HttpContext.
        // - Khong check role/current user.
        // - Chi index document da nam trong job hop le.
        //
        // Logic can tach/di chuyen tu IndexAsync sang day:
        // 1. Query document theo documentId va Status != Deleted.
        // 2. Validate document.Status la Chunked hoac Indexed.
        // 3. Lay DocumentChunks theo DocumentId, order by ChunkIndex.
        // 4. Neu khong co chunks thi throw/return failed.
        // 5. Set document.Status = Processing, clear ErrorMessage.
        // 6. Tao PythonVectorIndexRequest.
        // 7. Goi _pythonVectorClient.IndexDocumentAsync.
        // 8. Neu fail:
        //    - set document.Status = Failed.
        //    - set ErrorMessage an toan.
        // 9. Neu success:
        //    - set document.Status = Indexed.
        //    - clear ErrorMessage.
        //
        // Luu y:
        // - Qdrant upsert theo chunkId de retry khong tao duplicate point.
        // - Khong log full chunk content.
        var document = await _db.Documents
            .Where(document => document.Id == documentId)
            .Where(document => document.Status != DocumentStatus.Deleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        if (document.Status != DocumentStatus.Chunked && document.Status != DocumentStatus.Indexed)
        {
            throw new ValidationApiException("invalid_document_state", "Document must be chunked before indexing.");
        }

        var documentChunks = await _db.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync(cancellationToken);

        if (documentChunks.Count == 0)
        {
            throw new ValidationApiException("invalid_document_state", "Document chunks are empty.");
        }

        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var pythonRequest = new PythonVectorIndexRequest
        {
            DocumentId = document.Id,
            OriginalFileName = document.OriginalFileName,
            AccessLevel = document.AccessLevel,
            DocumentStatus = document.Status,
            Chunks = documentChunks.Select(chunk => new PythonVectorChunkInput
            {
                ChunkId = chunk.Id,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                Metadata = ParseMetadataJson(chunk.MetadataJson)
            }).ToList()
        };

        PythonVectorIndexResponse pythonResponse;

        try
        {
            pythonResponse = await _pythonVectorClient.IndexDocumentAsync(pythonRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "Python vector service error.";
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            throw new ExternalServiceApiException("python_service_error", "Python vector service error.");
        }

        if (!pythonResponse.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(pythonResponse.ErrorMessage)
                ? "Document indexing failed."
                : pythonResponse.ErrorMessage;

            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = errorMessage;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new DocumentIndexResponse
            {
                DocumentId = document.Id,
                Status = document.Status,
                Success = false,
                IndexedCount = pythonResponse.IndexedCount,
                CollectionName = pythonResponse.CollectionName,
                ErrorMessage = errorMessage
            };
        }

        document.Status = DocumentStatus.Indexed;
        document.ErrorMessage = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new DocumentIndexResponse
        {
            DocumentId = document.Id,
            Status = document.Status,
            Success = true,
            IndexedCount = pythonResponse.IndexedCount,
            CollectionName = pythonResponse.CollectionName,
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

    private static JsonElement ParseMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return JsonSerializer.SerializeToElement(new { });
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(new
            {
                metadataWarning = "invalid_metadata_json"
            });
        }
    }
}
