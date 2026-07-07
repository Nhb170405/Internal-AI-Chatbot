namespace backend_dotnet.Contracts.Admin;

public sealed class AdminAuditLogItemResponse
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? ActorGuestSessionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public string? MetadataJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
