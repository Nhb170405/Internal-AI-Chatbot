using backend_dotnet.Contracts.Assistant;

namespace backend_dotnet.Modules.Assistant;

public sealed class AssistantRouter
{
    public AssistantRouteDecision Decide(string message)
    {
        var cleanedMessage = message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanedMessage))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.Unsupported,
                Confidence = 1,
                Reason = "empty_message"
            };
        }

        var normalized = cleanedMessage.ToLowerInvariant();
        var documentHint = TryExtractDocumentHint(cleanedMessage);

        if (ContainsAny(normalized, "bieu do", "biểu đồ", "chart", "ve bieu do", "vẽ biểu đồ"))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.Chart,
                Confidence = 0.8,
                Reason = "matched_chart_keyword",
                DocumentHint = documentHint
            };
        }

        if (ContainsAny(normalized, "top", "tong", "tổng", "trung binh", "trung bình", "sum", "average", "doanh thu"))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.DatasetAnalyze,
                Confidence = 0.75,
                Reason = "matched_dataset_analyze_keyword",
                DocumentHint = documentHint
            };
        }

        if (ContainsAny(normalized, "cot nao", "cột nào", "column", "columns", "sheet", "truong du lieu", "trường dữ liệu"))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.DatasetProfile,
                Confidence = 0.85,
                Reason = "matched_dataset_profile_keyword",
                DocumentHint = documentHint
            };
        }

        if (ShouldUseRagForDocumentHint(documentHint, normalized) ||
            ContainsAny(normalized, "tai lieu", "tài liệu", "chinh sach", "chính sách", "quy dinh", "quy định", "huong dan", "hướng dẫn", "noi bo", "nội bộ", "noi dung", "nội dung", "tom tat", "tóm tắt"))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.Rag,
                Confidence = 0.78,
                Reason = documentHint is null ? "matched_rag_keyword" : "matched_document_hint_rag",
                DocumentHint = documentHint
            };
        }

        if (IsChitchat(normalized))
        {
            return new AssistantRouteDecision
            {
                Route = AssistantRoute.Chitchat,
                Confidence = 0.95,
                Reason = "matched_chitchat_keyword",
                DocumentHint = documentHint
            };
        }

        return new AssistantRouteDecision
        {
            Route = AssistantRoute.Chitchat,
            Confidence = 0.5,
            Reason = "fallback_to_chitchat",
            DocumentHint = documentHint
        };
    }

    private static bool IsChitchat(string normalized)
    {
        if (normalized.Length <= 20 && ContainsAny(normalized, "hello", "hi", "hey", "xin chao", "xin chào", "chao", "chào"))
        {
            return true;
        }

        return normalized.Length <= 30 && ContainsAny(normalized, "cam on", "cảm ơn", "thanks", "thank you");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldUseRagForDocumentHint(string? documentHint, string normalized)
    {
        if (string.IsNullOrWhiteSpace(documentHint))
        {
            return false;
        }

        var lowerHint = documentHint.ToLowerInvariant();
        var isTableFile = lowerHint.EndsWith(".xlsx") || lowerHint.EndsWith(".xls") || lowerHint.EndsWith(".csv");

        if (isTableFile)
        {
            return false;
        }

        return ContainsAny(normalized, "file", "noi dung", "nội dung", "tom tat", "tóm tắt", "doc", "đọc", "trong file");
    }

    private static string? TryExtractDocumentHint(string message)
    {
        var supportedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".csv", ".txt" };
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var cleanedPart = part.Trim(',', '.', ':', ';', '"', '\'', '(', ')', '[', ']');

            if (supportedExtensions.Any(extension => cleanedPart.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return cleanedPart;
            }
        }

        return null;
    }
}
