using backend_dotnet.Contracts.Assistant;

namespace backend_dotnet.Modules.Assistant;

public sealed class AssistantRouteDecision
{
    public string Route { get; set; } = AssistantRoute.Unsupported;

    // Do tu tin cua router. Rule-based ban dau chi can uoc luong don gian.
    public double Confidence { get; set; }

    // Ly do route duoc chon, dung de debug/audit.
    public string Reason { get; set; } = string.Empty;

    // Neu user nhac ten file, sau nay co the luu vao day.
    // Vi du: "Real_Estate.xlsx".
    public string? DocumentHint { get; set; }
}
