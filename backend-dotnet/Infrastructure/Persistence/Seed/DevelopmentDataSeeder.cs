using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Infrastructure.Persistence.Seed;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        await SeedUserAsync(
            db,
            passwordHasher,
            email: "admin@company.com",
            displayName: "System Admin",
            role: UserRole.Admin,
            password: "Admin@123");

        await SeedUserAsync(
            db,
            passwordHasher,
            email: "employee@company.com",
            displayName: "Demo Employee",
            role: UserRole.Employee,
            password: "Employee@123");

        await db.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        AppDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        string email,
        string displayName,
        string role,
        string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var userExists = await db.Users.AnyAsync(user => user.Email == normalizedEmail);

        if (userExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName,
            Role = role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
    }
}
