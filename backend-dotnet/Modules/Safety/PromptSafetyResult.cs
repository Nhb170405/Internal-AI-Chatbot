namespace backend_dotnet.Modules.Safety;

public sealed class PromptSafetyResult
{
    public bool IsAllowed { get; set; }

    public string? ReasonCode { get; set; }

    public string? SafeMessage { get; set; }
}
