using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameVault.Data;
using GameVault.Localization;

namespace GameVault.Services;

public class PublicStatsSyncService : INotifyPropertyChanged
{
    private readonly GameVaultApiClient _apiClient;
    private readonly AuthSession _session;
    private readonly GameRepository _gameRepository;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private string _statusKey = "StatsSyncAutomatic";
    private string? _statusArgument;

    public PublicStatsSyncService(
        GameVaultApiClient apiClient,
        AuthSession session,
        GameRepository gameRepository,
        ActivityLogRepository activityLogRepository)
    {
        _apiClient = apiClient;
        _session = session;
        _gameRepository = gameRepository;
        _activityLogRepository = activityLogRepository;
        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(StatusText));
    }

    public string StatusText => _statusArgument is null
        ? LocalizationService.Instance.Get(_statusKey)
        : LocalizationService.Instance.Format(_statusKey, _statusArgument);

    public async Task TrySyncAsync()
    {
        if (!_session.IsLoggedIn)
        {
            SetStatus("StatsSyncAutomatic");
            return;
        }

        await _syncLock.WaitAsync();
        try
        {
            if (!_session.IsLoggedIn)
            {
                return;
            }

            var games = _gameRepository.GetAllGames();
            var favoriteGenre = games
                .Where(game => !string.IsNullOrWhiteSpace(game.Genre))
                .GroupBy(game => game.Genre.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault() ?? string.Empty;

            var lastPlayedGame = _activityLogRepository.GetAll()
                .FirstOrDefault(log =>
                    log.Action.Equals("Launched", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(log.GameTitle))
                ?.GameTitle
                ?? games.OrderByDescending(game => game.DateAdded).FirstOrDefault()?.Title
                ?? string.Empty;

            await _apiClient.SyncStatsAsync(new UpdateStatsRequest(
                games.Count,
                games.Sum(game => game.PlayTimeMinutes),
                favoriteGenre,
                lastPlayedGame));

            SetStatus("LastSynced", DateTime.Now.ToString("HH:mm"));
        }
        catch (Exception)
        {
            SetStatus("SyncUnavailable");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetStatus(string key, string? argument = null)
    {
        _statusKey = key;
        _statusArgument = argument;
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
