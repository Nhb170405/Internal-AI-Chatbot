using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Assistant;
using backend_dotnet.Contracts.Datasets;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Datasets;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Assistant;

public sealed class AssistantDatasetProfileHandler
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AssistantDatasetProfileHandler(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AssistantChatResponse> HandleAsync(
        string message,
        string? documentHint,
        CancellationToken cancellationToken = default)
    {
        // Handler nay xu ly cau hoi kieu:
        // - "Real_Estate.xlsx co nhung cot nao?"
        // - "file doanh thu co sheet nao?"
        //
        // Giai doan nay chi doc thong tin da co trong SQL:
        // - Documents
        // - DocumentMetadatas
        // - DocumentTableProfiles
        //
        // Khong tu goi Python profile o day, vi profile co the ton thoi gian.

        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
        }

        var role = GetRole(principal);

        if (string.IsNullOrWhiteSpace(documentHint))
        {
            return new AssistantChatResponse
            {
                Route = AssistantRoute.DatasetProfile,
                Answer = "Toi nhan ra ban muon xem cau truc file bang, nhung chua xac dinh duoc ten file. Hay nhap ro ten file hoac mo trang Datasets de chon file.",
                NeedsUserAction = true,
                SuggestedAction = "select_dataset_document"
            };
        }

        var documents = await BuildReadableDocumentQuery(role)
            .Where(document => document.OriginalFileName.Contains(documentHint))
            .OrderByDescending(document => document.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return new AssistantChatResponse
            {
                Route = AssistantRoute.DatasetProfile,
                Answer = $"Toi khong tim thay file ban co quyen doc gan voi ten '{documentHint}'. Hay kiem tra lai ten file hoac quyen truy cap.",
                NeedsUserAction = true,
                SuggestedAction = "check_document_name",
                Data = JsonSerializer.SerializeToElement(new
                {
                    documentHint
                })
            };
        }

        if (documents.Count > 1)
        {
            return BuildMultipleDocumentsResponse(documentHint, documents);
        }

        var document = documents[0];

        var metadata = await _db.DocumentMetadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);

        var profiles = await _db.DocumentTableProfiles
            .AsNoTracking()
            .Where(profile => profile.DocumentId == document.Id)
            .OrderBy(profile => profile.SheetName)
            .ThenBy(profile => profile.TableIndex)
            .ToListAsync(cancellationToken);

        var sheetNames = DeserializeStringList(metadata?.SheetNamesJson);
        var detectedColumns = DeserializeStringList(metadata?.DetectedColumnsJson);
        var profileResponses = profiles.Select(MapProfile).ToList();

        var answer = BuildAnswer(document.OriginalFileName, sheetNames, detectedColumns, profileResponses);

        return new AssistantChatResponse
        {
            Route = AssistantRoute.DatasetProfile,
            Answer = answer,
            NeedsUserAction = profileResponses.Count == 0 && detectedColumns.Count == 0,
            SuggestedAction = profileResponses.Count == 0 && detectedColumns.Count == 0
                ? "run_dataset_profile"
                : null,
            Data = JsonSerializer.SerializeToElement(new
            {
                documentId = document.Id,
                document.OriginalFileName,
                document.AccessLevel,
                document.Status,
                sheetNames,
                detectedColumns,
                profiles = profileResponses
            })
        };
    }

    private IQueryable<Document> BuildReadableDocumentQuery(string role)
    {
        var query = _db.Documents
            .AsNoTracking()
            .Where(document => document.Status != DocumentStatus.Deleted);

        return role switch
        {
            UserRole.Admin => query,
            UserRole.Employee => query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest),
            UserRole.Guest => query.Where(document => document.AccessLevel == DocumentAccessLevel.Guest),
            _ => query.Where(_ => false)
        };
    }

    private static AssistantChatResponse BuildMultipleDocumentsResponse(string documentHint, List<Document> documents)
    {
        return new AssistantChatResponse
        {
            Route = AssistantRoute.DatasetProfile,
            Answer = $"Toi tim thay nhieu file cung hoac gan ten '{documentHint}'. Hay xoa bot file trung hoac doi ten file de he thong xac dinh dung file can hoi.",
            NeedsUserAction = true,
            SuggestedAction = "resolve_duplicate_documents",
            Data = JsonSerializer.SerializeToElement(new
            {
                documentHint,
                candidates = documents.Select(document => new
                {
                    documentId = document.Id,
                    document.OriginalFileName,
                    document.AccessLevel,
                    document.Status,
                    document.UpdatedAt
                })
            })
        };
    }

    private static DatasetTableProfileResponse MapProfile(DocumentTableProfile profile)
    {
        return new DatasetTableProfileResponse
        {
            Id = profile.Id,
            DocumentId = profile.DocumentId,
            SheetName = profile.SheetName,
            TableIndex = profile.TableIndex,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            Columns = DeserializeJson<List<DatasetColumnProfileResponse>>(profile.ColumnsJson) ?? [],
            SampleRows = DeserializeJson<List<Dictionary<string, object?>>>(profile.SampleRowsJson) ?? [],
            Warnings = DeserializeJson<List<string>>(profile.WarningsJson) ?? [],
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static string BuildAnswer(
        string originalFileName,
        List<string> sheetNames,
        List<string> detectedColumns,
        List<DatasetTableProfileResponse> profiles)
    {
        if (profiles.Count > 0)
        {
            var tableSummaries = profiles.Select(profile =>
            {
                var columns = profile.Columns.Count == 0
                    ? "chua co cot nao duoc detect"
                    : string.Join(", ", profile.Columns.Select(column => column.Name));

                return $"- Sheet '{profile.SheetName}', table {profile.TableIndex}: {profile.RowCount} dong, {profile.ColumnCount} cot. Cot: {columns}.";
            });

            return $"File {originalFileName} co {profiles.Count} bang da profile:\n{string.Join("\n", tableSummaries)}";
        }

        if (detectedColumns.Count > 0 || sheetNames.Count > 0)
        {
            var sheetText = sheetNames.Count > 0
                ? $"Sheet: {string.Join(", ", sheetNames)}."
                : "Chua co thong tin sheet.";

            var columnText = detectedColumns.Count > 0
                ? $"Cot detect duoc: {string.Join(", ", detectedColumns)}."
                : "Chua co thong tin cot.";

            return $"File {originalFileName}: {sheetText} {columnText}";
        }

        return $"File {originalFileName} da ton tai, nhung chua co profile cot/sheet trong SQL. Hay chay Dataset Profile truoc.";
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

    private static List<string> DeserializeStringList(string? json)
    {
        return DeserializeJson<List<string>>(json) ?? [];
    }

    private static T? DeserializeJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
