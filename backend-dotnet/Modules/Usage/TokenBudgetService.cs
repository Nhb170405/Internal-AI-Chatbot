using System.Security.Claims;
using backend_dotnet.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Modules.Usage;

public sealed class TokenBudgetService
{
    private readonly AppDbContext _db;
    private readonly TokenBudgetOptions _options;

    public TokenBudgetService(
        AppDbContext db,
        IOptions<TokenBudgetOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task EnsureCanUseTokensAsync(
        ClaimsPrincipal principal,
        int estimatedTokens,
        CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 16:
        // 1. Doc role tu principal.
        // 2. Doc userId hoac guestSessionId tu claims.
        // 3. Lay daily limit theo role.
        // 4. Query TokenUsages trong ngay hien tai.
        // 5. Tinh usedTokens + estimatedTokens.
        // 6. Neu vuot limit thi throw InvalidOperationException voi message an toan.
        //
        // Goi y:
        // - estimatedTokens la so uoc tinh truoc khi goi OpenAI.
        // - Sau khi OpenAI tra ve, Chat/RAG service van luu usage thuc te nhu hien tai.
        await Task.CompletedTask;
    }

    public int GetDailyLimitForRole(string role)
    {
        // Bai tap:
        // 1. role admin -> AdminDailyTokenLimit.
        // 2. role employee -> EmployeeDailyTokenLimit.
        // 3. role guest -> GuestDailyTokenLimit.
        // 4. role khac -> 0 hoac throw tuy chinh sach.
        return 0;
    }
}
