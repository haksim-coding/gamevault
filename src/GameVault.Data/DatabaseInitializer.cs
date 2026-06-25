using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var context = new AppDbContext();
        context.Database.EnsureCreated();
        CreateMilestoneTables(context);
        AddCatalogColumns(context);
        SeedGames(context);
        SeedProfile(context);
        SeedAchievements(context);
        context.SaveChanges();
    }

    private static void AddCatalogColumns(AppDbContext context)
    {
        AddColumnIfMissing(context, "Games", "CatalogId", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(context, "Games", "InstallStatus", "TEXT NOT NULL DEFAULT 'Installed'");
        AddColumnIfMissing(context, "Games", "DownloadProgress", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(context, "Games", "Version", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(context, "Games", "DownloadUrl", "TEXT NOT NULL DEFAULT ''");
    }

    private static void AddColumnIfMissing(
        AppDbContext context,
        string tableName,
        string columnName,
        string definition)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }

        var alterTableSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {definition};";
        context.Database.ExecuteSqlRaw(alterTableSql);
    }

    private static void CreateMilestoneTables(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "ActivityLogs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityLogs" PRIMARY KEY AUTOINCREMENT,
                "GameTitle" TEXT NOT NULL,
                "Timestamp" TEXT NOT NULL,
                "Action" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "UserProfiles" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UserProfiles" PRIMARY KEY AUTOINCREMENT,
                "Username" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "Bio" TEXT NOT NULL,
                "FavoriteGame" TEXT NOT NULL,
                "AvatarImageBytes" BLOB NULL,
                "CreatedAt" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "Achievements" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Achievements" PRIMARY KEY AUTOINCREMENT,
                "Code" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "UserAchievements" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UserAchievements" PRIMARY KEY AUTOINCREMENT,
                "UserProfileId" INTEGER NOT NULL,
                "AchievementId" INTEGER NOT NULL,
                "UnlockedAt" TEXT NOT NULL,
                CONSTRAINT "FK_UserAchievements_UserProfiles_UserProfileId"
                    FOREIGN KEY ("UserProfileId") REFERENCES "UserProfiles" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_UserAchievements_Achievements_AchievementId"
                    FOREIGN KEY ("AchievementId") REFERENCES "Achievements" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Achievements_Code"
                ON "Achievements" ("Code");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAchievements_UserProfileId_AchievementId"
                ON "UserAchievements" ("UserProfileId", "AchievementId");
            """);
    }

    private static void SeedGames(AppDbContext context)
    {
        if (context.Games.Any())
        {
            return;
        }

        context.Games.AddRange(
            new Game
            {
                Title = "Hades",
                Genre = "Action Roguelike",
                ReleaseYear = 2020,
                Developer = "Supergiant Games",
                PlayTimeMinutes = 2520,
                DateAdded = DateTime.Now
            },
            new Game
            {
                Title = "Stardew Valley",
                Genre = "Farming RPG",
                ReleaseYear = 2016,
                Developer = "ConcernedApe",
                PlayTimeMinutes = 5220,
                DateAdded = DateTime.Now
            },
            new Game
            {
                Title = "Portal 2",
                Genre = "Puzzle Adventure",
                ReleaseYear = 2011,
                Developer = "Valve",
                PlayTimeMinutes = 1080,
                DateAdded = DateTime.Now
            });
    }

    private static void SeedProfile(AppDbContext context)
    {
        if (context.UserProfiles.Any())
        {
            return;
        }

        context.UserProfiles.Add(new UserProfile
        {
            Username = "player",
            DisplayName = "GameVault Player",
            Bio = "Building a personal game collection.",
            FavoriteGame = "Hades",
            CreatedAt = DateTime.Now
        });
    }

    private static void SeedAchievements(AppDbContext context)
    {
        if (context.Achievements.Any())
        {
            return;
        }

        context.Achievements.AddRange(
            new Achievement
            {
                Code = "FIRST_GAME_ADDED",
                Title = "First Entry",
                Description = "Add your first game to the library."
            },
            new Achievement
            {
                Code = "FIRST_LAUNCH",
                Title = "Ready to Play",
                Description = "Launch a game from GameVault."
            },
            new Achievement
            {
                Code = "FIVE_GAMES_ADDED",
                Title = "Collector",
                Description = "Build a library of at least five games."
            });
    }
}
