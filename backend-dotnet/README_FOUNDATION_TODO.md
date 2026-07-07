# Backend Foundation

Day la bai tap Milestone 1. Cac file da co san khung class/method, nhung chua co implementation.

Thu tu nen lam:
1. Xoa endpoint mau `weatherforecast` trong `Program.cs`.
2. Them controller support: `builder.Services.AddControllers()` va `app.MapControllers()`.
3. Cai EF Core + SQL Server packages.
4. Viet `AppDbContext`.
5. Tao migration dau tien.
6. Cau hinh cookie authentication.
7. Dang ky service vao DI container.
8. Implement `HealthController`.
9. Implement guest login.
10. Implement employee/admin login.
11. Implement logout va `/api/auth/me`.
12. Ghi audit log cho login/logout.

Y nghia thu muc:
- `Modules/Auth`: login, logout, current user, cookie auth.
- `Modules/Users`: user entity, role, user lookup.
- `Modules/Sessions`: guest session.
- `Modules/Audit`: ghi lai su kien bao mat.
- `Infrastructure/Persistence`: SQL Server/EF Core.
- `Contracts/Auth`: request/response DTO cho API auth.

Giu Milestone 1 gon va chac. Chat, RAG, uploads, tools, admin dashboard se lam sau.
