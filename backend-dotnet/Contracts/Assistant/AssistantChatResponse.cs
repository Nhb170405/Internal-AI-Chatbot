using System.Text.Json;

namespace backend_dotnet.Contracts.Assistant;

public sealed class AssistantChatResponse
{
    // Route backend da chon: chitchat, rag, dataset_profile, ...
    public string Route { get; set; } = AssistantRoute.Unsupported;

    // Cau tra loi de hien thi trong ChatPage.
    public string Answer { get; set; } = string.Empty;

    // Model chi co gia tri khi route co goi OpenAI chat.
    public string? Model { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    public List<AssistantCitationResponse> Citations { get; set; } = [];

    // Data dung cho dataset/profile/chart sau nay.
    // Vi du: danh sach cot, ket qua analyze, chart metadata.
    public JsonElement? Data { get; set; }

    public string? ChartPath { get; set; }

    public List<string> Warnings { get; set; } = [];

    // Neu true, frontend nen huong user sang page khac hoac yeu cau user chon file.
    public bool NeedsUserAction { get; set; }

    public string? SuggestedAction { get; set; }
}
