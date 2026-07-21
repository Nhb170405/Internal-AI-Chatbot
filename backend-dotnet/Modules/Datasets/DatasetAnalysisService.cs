using backend_dotnet.Contracts.Datasets;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Modules.Audit;
using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Python;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Infrastructure.Storage;

namespace backend_dotnet.Modules.Datasets;

public sealed class DatasetAnalysisService
{
    private readonly AppDbContext _db;
    private readonly PythonDatasetClient _pythonDatasetClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileStorageService _fileStorageService;

    public DatasetAnalysisService(AppDbContext db, PythonDatasetClient pythonDatasetClient, AuditLogService auditLogService, IHttpContextAccessor httpContextAccessor, IFileStorageService fileStorageService)
    {
        _db = db;
        _pythonDatasetClient = pythonDatasetClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _fileStorageService = fileStorageService;
    }

    public async Task<DatasetAnalysisResponse> AnalyzeAsync(Guid documentId, DatasetAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 11:
        // 1. Validate request khong null.
        // 2. Check auth + permission:
        //    - admin: analyze moi file.
        //    - employee: analyze employee/guest.
        //    - guest: analyze guest.
        // 3. Validate document extension .csv/.xlsx/.xls.
        // 4. Validate operation:
        //    preview, list_columns, count, sum, average, group_by, top_n.
        // 5. Tao PythonDatasetAnalysisRequest tu Document + request.
        // 6. Goi _pythonDatasetClient.AnalyzeAsync.
        // 7. Audit action = dataset_analyze, chi log operation/sheet/columns, khong log result lon.
        // 8. Map Python response sang DatasetAnalysisResponse.
        if (request is null)
        {
            throw new ValidationApiException("invalid_dataset_analysis_request", "Dataset analysis request is required.");
        }

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

        var operation = NormalizeOperation(request.Operation);

        var readReference = await _fileStorageService.GetReadReferenceAsync(document, cancellationToken);

        var pythonDatasetAnalysisRequest = new PythonDatasetAnalysisRequest
        {
            DocumentId = document.Id.ToString(),
            FilePath = readReference.Value,
            FileReferenceType = readReference.ReferenceType,
            FileReferenceValue = readReference.Value,
            FileName = document.OriginalFileName,
            Extension = document.Extension,
            Operation = operation,
            SheetName = request.SheetName,
            ValueColumn = request.ValueColumn,
            GroupByColumn = request.GroupByColumn,
            TopN = request.TopN
        };

        PythonDatasetAnalysisResponse pythonResponse;
        try
        {
            pythonResponse = await _pythonDatasetClient.AnalyzeAsync(pythonDatasetAnalysisRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("python_service_error", "Python dataset analysis service error.");
        }

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier),
            ActorGuestSessionId = role == UserRole.Guest ? TryGetGuidClaim(principal, "guest_session_id") : null,
            Action = "dataset_analyze",
            ResourceType = "document",
            ResourceId = document.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                operation,
                sheetName = request.SheetName,
                valueColumn = request.ValueColumn,
                groupByColumn = request.GroupByColumn,
                topN = request.TopN,
                success = pythonResponse.Success
            }),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);


        return new DatasetAnalysisResponse
        {
            DocumentId = document.Id,
            Success = pythonResponse.Success,
            Operation = pythonResponse.Operation,
            Result = pythonResponse.Result,
            RowCount = pythonResponse.RowCount,
            Warnings = pythonResponse.Warnings,
            ErrorMessage = pythonResponse.ErrorMessage
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

    private static string NormalizeOperation(string? operation)
    {
        var normalized = operation?.Trim().ToLowerInvariant();

        var isSupported =
            normalized == "preview" ||
            normalized == "list_columns" ||
            normalized == "count" ||
            normalized == "sum" ||
            normalized == "average" ||
            normalized == "group_by" ||
            normalized == "top_n";

        if (!isSupported)
        {
            throw new ValidationApiException("invalid_dataset_analysis_request", "Operation is not supported.");
        }

        return normalized!;
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
