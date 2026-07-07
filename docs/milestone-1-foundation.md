# Milestone 1: Project Foundation

Trang thai: Done

Muc tieu:
- Tao nen mong cho Factory Chatbot V2 bang ASP.NET Core Web API va SQL Server.
- Co authentication bang cookie-session cho guest, employee, admin.
- Co database schema toi thieu cho user, guest session, audit log.
- Co cau truc module ro rang truoc khi them Chat, RAG, Python, Qdrant, OpenAI.

Quyet dinh da chon:
- Dung cookie authentication thay JWT trong giai doan dau.
- Co 3 role chinh: guest, employee, admin.
- Guest khong luu vao bang Users, ma co bang GuestSessions rieng.
- AuditLog chi luu metadata an toan, khong luu password, token, cookie, API key.
- Development co seeder dev-only de tao user admin/employee test.

Pham vi da lam:
- Health check endpoint.
- Auth endpoints: guest login, employee/admin login, logout, me.
- Cookie-session auth va claim-based identity.
- EF Core + SQL Server + migrations.
- AppDbContext mapping cho Users, GuestSessions, AuditLogs.
- AuditLogService co ban.
- DevelopmentDataSeeder cho user test.

Pham vi chua lam:
- Chat voi OpenAI.
- Upload file.
- RAG/Qdrant.
- Python FastAPI integration.
- Admin dashboard.
- Rate limit.
- Production deployment.

File/module chinh:
- backend-dotnet/Program.cs
- backend-dotnet/Infrastructure/Persistence/AppDbContext.cs
- backend-dotnet/Infrastructure/Persistence/Seed/DevelopmentDataSeeder.cs
- backend-dotnet/Modules/Health/HealthController.cs
- backend-dotnet/Modules/Auth/AuthController.cs
- backend-dotnet/Modules/Auth/AuthService.cs
- backend-dotnet/Modules/Users/AppUser.cs
- backend-dotnet/Modules/Users/UserRole.cs
- backend-dotnet/Modules/Users/UserService.cs
- backend-dotnet/Modules/Sessions/GuestSession.cs
- backend-dotnet/Modules/Sessions/SessionService.cs
- backend-dotnet/Modules/Audit/AuditLog.cs
- backend-dotnet/Modules/Audit/AuditLogEntry.cs
- backend-dotnet/Modules/Audit/AuditLogService.cs
- backend-dotnet/Contracts/Auth/*

Ham/logic quan trong:
- AuthService.GuestLoginAsync
  - Tao GuestSession.
  - Tao claims cho guest.
  - SignInAsync bang cookie.
  - Ghi audit log.
  - Tra CurrentUserResponse.

- AuthService.LoginAsync
  - Tim user bang email.
  - Kiem tra user active.
  - Verify password hash bang IPasswordHasher.
  - Tao claims user id/email/name/role.
  - SignInAsync bang cookie.
  - Ghi audit log.

- AuthService.LogoutAsync
  - SignOutAsync.
  - Ghi audit log logout.

- AuthService.GetCurrentUserAsync
  - Doc HttpContext.User.
  - Neu anonymous thi tra role anonymous.
  - Neu guest thi doc guest_session_id.
  - Neu employee/admin thi doc user id.

- SessionService.CreateGuestSessionAsync
  - Tao GuestSession.Id.
  - Tao SessionKey bang random an toan.
  - Set ExpiresAt/CreatedAt/IsActive.
  - Luu DB.

- SessionService.IsGuestSessionActiveAsync
  - Kiem tra guest session con active va chua het han.

- UserService.FindByEmailAsync
  - Query AppUser theo email.

- UserService.GetByIdAsync
  - Query AppUser theo id.

- AuditLogService.LogAsync
  - Nhan AuditLogEntry.
  - Luu action/resource/actor/metadata/ip/time vao DB.

Flow guest login:
Swagger/Frontend
 -> POST /api/auth/guest-login
 -> AuthController.GuestLogin
 -> AuthService.GuestLoginAsync
 -> SessionService.CreateGuestSessionAsync
 -> HttpContext.SignInAsync
 -> AuditLogService.LogAsync
 -> return CurrentUserResponse

Flow employee/admin login:
Swagger/Frontend
 -> POST /api/auth/login
 -> AuthController.Login
 -> AuthService.LoginAsync
 -> UserService.FindByEmailAsync
 -> IPasswordHasher.VerifyHashedPassword
 -> HttpContext.SignInAsync
 -> AuditLogService.LogAsync
 -> return CurrentUserResponse

Flow me:
Swagger/Frontend
 -> GET /api/auth/me
 -> AuthController.Me
 -> AuthService.GetCurrentUserAsync
 -> doc claims tu cookie principal
 -> return CurrentUserResponse

Database tables:
- Users
  - Id, Email, DisplayName, Role, DepartmentId, PasswordHash, IsActive, CreatedAt, UpdatedAt.
- GuestSessions
  - Id, DisplayName, SessionKey, ExpiresAt, CreatedAt, IsActive.
- AuditLogs
  - Id, ActorUserId, ActorGuestSessionId, Action, ResourceType, ResourceId, MetadataJson, IpAddress, CreatedAt.

API endpoints:
- GET /api/health
- POST /api/auth/guest-login
- POST /api/auth/login
- POST /api/auth/logout
- GET /api/auth/me

Cach test:
- GET /api/health tra 200.
- Guest login thanh cong, sau do /api/auth/me tra role guest.
- Employee/admin login thanh cong bang user seeded.
- Logout xong /api/auth/me tra anonymous hoac protected endpoint tra 401.
- User sai password khong login duoc.
- AuditLogs co record login/logout.

Dau hieu hoan thanh:
- Build pass.
- SQL Server tao duoc tables.
- Swagger goi duoc auth endpoints.
- Cookie auth phan biet anonymous va authenticated.
- Role guest/employee/admin nam trong claims.

Ghi chu can nho:
- Cookie auth voi API can override redirect, neu khong ASP.NET co the redirect sang login path va gay 405.
- Khong log du lieu nhay cam trong AuditLog.
- AppDbContext la source mapping giua entity C# va SQL Server.
- Migration thay doi schema, database update ap dung schema vao SQL Server.

Can update sau nay neu thay doi:
- Neu doi sang JWT hoac them Identity chuan.
- Neu them register user public.
- Neu them department/permission vao auth.
- Neu audit log co schema moi.
