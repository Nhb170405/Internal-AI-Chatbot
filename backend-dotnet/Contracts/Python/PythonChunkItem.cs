using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonChunkItem
{
    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? StartOffset { get; set; }

    public int? EndOffset { get; set; }

    public JsonElement Metadata { get; set; }
}
