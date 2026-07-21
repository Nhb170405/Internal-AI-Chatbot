using backend_dotnet.Contracts.Rag;
using backend_dotnet.Infrastructure.OpenAI;

namespace backend_dotnet.Modules.Assistant;

// Carries the final model response together with citations produced by tools.
// Citations are application data and must not be discarded after the model
// consumes the tool output.
public sealed class AssistantToolCallingResult
{
    public required OpenAIChatResult ChatResult { get; init; }

    public List<CitationDto> Citations { get; init; } = [];
}
