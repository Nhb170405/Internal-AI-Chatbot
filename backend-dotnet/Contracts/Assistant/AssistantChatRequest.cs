namespace backend_dotnet.Contracts.Assistant;

public sealed class AssistantChatRequest
{
    // Noi dung user nhap trong o chat.
    // Vi du: "hello", "Real_Estate.xlsx co nhung cot nao?", "theo tai lieu noi bo..."
    public string Message { get; set; } = string.Empty;

    // So chunk toi da muon lay khi route la RAG.
    // De nho de tranh ton token, mac dinh nen la 3.
    public int TopK { get; set; } = 3;
}
