using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonChunkResponse
{
    public Guid DocumentId { get; set; }

    public bool Success { get; set; }

    public int ChunkCount { get; set; }

    public List<PythonChunkItem> Chunks { get; set; } = [];

    public JsonElement Metadata { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
