namespace backend_dotnet.Modules.Usage;

public sealed class TokenBudgetOptions
{
    public int GuestDailyTokenLimit { get; set; } = 20_000;

    public int EmployeeDailyTokenLimit { get; set; } = 300_000;

    public int AdminDailyTokenLimit { get; set; } = 1_000_000;
}
