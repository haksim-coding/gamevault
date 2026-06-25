using GameVault.Core.Models;

namespace GameVault.Services;

public class ReportData
{
    public required UserProfile Profile { get; init; }
    public required List<Game> Games { get; init; }
    public required List<ReportAchievement> Achievements { get; init; }
    public int TotalGames => Games.Count;
    public int TotalPlayTimeMinutes => Games.Sum(game => game.PlayTimeMinutes);
    public string MostCommonGenre => Games
        .Where(game => !string.IsNullOrWhiteSpace(game.Genre))
        .GroupBy(game => game.Genre, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key)
        .Select(group => group.Key)
        .FirstOrDefault() ?? "No genre data";
    public Game? NewestAddedGame => Games.OrderByDescending(game => game.DateAdded).FirstOrDefault();
    public int UnlockedAchievementCount => Achievements.Count(item => item.IsUnlocked);
}

public class ReportAchievement
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsUnlocked { get; init; }
    public DateTime? UnlockedAt { get; init; }
}
