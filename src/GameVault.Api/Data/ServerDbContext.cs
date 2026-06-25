using GameVault.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Data;

public class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<PublicGameStats> PublicGameStats => Set<PublicGameStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>();
            entity.Property(user => user.DisplayName).HasMaxLength(100);
            entity.Property(user => user.Bio).HasMaxLength(500);
            entity.Property(user => user.FavoriteGame).HasMaxLength(150);
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.Property(friendship => friendship.Status).HasConversion<string>();
            entity.HasIndex(friendship => new
            {
                friendship.RequesterUserId,
                friendship.ReceiverUserId
            }).IsUnique();

            entity.HasOne(friendship => friendship.RequesterUser)
                .WithMany()
                .HasForeignKey(friendship => friendship.RequesterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(friendship => friendship.ReceiverUser)
                .WithMany()
                .HasForeignKey(friendship => friendship.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PublicGameStats>(entity =>
        {
            entity.HasIndex(stats => stats.UserId).IsUnique();
            entity.Property(stats => stats.FavoriteGenre).HasMaxLength(100);
            entity.Property(stats => stats.LastPlayedGame).HasMaxLength(150);

            entity.HasOne(stats => stats.User)
                .WithOne(user => user.PublicGameStats)
                .HasForeignKey<PublicGameStats>(stats => stats.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
