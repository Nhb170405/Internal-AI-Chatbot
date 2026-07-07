# Milestone 2: Basic Chat Without RAG

Trang thai: Done

Muc tieu:
- Tao API chat co ban cho user da dang nhap.
- Backend goi OpenAI API va tra cau tra loi ve client.
- Tra token usage neu OpenAI response co.
- Ghi audit log cho chat request o muc metadata an toan.

Quyet dinh da chon:
- API key khong hard-code trong source.
- Development dung user-secrets de luu OpenAI:ApiKey.
- Chat co ban chua co history DB trong milestone nay.
- Audit log chat chi luu metadata, khong luu full message user.
- Rate limit de milestone sau, khong lam voi Milestone 2.

Pham vi da lam:
- Chat endpoint yeu cau authenticated user.
- OpenAIOptions doc tu config.
- OpenAIClient goi endpoint /chat/completions.
- ChatService validate message va tao prompt co system + user.
- Response tra answer, model, promptTokens, completionTokens, totalTokens.
- Chat audit log luu role, messageLength, model, totalTokens.

Pham vi chua lam:
- RAG.
- Qdrant.
- OCR.
- Upload file.
- Tool calling.
- SQL tool.
- Chat history database.
- Rate limit.
- Streaming response.

File/module chinh:
- backend-dotnet/Modules/Chat/ChatController.cs
- backend-dotnet/Modules/Chat/ChatService.cs
- backend-dotnet/Infrastructure/OpenAI/OpenAIOptions.cs
- backend-dotnet/Infrastructure/OpenAI/OpenAIClient.cs
- backend-dotnet/Infrastructure/OpenAI/OpenAIChatMessage.cs
- backend-dotnet/Infrastructure/OpenAI/OpenAIChatResult.cs
- backend-dotnet/Contracts/Chat/ChatMessageRequest.cs
- backend-dotnet/Contracts/Chat/ChatMessageResponse.cs
- backend-dotnet/Program.cs

Ham/logic quan trong:
- ChatController.SendMessage
  - Nhan POST /api/chat/message.
  - Goi ChatService.SendMessageAsync.
  - Map ArgumentException thanh 400.
  - Map UnauthorizedAccessException thanh 401.
  - Map InvalidOperationException tu provider/config thanh 502.

- ChatService.SendMessageAsync
  - Doc current principal.
  - Check authenticated.
  - Validate request.Message khong rong va khong qua dai.
  - Tao List<OpenAIChatMessage> gom system prompt va user message.
  - Goi OpenAIClient.SendChatAsync.
  - Map OpenAIChatResult sang ChatMessageResponse.
  - Ghi audit log action chat_message.

- OpenAIClient.SendChatAsync
  - Validate BaseUrl, ApiKey, ChatModel.
  - Tao HttpRequestMessage POST den OpenAI.
  - Gan Authorization Bearer API key.
  - Gui JSON body: model + messages.
  - Neu response loi thi throw InvalidOperationException voi body da trim.
  - Parse answer tu choices[0].message.content.
  - Parse usage prompt/completion/total tokens.
  - Return OpenAIChatResult.

Flow chat co ban:
Swagger/Frontend
 -> POST /api/chat/message
 -> ChatController.SendMessage
 -> ChatService.SendMessageAsync
 -> OpenAIClient.SendChatAsync
 -> OpenAI API
 -> OpenAIClient parse answer + token usage
 -> ChatService ghi audit metadata
 -> ChatController return ChatMessageResponse

Config:
- appsettings.Development.json co section OpenAI:
  - BaseUrl
  - ChatModel
  - SystemPrompt
- ApiKey nen luu bang user-secrets hoac environment variable.

API endpoints:
- POST /api/chat/message

Cach test:
- Anonymous goi /api/chat/message phai tra 401.
- Login guest/admin roi chat duoc.
- Body message rong tra 400.
- API key sai/provider loi tra 502.
- Response co answer/model/token usage.
- AuditLogs co record chat_message va khong co full message.

Dau hieu hoan thanh:
- Authenticated user chat duoc voi AI.
- API khong crash khi OpenAI loi.
- API key khong hard-code trong C#.
- Token usage duoc tra ve.
- Chat request duoc audit an toan.

Ghi chu can nho:
- Prompt gui len OpenAI la list messages, khong phai 1 string duy nhat.
- System prompt nen ngan gon, dung vai tro rule chung.
- Output tu tool/RAG sau nay can cat gon de tiet kiem token.
- Khong log API key/request body nhay cam.

Can update sau nay neu thay doi:
- Neu doi sang Responses API hoac tool calling.
- Neu them streaming.
- Neu them model fallback.
- Neu them rate limit/token quota.
