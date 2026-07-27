using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Infrastructure.Persistence.Seed;

public static class AdminBootstrapSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("BootstrapAdmin:Enabled"))
        {
            return;
        }

        var email = (configuration["BootstrapAdmin:Email"] ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Email is required when admin bootstrap is enabled.");
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Password must contain at least 12 characters.");
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(user => user.Role == UserRole.Admin))
        {
            return;
        }

        if (await db.Users.AnyAsync(user => user.Email == email))
        {
            throw new InvalidOperationException(
                "The bootstrap admin email is already assigned to a non-admin user.");
        }

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AppUser>>();
        var now = DateTimeOffset.UtcNow;
        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = "System Admin",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
