namespace GameVault.Core.Models;

public class UserAchievement
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public int AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.Now;
}
