namespace backend_dotnet.Contracts.Rag;

public sealed class RagChatRequest
{
    // Cau hoi cua user.
    // Vi du: "Theo tai lieu noi bo, nhan vien duoc nghi phep bao nhieu ngay?"
    public string Question { get; set; } = string.Empty;

    // So chunk lien quan muon lay tu Qdrant.
    // Nen bat dau nho, vi moi chunk dua vao prompt deu ton token.
    public int TopK { get; set; } = 5;
}
