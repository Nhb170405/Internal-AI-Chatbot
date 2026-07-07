using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using backend_dotnet.Contracts.Documents;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Modules.Users;
using backend_dotnet.Infrastructure.Errors;

namespace backend_dotnet.Modules.Documents;

public sealed class DocumentMetadataService
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentMetadataService(
        AppDbContext db,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task CreateDefaultMetadataForUploadAsync(Document document, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 10:
        // 1. Ham nay duoc DocumentService.UploadAsync goi sau khi tao Document.
        // 2. Kiem tra document.Id co hop le khong.
        // 3. Neu DocumentMetadata cua document da ton tai thi return, khong tao trung.
        // 4. Tao Title mac dinh tu document.OriginalFileName:
        //    - dung Path.GetFileNameWithoutExtension.
        //    - trim khoang trang.
        // 5. Thu parse ReportMonth/ReportYear tu ten file neu co mau don gian.
        //    Giai doan dau co the tach helper ParseMonthYearFromFileName.
        // 6. KeywordsJson va TagsJson nen luu "[]" thay vi null de de doc.
        // 7. CreatedAt va UpdatedAt = DateTimeOffset.UtcNow.
        // 8. SaveChangesAsync.
        // 9. Ghi audit action = "document_metadata_create_default".

        if (document.Id == Guid.Empty)
        {
            throw new ValidationApiException("invalid_document", "Missing document ID.");
        }

        var metadataExists = await _db.DocumentMetadatas
            .AnyAsync(metadata => metadata.DocumentId == document.Id, cancellationToken);

        if (metadataExists)
        {
            return;
        }

        var title = Path.GetFileNameWithoutExtension(document.OriginalFileName).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.IsNullOrWhiteSpace(document.OriginalFileName)
                ? document.Id.ToString()
                : document.OriginalFileName.Trim();
        }

        var (parsedMonth, parsedYear) = TryParseMonthYearFromFileName(title);
        var now = DateTimeOffset.UtcNow;

        var documentMetadata = new DocumentMetadata
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Title = title,
            ReportMonth = parsedMonth,
            ReportYear = parsedYear,
            Language = "unknown",
            KeywordsJson = "[]",
            TagsJson = "[]",
            CreatedAt = now,
            UpdatedAt = now,
            ReportType = null,
            Department = null,
            SourceSystem = null
        };

        _db.DocumentMetadatas.Add(documentMetadata);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = document.UploadedByUserId ?? TryGetGuidClaim(ClaimTypes.NameIdentifier),
            Action = "document_metadata_create_default",
            ResourceType = "DocumentMetadata",
            ResourceId = document.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                title,
                reportMonth = parsedMonth,
                reportYear = parsedYear,
                source = "upload_default"
            }),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);


    }

    public async Task<DocumentMetadataResponse> GetAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 10:
        // 1. Lay current user tu _httpContextAccessor.HttpContext.User.
        // 2. Check authenticated, neu chua login thi throw UnauthorizedAccessException.
        // 3. Query Document theo documentId va Status != Deleted.
        // 4. Check quyen doc theo accessLevel:
        //    - admin doc moi document.
        //    - employee doc employee/guest.
        //    - guest doc guest.
        // 5. Query DocumentMetadata theo DocumentId.
        // 6. Neu metadata chua co, co the tao response voi metadata null/empty.
        // 7. Map Document + DocumentMetadata sang DocumentMetadataResponse.

        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var role = GetRole(principal);

        var document = await _db.Documents
            .FirstOrDefaultAsync(
                item => item.Id == documentId && item.Status != DocumentStatus.Deleted,
                cancellationToken);

        if (document == null || !CanReadDocument(role, document))
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        var metadata = await _db.DocumentMetadatas
            .FirstOrDefaultAsync(
                item => item.DocumentId == documentId,
                cancellationToken);

        return MapToResponse(document, metadata);
    }

    public async Task<DocumentMetadataResponse> UpdateAsync(Guid documentId, UpdateDocumentMetadataRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 10:
        // 1. Lay current user va check authenticated.
        // 2. Query Document theo documentId va Status != Deleted.
        // 3. Check quyen sua metadata:
        //    - admin sua moi document.
        //    - employee chi sua document accessLevel employee/guest neu phu hop rule hien tai.
        //    - guest khong duoc sua.
        // 4. Validate request:
        //    - ReportMonth neu co phai 1..12.
        //    - ReportYear neu co nen trong khoang hop ly, vi du 2000..2100.
        //    - Title/ReportType/Department trim va gioi han do dai.
        //    - Keywords/Tags trim, bo item rong, distinct, gioi han so luong.
        // 5. Neu metadata chua co thi tao moi, neu co thi update.
        // 6. Serialize Keywords va Tags thanh JSON array string.
        // 7. UpdatedAt = DateTimeOffset.UtcNow.
        // 8. SaveChangesAsync.
        // 9. Ghi audit action = "document_metadata_update", khong log noi dung qua dai.
        // 10. Return response sau update.
        if (request == null)
        {
            throw new ValidationApiException("invalid_metadata", "Metadata request is required.");
        }

        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var role = GetRole(principal);

        var document = await _db.Documents
            .FirstOrDefaultAsync(
                item => item.Id == documentId && item.Status != DocumentStatus.Deleted,
                cancellationToken);

        if (document == null || !CanUpdateDocument(role, document))
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        if (request.ReportMonth is < 1 or > 12)
        {
            throw new ValidationApiException("invalid_metadata", "Report month must be between 1 and 12.");
        }

        if (request.ReportYear is < 2000 or > 2100)
        {
            throw new ValidationApiException("invalid_metadata", "Report year is out of supported range.");
        }

        var metadata = await _db.DocumentMetadatas
            .FirstOrDefaultAsync(
                item => item.DocumentId == documentId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (metadata == null)
        {
            metadata = new DocumentMetadata
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                KeywordsJson = "[]",
                TagsJson = "[]",
                CreatedAt = now
            };

            _db.DocumentMetadatas.Add(metadata);
        }

        metadata.Title = NormalizeNullableText(request.Title, 500);
        metadata.Description = NormalizeNullableText(request.Description, 2000);
        metadata.ReportType = NormalizeNullableText(request.ReportType, 100);
        metadata.ReportDate = request.ReportDate;
        metadata.ReportMonth = request.ReportMonth ?? request.ReportDate?.Month;
        metadata.ReportYear = request.ReportYear ?? request.ReportDate?.Year;
        metadata.Department = NormalizeNullableText(request.Department, 100);
        metadata.SourceSystem = NormalizeNullableText(request.SourceSystem, 100);
        metadata.Language = NormalizeNullableText(request.Language, 20);

        if (request.Keywords != null)
        {
            metadata.KeywordsJson = JsonSerializer.Serialize(NormalizeStringList(request.Keywords, 30, 100));
        }

        if (request.Tags != null)
        {
            metadata.TagsJson = JsonSerializer.Serialize(NormalizeStringList(request.Tags, 30, 100));
        }

        metadata.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = TryGetGuidClaim(ClaimTypes.NameIdentifier),
            Action = "document_metadata_update",
            ResourceType = "DocumentMetadata",
            ResourceId = documentId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                metadata.Title,
                metadata.ReportType,
                metadata.ReportMonth,
                metadata.ReportYear,
                metadata.Department,
                keywordCount = DeserializeStringList(metadata.KeywordsJson).Count,
                tagCount = DeserializeStringList(metadata.TagsJson).Count
            }),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        return MapToResponse(document, metadata);
    }



    ///////////////////////////////////////////
    ///   Helper
    //////////////////////////////////////////

    private static (int? month, int? year) TryParseMonthYearFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return (null, null);
        }

        // Bat cac mau pho bien:
        // - 2026-04, 2026_04, 2026.04
        // - 04-2026, 04_2026, 04.2026
        // - thang-4-2026, thang 04 2026
        var normalized = fileName.Trim().ToLowerInvariant();

        var yearMonthMatch = Regex.Match(
            normalized,
            @"(?<!\d)(20\d{2})[-_.\s](0?[1-9]|1[0-2])(?!\d)");

        if (yearMonthMatch.Success)
        {
            var year = int.Parse(yearMonthMatch.Groups[1].Value);
            var month = int.Parse(yearMonthMatch.Groups[2].Value);
            return (month, year);
        }

        var monthYearMatch = Regex.Match(
            normalized,
            @"(?<!\d)(0?[1-9]|1[0-2])[-_.\s](20\d{2})(?!\d)");

        if (monthYearMatch.Success)
        {
            var month = int.Parse(monthYearMatch.Groups[1].Value);
            var year = int.Parse(monthYearMatch.Groups[2].Value);
            return (month, year);
        }

        var vietnameseMonthMatch = Regex.Match(
            normalized,
            @"thang[-_\s]*(0?[1-9]|1[0-2])[-_\s]*(20\d{2})");

        if (vietnameseMonthMatch.Success)
        {
            var month = int.Parse(vietnameseMonthMatch.Groups[1].Value);
            var year = int.Parse(vietnameseMonthMatch.Groups[2].Value);
            return (month, year);
        }

        return (null, null);
    }

    private Guid? TryGetGuidClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        // Lay HttpContext.User.
        // Neu HttpContext null thi day la loi pipeline/DI, throw InvalidOperationException.
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        // Doc ClaimTypes.Role.
        // Neu khong co role thi return anonymous.
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

    private static bool CanReadDocument(string role, Document document)
    {
        return role switch
        {
            UserRole.Admin => true,
            UserRole.Employee => document.AccessLevel is DocumentAccessLevel.Employee or DocumentAccessLevel.Guest,
            UserRole.Guest => document.AccessLevel == DocumentAccessLevel.Guest,
            _ => false
        };
    }

    private static bool CanUpdateDocument(string role, Document document)
    {
        return role switch
        {
            UserRole.Admin => true,
            UserRole.Employee => document.AccessLevel is DocumentAccessLevel.Employee or DocumentAccessLevel.Guest,
            _ => false
        };
    }

    private static string? NormalizeNullableText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ValidationApiException("invalid_metadata", $"Text value must be at most {maxLength} characters.");
        }

        return normalized;
    }

    private static List<string> NormalizeStringList(List<string> values, int maxItems, int maxItemLength)
    {
        return values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value =>
            {
                if (value!.Length > maxItemLength)
                {
                    throw new ValidationApiException("invalid_metadata", $"List item must be at most {maxItemLength} characters.");
                }

                return value;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToList();
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
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
