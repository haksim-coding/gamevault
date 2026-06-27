using System.Diagnostics;
using System.IO;
using GameVault.Core.Models;
using GameVault.Data;
using GameVault.Localization;

namespace GameVault.Services;

public class GameLauncherService
{
    private readonly ActivityLogRepository _activityLogRepository;

    public GameLauncherService(ActivityLogRepository activityLogRepository)
    {
        _activityLogRepository = activityLogRepository;
    }

    public bool TryLaunch(Game game, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(game.ExecutablePath))
        {
            errorMessage = LocalizationService.Instance.Format("NoExecutableConfigured", game.Title);
            return false;
        }

        if (!File.Exists(game.ExecutablePath))
        {
            errorMessage = LocalizationService.Instance.Format("ExecutableMissing", game.ExecutablePath);
            return false;
        }

        try
        {
            var workingDirectory = Directory.Exists(game.InstallPath)
                ? game.InstallPath
                : Path.GetDirectoryName(game.ExecutablePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = game.ExecutablePath,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = true
            });

            _activityLogRepository.AddLaunchedEntry(game.Title);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = LocalizationService.Instance.Format("LaunchFailed", game.Title, exception.Message);
            return false;
        }
    }
}
