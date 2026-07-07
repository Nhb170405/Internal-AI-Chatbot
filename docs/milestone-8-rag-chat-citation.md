# Milestone 8: Chat RAG With Citation

Trang thai: Completed

Ngay cap nhat: 2026-06-27

Muc tieu:
- Khi user hoi, he thong tim chunks lien quan trong Qdrant.
- Dua context vao OpenAI prompt.
- Tra loi dua tren tai lieu noi bo.
- Tra citation/source kem cau tra loi.

Ly do lam milestone nay:
- Day la gia tri chinh cua internal AI chatbot.
- Chatbot khong chi tra loi chung chung, ma dua tren knowledge noi bo.
- Citation giup user kiem chung cau tra loi.

Kien thuc can hoc:
- RAG pipeline.
- Prompt engineering cho context.
- TopK retrieval.
- Context window.
- Citation mapping.
- Hallucination control.
- "I do not know" behavior.

Pham vi nen lam:
- RagService.
- PromptBuilder.
- Search internal documents.
- Build context tu top chunks.
- Chat answer kem citation.
- Neu khong co context lien quan thi noi khong biet.

Pham vi chua nen lam:
- Tool calling phuc tap.
- Multi-step agent.
- Advanced re-ranking.
- Permission nang cao neu Milestone 14 chua lam.
- Streaming.

Module du kien:
- backend-dotnet/Modules/Rag/RagService.cs
- backend-dotnet/Modules/Rag/PromptBuilder.cs
- backend-dotnet/Modules/Rag/RagChatController.cs
- backend-dotnet/Contracts/Rag/RagChatRequest.cs
- backend-dotnet/Contracts/Rag/RagChatResponse.cs
- backend-dotnet/Contracts/Rag/CitationDto.cs
- backend-dotnet/Infrastructure/Qdrant/QdrantSearchClient.cs neu .NET search

Module da tao thuc te:
- `backend-dotnet/Contracts/Rag/RagChatRequest.cs`
- `backend-dotnet/Contracts/Rag/RagChatResponse.cs`
- `backend-dotnet/Contracts/Rag/CitationDto.cs`
- `backend-dotnet/Modules/Rag/RagPrompt.cs`
- `backend-dotnet/Modules/Rag/PromptBuilder.cs`
- `backend-dotnet/Modules/Rag/RagService.cs`
- `backend-dotnet/Modules/Rag/RagController.cs`

Dang ky DI trong `Program.cs`:
- `PromptBuilder`
- `RagService`

Flow RAG chat:
Frontend/Swagger
 -> POST /api/rag/chat hoac /api/chat/sessions/{id}/rag-messages
 -> auth/owner check
 -> embedding query
 -> Qdrant search topK
 -> load chunk metadata/source
 -> PromptBuilder build system + context + question
 -> OpenAIClient.SendChatAsync
 -> parse answer
 -> map citations tu chunks da dung
 -> save chat message/token usage neu gan voi session
 -> return answer + citations

Flow thuc te da implement:
Frontend/Swagger
 -> POST /api/rag/chat
 -> RagController.Chat
 -> RagService.SendAsync
 -> check authenticated
 -> validate question/topK
 -> DocumentIndexingService.SearchAsync
 -> C# build allowedAccessLevels theo role
 -> Python embed query va search Qdrant
 -> PromptBuilder.Build tao system + user messages
 -> OpenAIClient.SendChatAsync
 -> OpenAI tra answer + token usage
 -> RagService.BuildCitations tu search results
 -> audit `rag_chat_message`
 -> return RagChatResponse

Prompt rule du kien:
- Chi tra loi dua tren context duoc cung cap.
- Neu context khong du, noi khong tim thay thong tin trong tai lieu noi bo.
- Khong tu bia chinh sach noi bo.
- Tra citation bang document/page/chunk.

Prompt template thuc te:
- System message:
  - assistant la internal company AI assistant.
  - chi tra loi dua tren CONTEXT.
  - neu context khong du, noi khong tim thay thong tin trong tai lieu noi bo.
  - khong tu bia policy/number/fact.
  - tra loi bang tieng Viet.
- User message:
  - CONTEXT gom cac chunks da retrieve.
  - Moi chunk co:
    - Source number.
    - DocumentId.
    - ChunkId.
    - ChunkIndex.
    - Score.
    - Content.
  - QUESTION la cau hoi user.
  - RESPONSE RULES nhac lai viec chi dung context.

Response du kien:
- answer
- citations:
  - documentId
  - documentTitle
  - pageNumber nullable
  - chunkId
  - snippet
  - score
- token usage

Response thuc te:
- `answer`
- `citations[]`
  - `documentId`
  - `chunkId`
  - `chunkIndex`
  - `score`
  - `snippet`
  - `pageNumber`
- `model`
- `promptTokens`
- `completionTokens`
- `totalTokens`

Quyet dinh token usage:
- Milestone 8 chi can luu/count token, chua can quy doi thanh tien.
- OpenAI Chat Completions API tra `usage` gom:
  - `prompt_tokens`
  - `completion_tokens`
  - `total_tokens`
- Trong RAG, `prompt_tokens` se gom:
  - system prompt
  - RAG instruction
  - retrieved context chunks
  - user question
  - chat history neu co
- `completion_tokens` la token cua cau tra loi AI.
- `total_tokens = prompt_tokens + completion_tokens`.
- He thong se dua cac field nay vao response va audit/log usage.
- Cost bang tien de Milestone 16 lam neu can, vi pricing thay doi theo model va thoi gian.

Token usage response du kien:
- model
- promptTokens
- completionTokens
- totalTokens

Quyet dinh citation:
- Citation duoc tao tu search hits cua Milestone 7.
- Moi search hit dai dien cho mot chunk, khong phai ca document.
- Mot cau hoi co the can nhieu citation tu nhieu documents khac nhau.
- Khong ep answer chi co mot document source.
- Citation nen giu du `documentId`, `chunkId`, `chunkIndex`, `score`, `snippet`.
- Neu nhieu chunks cung thuoc mot document, co the gom citation theo document trong UI, nhung backend van nen giu chunk-level citation de truy vet chinh xac.
- Neu chunk khong co `pageNumber` thi de null, khong tu doan page.

Ghi chu citation thuc te:
- Milestone 8 citation la chunk-level citation.
- `snippet` duoc cat ngan tu chunk content.
- Chua co `originalFileName` trong response citation vi DTO search Milestone 7 chua expose field nay.
- Chua co `pageNumber`, tam thoi de null.
- Sau nay co the doc `DocumentChunk.MetadataJson` de lay page/section neu co.

## API thuc te

```http
POST /api/rag/chat
```

Request:

```json
{
  "question": "Noi dung tai lieu noi gi?",
  "topK": 5
}
```

Response:

```json
{
  "answer": "...",
  "citations": [
    {
      "documentId": "...",
      "chunkId": "...",
      "chunkIndex": 0,
      "score": 0.8,
      "snippet": "...",
      "pageNumber": null
    }
  ],
  "model": "...",
  "promptTokens": 0,
  "completionTokens": 0,
  "totalTokens": 0
}
```

## Ham chinh

### `PromptBuilder.Build`

Nhiem vu:
- Nhan `question` va `searchResults`.
- Tao context block tu cac chunks.
- Tao messages gui OpenAI Chat API.

Flow:
- trim va validate question.
- validate searchResults.
- build system prompt.
- build context block.
- build user prompt gom CONTEXT + QUESTION + RESPONSE RULES.
- return `RagPrompt`.

### `PromptBuilder.BuildContextBlock`

Nhiem vu:
- Bien danh sach chunks thanh text context.

Luu y:
- Moi chunk co source number.
- Content duoc cat ngan de han che token.
- Khong dua metadata qua nhieu vao prompt.

### `RagService.SendAsync`

Nhiem vu:
- Dieu phoi toan bo RAG chat.

Flow:
- lay principal.
- check authenticated.
- validate request/question/topK.
- goi `DocumentIndexingService.SearchAsync`.
- neu khong co result thi return safe answer, khong goi OpenAI.
- build prompt.
- goi OpenAI Chat API.
- build citations.
- audit token usage va citation count.
- return answer + citations + token usage.

### `RagService.BuildCitations`

Nhiem vu:
- Map `DocumentSearchResultItem` thanh `CitationDto`.

Luu y:
- Citation giu chunk-level trace:
  - documentId
  - chunkId
  - chunkIndex
  - score
  - snippet

### `RagController.Chat`

Nhiem vu:
- Nhan HTTP request.
- Goi `RagService.SendAsync`.
- Map loi:
  - `ArgumentException` -> 400.
  - `UnauthorizedAccessException` -> 401.
  - `InvalidOperationException` -> 502.

Cach test:
- Hoi cau co trong tai lieu, answer dung va co citation.
- Hoi cau khong co trong tai lieu, bot noi khong biet.
- Citation document/page/chunk dung.
- User khong co quyen voi document thi chunk khong duoc dua vao context.
- Token usage duoc luu.

Test da pass:
- Cau co trong tai lieu:
  - API tra answer.
  - Co citations.
  - Co token usage.
- Cau khong co trong tai lieu:
  - Bot khong bia qua tu tin.
- `topK = 0`:
  - Backend dung default.
- `topK` qua lon:
  - Tra 400.
- Question rong:
  - Tra 400.
- Anonymous:
  - Tra 401.
- Permission:
  - Khong leak chunks user khong duoc phep doc.

## Gioi han da phat hien

### 1. Answer thieu noi dung neu retrieved chunks thieu context

Khi test voi cau hoi yeu cau lay ca phan "tom tat ly thuyet", retrieval co the lay dung chunk dau nhung khong lay du cac chunk tiep theo cua section.

Nguyen nhan:
- Milestone 8 hien dung topK semantic search.
- Qdrant tra chunks gan nghia nhat, khong tu dong lay toan bo section.
- Chunking Milestone 6 chua co section boundary metadata.

Ket luan:
- RAG answer chi tot bang context duoc dua vao prompt.
- Neu context thieu, OpenAI se tra loi thieu.
- Day la gioi han retrieval, khong phai bug cua OpenAIClient.

Huong cai tien sau:
- Tang topK cho cau hoi tong hop.
- Neighbor chunk expansion:
  - neu lay chunk 0 thi lay them chunk 1, 2 cung document.
- Them section metadata:
  - `lessonTitle`
  - `sectionTitle`
  - `pageNumber`
- Section-aware retrieval:
  - neu query co "tom tat ly thuyet", lay toan bo chunks trong section do.
- Reranking:
  - lay top 15 tu Qdrant, chon lai top 5 tot nhat.

### 2. Citation chua day du ten file/page

Milestone 8 citation hien co documentId/chunkId/chunkIndex/score/snippet.

Chua co:
- originalFileName.
- pageNumber.
- sectionTitle.

Ly do:
- Milestone 7 search DTO chua expose day du payload.
- DocumentChunk metadata chua chuan hoa page/section.

Huong cai tien:
- Mo rong `DocumentSearchResultItem`.
- Mo rong Qdrant payload.
- Lay page/section tu `DocumentChunk.MetadataJson`.

Dau hieu hoan thanh:
- Chatbot tra loi dua tren tai lieu.
- Co nguon tham khao.
- Khong co context thi khong bia qua tu tin.
- Retrieval + generation flow chay on dinh.

Trang thai thuc te:
- Da hoan thanh muc tieu co ban cua Milestone 8.
- RAG chat da chay end-to-end.
- Citation da co o muc chunk-level.
- Token usage da tra ve trong response.
- Permission khong leak tai lieu trai quyen.
- Da san sang qua Milestone 9 OCR hoac cai tien retrieval sau nay.

## Viec de danh cho milestone sau

- Milestone 9:
  - OCR cho PDF scan/anh.
- Milestone 13:
  - Admin xem usage/citation/document status.
- Milestone 14:
  - Permission theo department/user.
- Milestone 15:
  - Background RAG indexing.
- Milestone 16:
  - Rate limit RAG chat.
  - Token quota.
  - Prompt injection guard.
- Retrieval improvement backlog:
  - Neighbor chunk expansion.
  - Section metadata.
  - Reranking.
