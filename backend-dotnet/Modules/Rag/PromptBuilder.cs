using System.Text;
using backend_dotnet.Contracts.Documents;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.OpenAI;
using backend_dotnet.Modules.Chat;
using Microsoft.OpenApi.Expressions;

namespace backend_dotnet.Modules.Rag;

public sealed class PromptBuilder
{
    public RagPrompt Build(string question, IReadOnlyList<DocumentSearchResultItem> searchResults)
    {
        // Bai tap Milestone 8:
        // 1. Validate question khong rong.
        // 2. Validate searchResults khong null.
        // 3. Tao system message co rule RAG:
        //    - Chi tra loi dua tren CONTEXT.
        //    - Neu context khong du, noi khong tim thay thong tin trong tai lieu noi bo.
        //    - Khong tu bia chinh sach/noi dung.
        //    - Khi tra loi, co the nhac nguon theo [chunkIndex] hoac de citations cho response.
        // 4. Tao context text tu searchResults:
        //    - Moi chunk nen co nhan dang: documentId, chunkId, chunkIndex, score.
        //    - Cat content qua dai neu can de tranh ton token.
        // 5. Tao user message gom:
        //    - CONTEXT
        //    - QUESTION
        // 6. Return RagPrompt co 2 messages: system + user.
        //
        // Goi y cau truc prompt:
        //
        // System:
        // You are an internal company assistant...
        //
        // User:
        // CONTEXT:
        // [Chunk 1]
        // DocumentId: ...
        // ChunkId: ...
        // Content: ...
        //
        // QUESTION:
        // ...
        //
        // RESPONSE RULES:
        // - Answer in Vietnamese.
        // - If context is insufficient, say you cannot find the answer.

        var cleanedQuestion = question.Trim();

        if (string.IsNullOrWhiteSpace(cleanedQuestion))
        {
            throw new ValidationApiException("invalid_rag_request", "Question is required.");
        }

        if (searchResults == null)
        {
            throw new ValidationApiException("invalid_rag_context", "Search results are required.");
        }

        var systemPrompt = """
        You are an internal company AI assistant.
        Answer only based on the provided CONTEXT.
        If the CONTEXT does not contain enough information, say that you cannot find the answer in internal documents.
        Do not invent policies, numbers, or facts.
        Answer in Vietnamese.
        """;

        var contextBlock = BuildContextBlock(searchResults);

        var userPrompt = $"""
        CONTEXT:
        {contextBlock}

        QUESTION:
        {cleanedQuestion}

        RESPONSE RULES:
        - Answer in Vietnamese.
        - Use only the CONTEXT above.
        - If the answer is not in the CONTEXT, say: "Tôi không tìm thấy thông tin này trong tài liệu nội bộ."
        - Do not mention documents that are not in the CONTEXT.
        """;

        return new RagPrompt
        {
            Messages =
       [
           new OpenAIChatMessage
            {
                Role = "system",
                Content = systemPrompt
            },
            new OpenAIChatMessage
            {
                Role = "user",
                Content = userPrompt
            }
       ]
        };
    }

    private static string BuildContextBlock(IReadOnlyList<DocumentSearchResultItem> searchResults)
    {
        // Bai tap:
        // 1. Dung StringBuilder de ghep nhieu chunk thanh context.
        // 2. Voi moi chunk, ghi:
        //    - Source number.
        //    - DocumentId.
        //    - ChunkId.
        //    - ChunkIndex.
        //    - Score.
        //    - Content.
        // 3. Khong dua qua nhieu metadata vao prompt neu khong can.
        // 4. Return context string.
        var builder = new StringBuilder();

        // Ban se tu viet vong lap for o day.
        for (var i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            var sourceNumber = i + 1;
            var content = TrimChunkContent(result.Content, maxLength: 1500);

            builder.AppendLine($"[Source {sourceNumber}]");
            builder.AppendLine($"DocumentId: {result.DocumentId}");
            builder.AppendLine($"ChunkId: {result.ChunkId}");
            builder.AppendLine($"ChunkIndex: {result.ChunkIndex}");
            builder.AppendLine($"Score: {result.Score}");
            builder.AppendLine("Content:");
            builder.AppendLine(content);
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private static string TrimChunkContent(string content, int maxLength)
    {
        // Bai tap:
        // 1. Neu content rong thi return string.Empty.
        // 2. Trim content.
        // 3. Neu do dai <= maxLength thi return content.
        // 4. Neu dai hon thi cat maxLength va them "...".
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var cleaned = content.Trim();

        if (cleaned.Length <= maxLength)
        {
            return cleaned;
        }

        return cleaned[..maxLength] + "...  ";
    }
}
