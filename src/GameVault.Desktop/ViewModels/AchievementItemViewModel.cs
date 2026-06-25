namespace GameVault.ViewModels;

public class AchievementItemViewModel
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsUnlocked { get; init; }
    public DateTime? UnlockedAt { get; init; }
    public string StatusText => IsUnlocked ? "UNLOCKED" : "LOCKED";
    public string DateText => UnlockedAt is null
        ? "Keep exploring GameVault"
        : $"Unlocked {UnlockedAt:dd MMM yyyy}";
}
