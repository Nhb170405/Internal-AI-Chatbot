using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorSearchHit
{
    public double Score { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public JsonElement Payload { get; set; }
}
