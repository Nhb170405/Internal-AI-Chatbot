using backend_dotnet.Contracts.Datasets;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Modules.Audit;
using System.Security.Claims;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Contracts.Python;
using backend_dotnet.Infrastructure.Errors;
using System.Text.Json;

namespace backend_dotnet.Modules.Datasets;

public sealed class DatasetProfileService
{
    private readonly AppDbContext _db;
    private readonly PythonDatasetClient _pythonDatasetClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatasetProfileService(
        AppDbContext db,
        PythonDatasetClient pythonDatasetClient,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _pythonDatasetClient = pythonDatasetClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DatasetProfileResponse> ProfileAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // API wrapper: check user/permission, audit request, roi goi ProfileSystemAsync.
        var principal = GetCurrentPrincipal();
        var role = GetRole(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        if (role != UserRole.Admin && role != UserRole.Employee && role != UserRole.Guest)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var query = _db.Documents.Where(document => document.Status != DocumentStatus.Deleted).AsQueryable();
        if (role == UserRole.Admin) { }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else if (role == UserRole.Guest)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }

        var document = await query.FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        IsDatasetExtension(document.Extension);

        var response = await ProfileSystemAsync(documentId, cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier),
            ActorGuestSessionId = role == UserRole.Guest ? TryGetGuidClaim(principal, "guest_session_id") : null,
            Action = response.Success ? "dataset_profile" : "dataset_profile_failed",
            ResourceType = "document",
            ResourceId = document.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                extension = document.Extension,
                tableCount = response.TableCount,
                warningCount = response.Warnings.Count,
                error = response.ErrorMessage
            }),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        return response;
    }

    public async Task<List<DatasetTableProfileResponse>> ListProfilesAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 11:
        // 1. Check auth + permission doc nhu ProfileAsync.
        // 2. Query DocumentTableProfiles theo DocumentId.
        // 3. Order by SheetName, TableIndex.
        // 4. Deserialize ColumnsJson, SampleRowsJson, WarningsJson.
        // 5. Map sang DatasetTableProfileResponse.
        var principal = GetCurrentPrincipal();
        var role = GetRole(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        if (role != UserRole.Admin && role != UserRole.Employee && role != UserRole.Guest)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var query = _db.Documents.Where(document => document.Status != DocumentStatus.Deleted).AsQueryable();
        if (role == UserRole.Admin) { }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else if (role == UserRole.Guest)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }

        var document = await query.FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        var profiles = await _db.DocumentTableProfiles
            .Where(profile => profile.DocumentId == document.Id)
            .OrderBy(profile => profile.SheetName)
            .ThenBy(profile => profile.TableIndex)
            .ToListAsync(cancellationToken);

        var mappedProfiles = profiles.Select(profile => new DatasetTableProfileResponse
        {
            Id = profile.Id,
            DocumentId = profile.DocumentId,
            SheetName = profile.SheetName,
            TableIndex = profile.TableIndex,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            Columns = JsonSerializer.Deserialize<List<DatasetColumnProfileResponse>>(profile.ColumnsJson) ?? [],
            SampleRows = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(profile.SampleRowsJson) ?? [],
            Warnings = JsonSerializer.Deserialize<List<string>>(profile.WarningsJson) ?? [],
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        }).ToList();

        return mappedProfiles;
    }

    public async Task<DatasetProfileResponse> ProfileSystemAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 15:
        // Ham nay danh cho background job/internal pipeline.
        //
        // Khac voi ProfileAsync:
        // - Khong doc HttpContext.
        // - Khong check role/current user.
        // - Chi profile file da duoc background job xu ly hop le.
        //
        // Logic can tach/di chuyen tu ProfileAsync sang day:
        // 1. Query document theo documentId va Status != Deleted.
        // 2. Validate extension la .csv/.xlsx/.xls.
        // 3. Tao PythonDatasetProfileRequest.
        // 4. Goi _pythonDatasetClient.ProfileAsync.
        // 5. Neu fail thi return DatasetProfileResponse Success=false.
        // 6. Neu success:
        //    - xoa DocumentTableProfiles cu theo DocumentId.
        //    - luu profile moi.
        //    - cap nhat DocumentMetadata.DetectedColumnsJson va SheetNamesJson.
        //    - return DatasetProfileResponse Success=true.
        //
        // Luu y:
        // - Profile la optional step trong pipeline, chi chay voi file bang.
        // - Profile retry phai replace profile cu, khong append duplicate.
        var document = await _db.Documents
            .Where(document => document.Id == documentId)
            .Where(document => document.Status != DocumentStatus.Deleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        IsDatasetExtension(document.Extension);

        var pythonRequest = new PythonDatasetProfileRequest
        {
            DocumentId = document.Id.ToString(),
            FilePath = document.StoragePath,
            FileName = document.OriginalFileName,
            Extension = document.Extension
        };

        PythonDatasetProfileResponse pythonResponse;
        try
        {
            pythonResponse = await _pythonDatasetClient.ProfileAsync(pythonRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("python_service_error", "Python dataset profile service error.");
        }

        if (!pythonResponse.Success)
        {
            return new DatasetProfileResponse
            {
                DocumentId = document.Id,
                Success = false,
                TableCount = 0,
                Profiles = [],
                Warnings = pythonResponse.Warnings,
                ErrorMessage = pythonResponse.ErrorMessage
            };
        }

        var oldProfiles = await _db.DocumentTableProfiles
            .Where(profile => profile.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        _db.DocumentTableProfiles.RemoveRange(oldProfiles);

        var now = DateTimeOffset.UtcNow;

        var savedProfiles = pythonResponse.Profiles.Select(profile => new DocumentTableProfile
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            SheetName = profile.SheetName,
            TableIndex = profile.TableIndex,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            ColumnsJson = JsonSerializer.Serialize(profile.Columns),
            SampleRowsJson = JsonSerializer.Serialize(profile.SampleRows),
            WarningsJson = JsonSerializer.Serialize(profile.Warnings),
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        _db.DocumentTableProfiles.AddRange(savedProfiles);

        var detectedColumns = pythonResponse.Profiles
            .SelectMany(profile => profile.Columns)
            .Select(column => column.Name?.Trim())
            .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(columnName => columnName)
            .ToList();

        var sheetNames = pythonResponse.Profiles
            .Select(profile => profile.SheetName?.Trim())
            .Where(sheetName => !string.IsNullOrWhiteSpace(sheetName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(sheetName => sheetName)
            .ToList();

        var metadata = await _db.DocumentMetadatas
            .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);

        if (metadata is null)
        {
            metadata = new DocumentMetadata
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                KeywordsJson = "[]",
                TagsJson = "[]",
                Language = "unknown",
                CreatedAt = now
            };

            _db.DocumentMetadatas.Add(metadata);
        }

        metadata.DetectedColumnsJson = JsonSerializer.Serialize(detectedColumns);
        metadata.SheetNamesJson = JsonSerializer.Serialize(sheetNames);
        metadata.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        var mappedProfiles = savedProfiles.Select(profile => new DatasetTableProfileResponse
        {
            Id = profile.Id,
            DocumentId = profile.DocumentId,
            SheetName = profile.SheetName,
            TableIndex = profile.TableIndex,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            Columns = JsonSerializer.Deserialize<List<DatasetColumnProfileResponse>>(profile.ColumnsJson) ?? [],
            SampleRows = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(profile.SampleRowsJson) ?? [],
            Warnings = JsonSerializer.Deserialize<List<string>>(profile.WarningsJson) ?? [],
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        }).ToList();

        return new DatasetProfileResponse
        {
            DocumentId = document.Id,
            Success = true,
            TableCount = mappedProfiles.Count,
            Profiles = mappedProfiles,
            Warnings = pythonResponse.Warnings,
            ErrorMessage = null
        };
    }


    ///////////////////////////////////////////////////////////////////////////////////////////
    /// Helper
    ///////////////////////////////////////////////////////////////////////////////////////////


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

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        // Doc claim string va parse sang Guid.
        // Neu claim khong co hoac parse fail thi return null.
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static bool IsDatasetExtension(string? extension)
    {
        // Chi cho phep .csv/.xlsx/.xls.
        var normalized = extension?.Trim().ToLowerInvariant();
        return normalized == ".csv" || normalized == ".xlsx" || normalized == ".xls"
            ? true
            : throw new ValidationApiException("invalid_dataset_file", "Only CSV/XLSX/XLS documents can be profiled.");
    }
}
