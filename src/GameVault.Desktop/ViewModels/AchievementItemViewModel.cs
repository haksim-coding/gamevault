using GameVault.Localization;

namespace GameVault.ViewModels;

public class AchievementItemViewModel
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsUnlocked { get; init; }
    public DateTime? UnlockedAt { get; init; }
    public string StatusText => LocalizationService.Instance.Get(IsUnlocked ? "Unlocked" : "Locked");
    public string DateText => UnlockedAt is null
        ? LocalizationService.Instance.Get("KeepExploring")
        : LocalizationService.Instance.Format("UnlockedOn", $"{UnlockedAt:dd MMM yyyy}");
}
