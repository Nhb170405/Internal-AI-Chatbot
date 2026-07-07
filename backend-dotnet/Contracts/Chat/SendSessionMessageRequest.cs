namespace backend_dotnet.Contracts.Chat;

public sealed class SendSessionMessageRequest
{
    // TODO:
    // Message user gui trong mot chat session cu the.
    // Co the reuse ChatMessageRequest, nhung tach DTO giup route session doc ro hon.
    public string Message { get; set; } = string.Empty;
}
