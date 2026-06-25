using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AppDbContext : DbContext
{
    public static string DatabasePath =>
        Path.Combine(AppContext.BaseDirectory, "gamevault.db");

    public DbSet<Game> Games => Set<Game>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Achievement>()
            .HasIndex(achievement => achievement.Code)
            .IsUnique();

        modelBuilder.Entity<UserAchievement>()
            .HasIndex(item => new { item.UserProfileId, item.AchievementId })
            .IsUnique();
    }
}
