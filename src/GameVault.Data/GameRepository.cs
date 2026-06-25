using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class GameRepository
{
    public GameRepository()
    {
        DatabaseInitializer.Initialize();
    }

    public List<Game> GetAllGames()
    {
        using var context = new AppDbContext();
        return context.Games
            .AsNoTracking()
            .OrderBy(game => game.Title)
            .ToList();
    }

    public Game? GetGameById(int id)
    {
        using var context = new AppDbContext();
        return context.Games
            .AsNoTracking()
            .FirstOrDefault(game => game.Id == id);
    }

    public Game? GetGameByCatalogId(string catalogId)
    {
        using var context = new AppDbContext();
        return context.Games
            .AsNoTracking()
            .FirstOrDefault(game => game.CatalogId == catalogId);
    }

    public void AddGame(Game game)
    {
        using var context = new AppDbContext();
        context.Games.Add(game);
        context.SaveChanges();
    }

    public void UpdateGame(Game game)
    {
        using var context = new AppDbContext();
        context.Games.Update(game);
        context.SaveChanges();
    }

    public Game AddOrUpdateCatalogGame(Game game)
    {
        using var context = new AppDbContext();
        var existingGame = context.Games.FirstOrDefault(item => item.CatalogId == game.CatalogId);

        if (existingGame is null)
        {
            context.Games.Add(game);
            context.SaveChanges();
            return game;
        }

        existingGame.Title = game.Title;
        existingGame.Genre = game.Genre;
        existingGame.ReleaseYear = game.ReleaseYear;
        existingGame.Developer = game.Developer;
        existingGame.InstallPath = game.InstallPath;
        existingGame.ExecutablePath = game.ExecutablePath;
        existingGame.CoverImagePath = game.CoverImagePath;
        existingGame.InstallStatus = game.InstallStatus;
        existingGame.DownloadProgress = game.DownloadProgress;
        existingGame.Version = game.Version;
        existingGame.DownloadUrl = game.DownloadUrl;
        context.SaveChanges();
        return existingGame;
    }

    public void UpdateCatalogInstallState(
        string catalogId,
        string status,
        int progress,
        string installPath = "",
        string executablePath = "",
        string coverImagePath = "")
    {
        using var context = new AppDbContext();
        var game = context.Games.FirstOrDefault(item => item.CatalogId == catalogId);
        if (game is null)
        {
            return;
        }

        game.InstallStatus = status;
        game.DownloadProgress = progress;
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            game.InstallPath = installPath;
        }

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            game.ExecutablePath = executablePath;
        }

        if (!string.IsNullOrWhiteSpace(coverImagePath))
        {
            game.CoverImagePath = coverImagePath;
        }

        context.SaveChanges();
    }

    public void DeleteGame(int id)
    {
        using var context = new AppDbContext();
        var game = context.Games.Find(id);

        if (game is null)
        {
            return;
        }

        context.Games.Remove(game);
        context.SaveChanges();
    }

    public void DeleteGameByCatalogId(string catalogId)
    {
        using var context = new AppDbContext();
        var game = context.Games.FirstOrDefault(item => item.CatalogId == catalogId);

        if (game is null)
        {
            return;
        }

        context.Games.Remove(game);
        context.SaveChanges();
    }

    public int ImportGames(IEnumerable<Game> games)
    {
        using var context = new AppDbContext();
        var existingKeys = context.Games
            .AsNoTracking()
            .Select(game => new { game.Title, game.ReleaseYear })
            .ToList()
            .Select(game => $"{game.Title.Trim().ToUpperInvariant()}|{game.ReleaseYear}")
            .ToHashSet();

        var importedCount = 0;
        foreach (var game in games)
        {
            var key = $"{game.Title.Trim().ToUpperInvariant()}|{game.ReleaseYear}";
            if (existingKeys.Contains(key))
            {
                continue;
            }

            context.Games.Add(game);
            existingKeys.Add(key);
            importedCount++;
        }

        context.SaveChanges();
        return importedCount;
    }
}
