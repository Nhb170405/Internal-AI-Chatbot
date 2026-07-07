using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorChunkInput
{
    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public JsonElement Metadata { get; set; }
}
