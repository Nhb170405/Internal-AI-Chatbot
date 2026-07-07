namespace backend_dotnet.Contracts.Rag;

public sealed class CitationDto
{
    // Id document goc chua chunk duoc dung lam nguon.
    public Guid DocumentId { get; set; }

    // Id chunk cu the, giup truy vet chinh xac ve doan noi dung.
    public Guid ChunkId { get; set; }

    // Thu tu chunk trong document.
    public int ChunkIndex { get; set; }

    // Do gan semantic do Qdrant tra ve.
    public double Score { get; set; }

    // Doan trich ngan hien thi cho user.
    // Khong nen tra full chunk qua dai o citation.
    public string Snippet { get; set; } = string.Empty;

    // De null trong Milestone 8 neu chunk chua co page metadata.
    // Sau nay co the lay tu DocumentChunk.MetadataJson.
    public int? PageNumber { get; set; }
}
