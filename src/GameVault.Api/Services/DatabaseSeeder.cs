using GameVault.Api.Data;
using GameVault.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Services;

public class DatabaseSeeder(ServerDbContext dbContext)
{
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public async Task SeedAsync()
    {
        if (await dbContext.AppUsers.AnyAsync())
        {
            return;
        }

        var admin = CreateUser("admin", "Admin", UserRole.Admin);
        var demo = CreateUser("demo", "Demo Player", UserRole.User);
        var friend = CreateUser("friend", "Friendly Player", UserRole.User);

        SetPassword(admin, "admin123");
        SetPassword(demo, "demo123");
        SetPassword(friend, "friend123");

        dbContext.AppUsers.AddRange(admin, demo, friend);
        await dbContext.SaveChangesAsync();

        dbContext.PublicGameStats.AddRange(
            new PublicGameStats
            {
                UserId = demo.Id,
                TotalGames = 12,
                TotalPlayTimeMinutes = 4_350,
                FavoriteGenre = "RPG",
                LastPlayedGame = "The Witcher 3"
            },
            new PublicGameStats
            {
                UserId = friend.Id,
                TotalGames = 8,
                TotalPlayTimeMinutes = 2_125,
                FavoriteGenre = "Strategy",
                LastPlayedGame = "Civilization VI"
            });

        await dbContext.SaveChangesAsync();
    }

    private static AppUser CreateUser(string username, string displayName, UserRole role) =>
        new()
        {
            Username = username,
            DisplayName = displayName,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

    private void SetPassword(AppUser user, string password)
    {
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
    }
}
