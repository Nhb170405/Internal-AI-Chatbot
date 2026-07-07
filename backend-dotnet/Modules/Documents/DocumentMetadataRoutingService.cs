using System.Security.Claims;
using backend_dotnet.Contracts.Documents;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Documents;

public sealed class DocumentMetadataRoutingService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentMetadataRoutingService(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<MetadataSearchResponse> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 10:
        // 1. Lay current principal va check authenticated.
        // 2. Xac dinh role hien tai.
        // 3. Tao query join Documents voi DocumentMetadata.
        // 4. Luon filter:
        //    - Document.Status != Deleted.
        //    - accessLevel theo role:
        //      admin: admin/employee/guest.
        //      employee: employee/guest.
        //      guest: guest.
        // 5. Neu request.ReportType co gia tri thi filter ReportType.
        // 6. Neu request.ReportMonth/ReportYear co gia tri thi filter thang/nam.
        // 7. Neu request.Department co gia tri thi filter Department.
        // 8. Neu request.Keyword co gia tri thi filter KeywordsJson contains keyword.
        //    Giai doan dau co the dung Contains, sau nay moi toi uu JSON query/full-text.
        // 9. Neu request.Query co gia tri thi search nhe tren Title, Description, OriginalFileName.
        // 10. Limit trong khoang an toan, vi du 1..50.
        // 11. Map sang DocumentMetadataResponse va return.
        request ??= new MetadataSearchRequest();

        var principal = GetCurrentPrincipal();
        var role = GetRole(principal);
        var limit = NormalizeLimit(request.Limit);

        var query = _db.Documents
            .Where(document => document.Status != DocumentStatus.Deleted)
            .GroupJoin(
                _db.DocumentMetadatas,
                document => document.Id,
                metadata => metadata.DocumentId,
                (document, metadataItems) => new
                {
                    Document = document,
                    Metadata = metadataItems.FirstOrDefault()
                });

        query = role switch
        {
            UserRole.Admin => query,
            UserRole.Employee => query.Where(item =>
                item.Document.AccessLevel == DocumentAccessLevel.Employee ||
                item.Document.AccessLevel == DocumentAccessLevel.Guest),
            UserRole.Guest => query.Where(item => item.Document.AccessLevel == DocumentAccessLevel.Guest),
            _ => throw new UnauthorizedApiException("unauthorized", "Unauthorized.")
        };

        var reportType = NormalizeText(request.ReportType);
        if (reportType != null)
        {
            query = query.Where(item => item.Metadata != null && item.Metadata.ReportType == reportType);
        }

        if (request.ReportMonth is < 1 or > 12)
        {
            throw new ValidationApiException("invalid_metadata_search_request", "Report month must be between 1 and 12.");
        }

        if (request.ReportMonth.HasValue)
        {
            query = query.Where(item => item.Metadata != null && item.Metadata.ReportMonth == request.ReportMonth.Value);
        }

        if (request.ReportYear is < 2000 or > 2100)
        {
            throw new ValidationApiException("invalid_metadata_search_request", "Report year is out of supported range.");
        }

        if (request.ReportYear.HasValue)
        {
            query = query.Where(item => item.Metadata != null && item.Metadata.ReportYear == request.ReportYear.Value);
        }

        var department = NormalizeText(request.Department);
        if (department != null)
        {
            query = query.Where(item => item.Metadata != null && item.Metadata.Department == department);
        }

        var keyword = NormalizeText(request.Keyword);
        if (keyword != null)
        {
            query = query.Where(item => item.Metadata != null
                && item.Metadata.KeywordsJson != null
                && item.Metadata.KeywordsJson.Contains(keyword));
        }

        var tag = NormalizeText(request.Tag);
        if (tag != null)
        {
            query = query.Where(item => item.Metadata != null
                && item.Metadata.TagsJson != null
                && item.Metadata.TagsJson.Contains(tag));
        }

        var textQuery = NormalizeText(request.Query);
        if (textQuery != null)
        {
            query = query.Where(item =>
                item.Document.OriginalFileName.Contains(textQuery) ||
                (item.Metadata != null && item.Metadata.Title != null && item.Metadata.Title.Contains(textQuery)) ||
                (item.Metadata != null && item.Metadata.Description != null && item.Metadata.Description.Contains(textQuery)) ||
                (item.Metadata != null && item.Metadata.KeywordsJson != null && item.Metadata.KeywordsJson.Contains(textQuery)) ||
                (item.Metadata != null && item.Metadata.TagsJson != null && item.Metadata.TagsJson.Contains(textQuery)));
        }

        var rows = await query
            .OrderByDescending(item => item.Document.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var results = rows
            .Select(item => MapToResponse(item.Document, item.Metadata))
            .ToList();

        return new MetadataSearchResponse
        {
            Success = true,
            Count = results.Count,
            Results = results
        };
    }

    public async Task<List<Guid>> FindCandidateDocumentIdsAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 10:
        // 1. Goi SearchAsync voi request da normalize.
        // 2. Lay Results.Select(x => x.DocumentId).ToList().
        // 3. Ham nay sau nay RagService co the dung truoc khi goi Qdrant.
        var response = await SearchAsync(request, cancellationToken);
        return response.Results
            .Select(result => result.DocumentId)
            .ToList();
    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
        }

        return principal;
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);
        return role switch
        {
            UserRole.Admin => UserRole.Admin,
            UserRole.Employee => UserRole.Employee,
            UserRole.Guest => UserRole.Guest,
            _ => "anonymous"
        };
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit < 1)
        {
            return 20;
        }

        return Math.Min(limit, 50);
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static DocumentMetadataResponse MapToResponse(Document document, DocumentMetadata? metadata)
    {
        return new DocumentMetadataResponse
        {
            DocumentId = document.Id,
            OriginalFileName = document.OriginalFileName,
            Extension = document.Extension,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            AccessLevel = document.AccessLevel,
            Title = metadata?.Title,
            Description = metadata?.Description,
            ReportType = metadata?.ReportType,
            ReportDate = metadata?.ReportDate,
            ReportMonth = metadata?.ReportMonth,
            ReportYear = metadata?.ReportYear,
            Department = metadata?.Department,
            SourceSystem = metadata?.SourceSystem,
            Language = metadata?.Language,
            Keywords = DeserializeStringList(metadata?.KeywordsJson),
            Tags = DeserializeStringList(metadata?.TagsJson),
            DetectedColumns = DeserializeStringList(metadata?.DetectedColumnsJson),
            SheetNames = DeserializeStringList(metadata?.SheetNamesJson),
            MetadataCreatedAt = metadata?.CreatedAt,
            MetadataUpdatedAt = metadata?.UpdatedAt
        };
    }
}
