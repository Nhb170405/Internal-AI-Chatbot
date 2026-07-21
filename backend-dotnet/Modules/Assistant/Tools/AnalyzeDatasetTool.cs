using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Datasets;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Datasets;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Assistant.Tools;

// Deterministic tool for calculations over an entire CSV/Excel dataset.
// The model selects the operation; DatasetAnalysisService enforces permissions,
// validates arguments, resolves storage, and delegates the calculation to pandas.
public sealed class AnalyzeDatasetTool : IAssistantTool
{
    private static readonly string[] SupportedExtensions = [".csv", ".xlsx", ".xls"];

    private readonly AppDbContext _db;
    private readonly DatasetAnalysisService _datasetAnalysisService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyzeDatasetTool(
        AppDbContext db,
        DatasetAnalysisService datasetAnalysisService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _datasetAnalysisService = datasetAnalysisService;
        _httpContextAccessor = httpContextAccessor;
    }

    public AssistantToolDefinition Definition { get; } = new()
    {
        Name = "analyze_dataset",
        Description =
            "Perform exact deterministic calculations over an entire CSV or Excel dataset. " +
            "Use for preview, list columns, row count, sum, average, group-by totals, and top-N. " +
            "Never calculate these values from search snippets or sample rows.",
        Parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                documentName = new
                {
                    type = "string",
                    description = "Dataset file name, with or without extension. Example: Real_Estate.xlsx."
                },
                operation = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "preview",
                        "list_columns",
                        "count",
                        "sum",
                        "average",
                        "group_by",
                        "top_n"
                    }
                },
                sheetName = new
                {
                    type = new[] { "string", "null" },
                    description = "Excel sheet name, or null to use the first sheet."
                },
                valueColumn = new
                {
                    type = new[] { "string", "null" },
                    description = "Numeric column for sum, average, group_by, or top_n."
                },
                groupByColumn = new
                {
                    type = new[] { "string", "null" },
                    description = "Category column required by group_by."
                },
                topN = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 100,
                    description = "Number of rows for preview or top_n; otherwise null."
                }
            },
            required = new[]
            {
                "documentName",
                "operation",
                "sheetName",
                "valueColumn",
                "groupByColumn",
                "topN"
            },
            additionalProperties = false
        }),
        Strict = true
    };

    public async Task<AssistantToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var arguments = DeserializeArguments(argumentsJson);
        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
        }

        var role = GetRole(principal);
        var matchedDocuments = await FindReadableDocumentsAsync(
            arguments.DocumentName,
            role,
            cancellationToken);

        if (matchedDocuments.Count == 0)
        {
            return AssistantToolExecutionResult.Ok(new
            {
                success = false,
                errorCode = "dataset_document_not_found",
                message = $"No readable dataset matched '{arguments.DocumentName}'."
            });
        }

        if (matchedDocuments.Count > 1)
        {
            return AssistantToolExecutionResult.Ok(new
            {
                success = false,
                errorCode = "ambiguous_dataset_document",
                message = "Multiple readable datasets matched. Ask the user to specify the exact file name.",
                candidates = matchedDocuments.Select(document => new
                {
                    documentId = document.Id,
                    document.OriginalFileName,
                    document.UpdatedAt
                })
            });
        }

        var document = matchedDocuments[0];
        var response = await _datasetAnalysisService.AnalyzeAsync(
            document.Id,
            new DatasetAnalysisRequest
            {
                Operation = arguments.Operation.Trim(),
                SheetName = NormalizeOptional(arguments.SheetName),
                ValueColumn = NormalizeOptional(arguments.ValueColumn),
                GroupByColumn = NormalizeOptional(arguments.GroupByColumn),
                TopN = Math.Clamp(arguments.TopN ?? 10, 1, 100)
            },
            cancellationToken);

        return AssistantToolExecutionResult.Ok(new
        {
            response.Success,
            documentId = document.Id,
            document.OriginalFileName,
            response.Operation,
            response.Result,
            response.RowCount,
            response.Warnings,
            response.ErrorMessage
        });
    }

    private static AnalyzeDatasetArguments DeserializeArguments(string argumentsJson)
    {
        AnalyzeDatasetArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<AnalyzeDatasetArguments>(
                argumentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            throw new ValidationApiException(
                "invalid_tool_arguments",
                "analyze_dataset received invalid JSON arguments.");
        }

        if (string.IsNullOrWhiteSpace(arguments?.DocumentName))
        {
            throw new ValidationApiException(
                "invalid_tool_arguments",
                "analyze_dataset requires a documentName.");
        }

        if (string.IsNullOrWhiteSpace(arguments.Operation))
        {
            throw new ValidationApiException(
                "invalid_tool_arguments",
                "analyze_dataset requires an operation.");
        }

        return arguments;
    }

    private async Task<List<Document>> FindReadableDocumentsAsync(
        string requestedDocumentName,
        string role,
        CancellationToken cancellationToken)
    {
        var cleanedName = Path.GetFileName(requestedDocumentName.Trim());
        var requestedExtension = Path.GetExtension(cleanedName).ToLowerInvariant();
        var includesSupportedExtension = SupportedExtensions.Contains(requestedExtension);

        var candidates = await BuildReadableDocumentQuery(role)
            .OrderByDescending(document => document.UpdatedAt)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(document => includesSupportedExtension
                ? document.OriginalFileName.Equals(cleanedName, StringComparison.OrdinalIgnoreCase)
                : Path.GetFileNameWithoutExtension(document.OriginalFileName)
                    .Equals(cleanedName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private IQueryable<Document> BuildReadableDocumentQuery(string role)
    {
        var query = _db.Documents
            .AsNoTracking()
            .Where(document => document.Status != DocumentStatus.Deleted)
            .Where(document => SupportedExtensions.Contains(document.Extension.ToLower()));

        return role switch
        {
            UserRole.Admin => query,
            UserRole.Employee => query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest),
            UserRole.Guest => query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Guest),
            _ => query.Where(_ => false)
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

        return role switch
        {
            UserRole.Admin => UserRole.Admin,
            UserRole.Employee => UserRole.Employee,
            UserRole.Guest => UserRole.Guest,
            _ => "anonymous"
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class AnalyzeDatasetArguments
    {
        public string DocumentName { get; set; } = string.Empty;

        public string Operation { get; set; } = string.Empty;

        public string? SheetName { get; set; }

        public string? ValueColumn { get; set; }

        public string? GroupByColumn { get; set; }

        public int? TopN { get; set; }
    }
}
