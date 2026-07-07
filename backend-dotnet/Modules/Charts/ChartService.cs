using backend_dotnet.Contracts.Charts;
using backend_dotnet.Contracts.Datasets;
using backend_dotnet.Contracts.Python;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Datasets;
using System.Security.Claims;
using System.Text.Json;

namespace backend_dotnet.Modules.Charts;

public sealed class ChartService
{
    private readonly DatasetAnalysisService _datasetAnalysisService;
    private readonly PythonChartClient _pythonChartClient;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ChartFileService _chartFileService;

    public ChartService(
        DatasetAnalysisService datasetAnalysisService,
        PythonChartClient pythonChartClient,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        ChartFileService chartFileService)
    {
        _datasetAnalysisService = datasetAnalysisService;
        _pythonChartClient = pythonChartClient;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _chartFileService = chartFileService;
    }

    public async Task<ChartResponse> CreateChartAsync(Guid documentId, ChartRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 12 - Cach A:
        // 1. Validate request khong null.
        // 2. Normalize/validate ChartType: bar, line, pie.
        // 3. Tao DatasetAnalysisRequest tu ChartRequest:
        //    - Operation
        //    - SheetName
        //    - ValueColumn
        //    - GroupByColumn
        //    - TopN
        // 4. Goi _datasetAnalysisService.AnalyzeAsync(documentId, analysisRequest).
        // 5. Neu analysisResponse.Success == false:
        //    - return ChartResponse success=false, ErrorMessage tu analysis.
        // 6. Validate analysisResponse.Result phu hop de ve chart:
        //    - Nen la JSON array object cho bar/line.
        // 7. Tao PythonChartRenderRequest:
        //    - ChartType
        //    - Title
        //    - Data = analysisResponse.Result
        //    - XField
        //    - YField
        // 8. Goi _pythonChartClient.RenderAsync.
        // 9. Audit action = dataset_chart_create.
        // 10. Map Python response sang ChartResponse.
        //
        // Luu y:
        // - Service nay khong doc file truc tiep.
        // - Service nay khong tu tinh sum/group_by.
        // - Tat ca tinh toan di qua DatasetAnalysisService.
        if (request is null)
        {
            throw new ValidationApiException("invalid_chart_request", "Chart request is required.");
        }

        var chartType = NormalizeChartType(request.ChartType);

        var analysisRequest = new DatasetAnalysisRequest
        {
            Operation = request.Operation,
            SheetName = request.SheetName,
            ValueColumn = request.ValueColumn,
            GroupByColumn = request.GroupByColumn,
            TopN = request.TopN
        };

        var analysisResponse = await _datasetAnalysisService.AnalyzeAsync(documentId, analysisRequest, cancellationToken);

        if (!analysisResponse.Success)
        {
            return new ChartResponse
            {
                DocumentId = documentId,
                Success = false,
                ChartType = chartType,
                ChartPath = null,
                Data = analysisResponse.Result,
                Warnings = analysisResponse.Warnings,
                ErrorMessage = analysisResponse.ErrorMessage
            };
        }

        if (analysisResponse.Result is null || analysisResponse.Result.Value.ValueKind != JsonValueKind.Array)
        {
            return new ChartResponse
            {
                DocumentId = documentId,
                Success = false,
                ChartType = chartType,
                ChartPath = null,
                Data = analysisResponse.Result,
                Warnings = analysisResponse.Warnings,
                ErrorMessage = "Chart data must be an array. Use operations like preview, group_by, or top_n."
            };
        }

        var pythonRequest = new PythonChartRenderRequest
        {
            ChartType = chartType,
            Title = request.Title,
            Data = analysisResponse.Result,
            XField = request.XField,
            YField = request.YField
        };

        PythonChartRenderResponse pythonResponse;
        try
        {
            pythonResponse = await _pythonChartClient.RenderAsync(pythonRequest, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("python_service_error", "Python chart render service error.");
        }

        var principal = _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier),
            ActorGuestSessionId = TryGetGuidClaim(principal, "guest_session_id"),
            Action = "dataset_chart_create",
            ResourceType = "document",
            ResourceId = documentId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                chartType,
                operation = request.Operation,
                valueColumn = request.ValueColumn,
                groupByColumn = request.GroupByColumn,
                topN = request.TopN,
                success = pythonResponse.Success
            }),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        return new ChartResponse
        {
            DocumentId = documentId,
            Success = pythonResponse.Success,
            ChartType = pythonResponse.ChartType,
            ChartPath = pythonResponse.ChartPath,
            ChartUrl = _chartFileService.CreateChartUrl(pythonResponse.ChartPath),
            Data = pythonResponse.Data,
            Warnings = pythonResponse.Warnings,
            ErrorMessage = pythonResponse.ErrorMessage
        };
    }

    private static string NormalizeChartType(string? chartType)
    {
        var normalized = chartType?.Trim().ToLowerInvariant();

        var isSupported =
            normalized == "bar" ||
            normalized == "line" ||
            normalized == "pie";

        if (!isSupported)
        {
            throw new ValidationApiException("invalid_chart_request", "Chart type is not supported.");
        }

        return normalized!;
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
