using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameVault.Data;

namespace GameVault.Services;

public class PublicStatsSyncService : INotifyPropertyChanged
{
    private readonly GameVaultApiClient _apiClient;
    private readonly AuthSession _session;
    private readonly GameRepository _gameRepository;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private string _statusText = "Stats sync automatically";

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
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async Task TrySyncAsync()
    {
        if (!_session.IsLoggedIn)
        {
            StatusText = "Stats sync automatically";
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

            StatusText = $"Last synced: {DateTime.Now:HH:mm}";
        }
        catch (Exception)
        {
            StatusText = "Sync unavailable";
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
