using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AchievementRepository
{
    public AchievementRepository()
    {
        DatabaseInitializer.Initialize();
    }

    public void EvaluateForUser(int userProfileId)
    {
        using var context = new AppDbContext();
        var gameCount = context.Games.Count();
        var hasLaunchedGame = context.ActivityLogs.Any(log => log.Action == "Launched");

        if (gameCount >= 1)
        {
            Unlock(context, userProfileId, "FIRST_GAME_ADDED");
        }

        if (gameCount >= 5)
        {
            Unlock(context, userProfileId, "FIVE_GAMES_ADDED");
        }

        if (hasLaunchedGame)
        {
            Unlock(context, userProfileId, "FIRST_LAUNCH");
        }

        context.SaveChanges();
    }

    public List<Achievement> GetAll()
    {
        using var context = new AppDbContext();
        return context.Achievements.AsNoTracking().OrderBy(item => item.Id).ToList();
    }

    public Dictionary<int, DateTime> GetUnlockedForUser(int userProfileId)
    {
        using var context = new AppDbContext();
        return context.UserAchievements
            .AsNoTracking()
            .Where(item => item.UserProfileId == userProfileId)
            .ToDictionary(item => item.AchievementId, item => item.UnlockedAt);
    }

    private static void Unlock(AppDbContext context, int userProfileId, string code)
    {
        var achievementId = context.Achievements
            .Where(item => item.Code == code)
            .Select(item => item.Id)
            .Single();

        var alreadyUnlocked = context.UserAchievements.Any(item =>
            item.UserProfileId == userProfileId
            && item.AchievementId == achievementId);

        if (!alreadyUnlocked)
        {
            context.UserAchievements.Add(new UserAchievement
            {
                UserProfileId = userProfileId,
                AchievementId = achievementId,
                UnlockedAt = DateTime.Now
            });
        }
    }
}
