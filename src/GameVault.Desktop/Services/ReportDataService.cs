using GameVault.Data;

namespace GameVault.Services;

public class ReportDataService
{
    private readonly GameRepository _gameRepository;
    private readonly UserProfileRepository _profileRepository;
    private readonly AchievementRepository _achievementRepository;

    public ReportDataService(
        GameRepository gameRepository,
        UserProfileRepository profileRepository,
        AchievementRepository achievementRepository)
    {
        _gameRepository = gameRepository;
        _profileRepository = profileRepository;
        _achievementRepository = achievementRepository;
    }

    public ReportData GetReportData()
    {
        var profile = _profileRepository.GetProfile();
        _achievementRepository.EvaluateForUser(profile.Id);

        var unlocked = _achievementRepository.GetUnlockedForUser(profile.Id);
        var achievements = _achievementRepository.GetAll()
            .Select(achievement =>
            {
                var isUnlocked = unlocked.TryGetValue(achievement.Id, out var unlockedAt);
                return new ReportAchievement
                {
                    Title = achievement.Title,
                    Description = achievement.Description,
                    IsUnlocked = isUnlocked,
                    UnlockedAt = isUnlocked ? unlockedAt : null
                };
            })
            .ToList();

        return new ReportData
        {
            Profile = profile,
            Games = _gameRepository.GetAllGames(),
            Achievements = achievements
        };
    }
}
