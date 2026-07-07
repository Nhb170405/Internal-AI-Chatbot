# Milestone 3: Chat History And Token Usage Persistence

Trang thai: Done

Muc tieu:
- Tao chat session.
- Luu user message va assistant message.
- Luu token usage cho tung lan goi OpenAI.
- List chat sessions cua current user/guest.
- Xem messages trong mot session.
- Dam bao user/guest chi xem duoc session cua minh.

Quyet dinh da chon:
- Chat history luu trong SQL Server, khong luu trong AuditLogs.
- AuditLogs chi luu metadata ve hanh dong chat.
- Moi ChatSession thuoc ve mot owner:
  - UserId neu employee/admin.
  - GuestSessionId neu guest.
- Neu session khong ton tai hoac khong thuoc owner hien tai thi tra 404, khong tra 403, de tranh lo thong tin.
- Khi gui message trong session, luu user message truoc roi lay recent messages de prompt co cau hoi moi nhat.

Pham vi da lam:
- Entities: ChatSession, ChatMessage, TokenUsage.
- DbSet va mapping trong AppDbContext.
- Migration AddChatHistory.
- DTO cho create/list/detail/message.
- ChatHistoryService.
- ChatSessionsController.
- ChatService.SendSessionMessageAsync.
- Token usage persistence.
- Ownership check cho guest/user.

Pham vi chua lam:
- RAG.
- Qdrant.
- OCR.
- File upload.
- Tool calling.
- Streaming.
- Rate limit.
- Advanced memory summarization.
- Pagination chat history.
- Transaction around user message + OpenAI + assistant message.

File/module chinh:
- backend-dotnet/Modules/Chat/ChatSession.cs
- backend-dotnet/Modules/Chat/ChatMessage.cs
- backend-dotnet/Modules/Chat/TokenUsage.cs
- backend-dotnet/Modules/Chat/ChatMessageRole.cs
- backend-dotnet/Modules/Chat/ChatHistoryService.cs
- backend-dotnet/Modules/Chat/ChatSessionsController.cs
- backend-dotnet/Modules/Chat/ChatService.cs
- backend-dotnet/Contracts/Chat/CreateChatSessionRequest.cs
- backend-dotnet/Contracts/Chat/ChatSessionResponse.cs
- backend-dotnet/Contracts/Chat/ChatSessionDetailResponse.cs
- backend-dotnet/Contracts/Chat/ChatMessageItemResponse.cs
- backend-dotnet/Contracts/Chat/SendSessionMessageRequest.cs
- backend-dotnet/Infrastructure/Persistence/AppDbContext.cs

Entities:
- ChatSession
  - Id
  - UserId
  - GuestSessionId
  - Title
  - CreatedAt
  - UpdatedAt
  - Messages
  - TokenUsages

- ChatMessage
  - Id
  - ChatSessionId
  - Role: user/assistant
  - Content
  - CreatedAt

- TokenUsage
  - Id
  - ChatSessionId
  - UserId
  - GuestSessionId
  - Model
  - PromptTokens
  - CompletionTokens
  - TotalTokens
  - CreatedAt

Ham/logic quan trong:
- ChatHistoryService.CreateSessionAsync
  - Lay current principal.
  - Xac dinh owner bang GetOwner.
  - Tao ChatSession voi title, owner, CreatedAt, UpdatedAt.
  - Luu DB va tra ChatSessionResponse.

- ChatHistoryService.ListSessionsAsync
  - Lay owner hien tai.
  - Query ChatSessions theo UserId hoac GuestSessionId.
  - Sort UpdatedAt desc.
  - Map sang ChatSessionResponse.

- ChatHistoryService.GetSessionDetailAsync
  - Goi GetOwnedSessionAsync.
  - Lay messages cua session.
  - Tra ChatSessionDetailResponse.

- ChatHistoryService.GetSessionMessagesAsync
  - Goi GetOwnedSessionAsync de check quyen.
  - Query ChatMessages theo sessionId.
  - Sort CreatedAt asc.
  - Map sang ChatMessageItemResponse.

- ChatHistoryService.GetOwnedSessionAsync
  - Lay owner hien tai tu claims.
  - Tim session co Id match va owner match.
  - Neu khong thay thi throw KeyNotFoundException.
  - Ham nay la cong bao mat quan trong.

- ChatHistoryService.AddMessageAsync
  - Validate role user/assistant.
  - Validate content.
  - Tao ChatMessage.
  - Luu DB.

- ChatHistoryService.AddTokenUsageAsync
  - Copy ChatSessionId va owner tu ChatSession.
  - Copy model/token tu ChatMessageResponse.
  - Luu TokenUsage.

- ChatHistoryService.UpdateSessionTimestampAsync
  - Set UpdatedAt = DateTimeOffset.UtcNow.
  - SaveChangesAsync.

- ChatHistoryService.GetRecentMessagesAsync
  - Lay N message moi nhat bang OrderByDescending + Take.
  - Sort lai CreatedAt asc truoc khi dua vao prompt.

- ChatService.SendSessionMessageAsync
  - Check auth.
  - Validate message.
  - GetOwnedSessionAsync.
  - AddMessageAsync role user.
  - GetRecentMessagesAsync.
  - Build OpenAI messages: system prompt + recent messages.
  - Goi OpenAIClient.SendChatAsync.
  - Map sang ChatMessageResponse.
  - AddMessageAsync role assistant.
  - AddTokenUsageAsync.
  - UpdateSessionTimestampAsync.
  - AuditLogService.LogAsync.

Flow tao session:
Swagger/Frontend
 -> POST /api/chat/sessions
 -> ChatSessionsController.CreateSession
 -> ChatHistoryService.CreateSessionAsync
 -> AppDbContext.ChatSessions.Add
 -> SaveChangesAsync
 -> return ChatSessionResponse

Flow list session:
Swagger/Frontend
 -> GET /api/chat/sessions
 -> ChatSessionsController.ListSessions
 -> ChatHistoryService.ListSessionsAsync
 -> query theo owner hien tai
 -> return list

Flow gui message trong session:
Swagger/Frontend
 -> POST /api/chat/sessions/{sessionId}/messages
 -> ChatSessionsController.SendMessage
 -> ChatService.SendSessionMessageAsync
 -> ChatHistoryService.GetOwnedSessionAsync
 -> ChatHistoryService.AddMessageAsync(user)
 -> ChatHistoryService.GetRecentMessagesAsync
 -> OpenAIClient.SendChatAsync
 -> ChatHistoryService.AddMessageAsync(assistant)
 -> ChatHistoryService.AddTokenUsageAsync
 -> ChatHistoryService.UpdateSessionTimestampAsync
 -> AuditLogService.LogAsync
 -> return ChatMessageResponse

API endpoints:
- POST /api/chat/sessions
- GET /api/chat/sessions
- GET /api/chat/sessions/{sessionId}
- GET /api/chat/sessions/{sessionId}/messages
- POST /api/chat/sessions/{sessionId}/messages

Cach test:
- Anonymous goi chat session endpoints bi 401.
- Guest login, tao session thanh cong.
- List sessions thay session vua tao.
- Get detail session chua co message tra messages rong.
- Send message vao session thanh cong.
- Get messages thay user va assistant messages.
- SQL Server co ChatMessages va TokenUsages.
- Message rong tra 400.
- SessionId khong ton tai tra 404.
- Guest B khong doc duoc session cua Guest A, tra 404.
- Logout xong protected endpoints tra 401.

Dau hieu hoan thanh:
- Co lich su chat theo user/guest.
- Co thong ke token usage co ban.
- Co phan quyen history.
- Khong doc/gui message vao session cua owner khac.
- Build pass va Swagger tests pass.

Ghi chu can nho:
- Server response trong Swagger la du lieu that; Responses/Example Value chi la schema mau.
- Khong dung GUID mau 3fa85f64-5717-4562-b3fc-2c963f66afa6 de test neu no khong phai id server tra ve.
- GetSessionMessagesAsync khong duoc bo GetOwnedSessionAsync vi do la check quyen.
- Co query du trong GetSessionDetailAsync vi GetSessionMessagesAsync check ownership lai; chap nhan duoc o milestone nay.

Can update sau nay neu thay doi:
- Neu them pagination cho messages.
- Neu them summary memory.
- Neu them transaction/failed assistant response status.
- Neu them cost estimate bang USD/VND.
- Neu them delete/rename session.
