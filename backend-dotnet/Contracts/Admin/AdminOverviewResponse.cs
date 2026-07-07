namespace backend_dotnet.Contracts.Admin;

public sealed class AdminOverviewResponse
{
    public int TotalDocuments { get; set; }

    public int IndexedDocuments { get; set; }

    public int FailedDocuments { get; set; }

    public int DeletedDocuments { get; set; }

    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int TotalAuditLogs { get; set; }

    public int AuditLogsLast24Hours { get; set; }

    public int TotalChatSessions { get; set; }

    public int TotalPromptTokens { get; set; }

    public int TotalCompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public List<AdminDocumentStatusCountResponse> DocumentStatusCounts { get; set; } = [];

    public List<AdminRecentAuditLogResponse> RecentAuditLogs { get; set; } = [];
}

public sealed class AdminDocumentStatusCountResponse
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class AdminRecentAuditLogResponse
{
    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
