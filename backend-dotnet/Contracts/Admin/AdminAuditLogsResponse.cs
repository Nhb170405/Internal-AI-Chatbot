namespace backend_dotnet.Contracts.Admin;

public sealed class AdminAuditLogsResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<AdminAuditLogItemResponse> Items { get; set; } = [];
}
