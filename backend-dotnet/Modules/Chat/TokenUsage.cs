namespace backend_dotnet.Modules.Chat;

public sealed class TokenUsage
{
    public Guid Id { get; set; }

    public Guid ChatSessionId { get; set; }

    // TODO:
    // Luu owner de sau nay query report theo user/guest de hon.
    public Guid? UserId { get; set; }

    public Guid? GuestSessionId { get; set; }

    public string Model { get; set; } = string.Empty;

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    // TODO:
    // Milestone sau co the them EstimatedCost.
    public DateTimeOffset CreatedAt { get; set; }

    public ChatSession? ChatSession { get; set; }
}
