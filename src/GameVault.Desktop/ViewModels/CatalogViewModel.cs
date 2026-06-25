using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GameVault.Data;
using GameVault.Services;

namespace GameVault.ViewModels;

public class CatalogViewModel : ViewModelBase
{
    private readonly CatalogService _catalogService;
    private readonly DownloadManagerService _downloadManagerService;
    private readonly GameRepository _gameRepository;
    private readonly ActivityLogRepository _activityLogRepository;
    private bool _isRefreshing;
    private string _statusMessage = string.Empty;

    public CatalogViewModel(
        CatalogService catalogService,
        DownloadManagerService downloadManagerService,
        GameRepository gameRepository,
        ActivityLogRepository activityLogRepository)
    {
        _catalogService = catalogService;
        _downloadManagerService = downloadManagerService;
        _gameRepository = gameRepository;
        _activityLogRepository = activityLogRepository;

        RefreshCatalogCommand = new AsyncRelayCommand(RefreshCatalogAsync);
        InstallCommand = new RelayCommand(StartInstall);
        CancelCommand = new RelayCommand(CancelInstall);
        OpenInLibraryCommand = new RelayCommand(_ => OpenLibraryRequested?.Invoke());
    }

    public ObservableCollection<CatalogGameItemViewModel> Games { get; } = [];
    public bool HasGames => Games.Count > 0;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCatalogCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenInLibraryCommand { get; }

    public event Action? LibraryChanged;
    public event Action? OpenLibraryRequested;

    public async Task RefreshCatalogAsync()
    {
        IsRefreshing = true;
        StatusMessage = "Loading server catalog...";

        try
        {
            var catalog = await _catalogService.GetCatalogAsync(AppSettings.DownloadManifestUrl);

            Games.Clear();
            var coverLoadTasks = new List<Task<bool>>();
            foreach (var manifestItem in catalog)
            {
                var item = new CatalogGameItemViewModel(manifestItem);
                var game = _gameRepository.GetGameByCatalogId(manifestItem.Id);
                if (game is not null)
                {
                    item.ApplyGameStatus(game.InstallStatus, game.DownloadProgress);
                }

                Games.Add(item);
                coverLoadTasks.Add(item.LoadCoverAsync(_catalogService));
            }

            await Task.WhenAll(coverLoadTasks);

            var updatedCachedCoverPaths = false;
            foreach (var item in Games)
            {
                var game = _gameRepository.GetGameByCatalogId(item.Id);
                if (game is null
                    || !string.IsNullOrWhiteSpace(game.CoverImagePath)
                    || !File.Exists(item.ManifestItem.CachedCoverPath))
                {
                    continue;
                }

                _gameRepository.UpdateCatalogInstallState(
                    item.Id,
                    game.InstallStatus,
                    game.DownloadProgress,
                    coverImagePath: item.ManifestItem.CachedCoverPath);
                updatedCachedCoverPaths = true;
            }

            if (updatedCachedCoverPaths)
            {
                LibraryChanged?.Invoke();
            }

            _activityLogRepository.AddEntry("Catalog refreshed");
            StatusMessage = Games.Count == 0
                ? "Server catalog is empty."
                : $"Catalog refreshed. {Games.Count} games available.";
        }
        catch
        {
            Games.Clear();
            StatusMessage = "Server catalog is unavailable.";
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(HasGames));
        }
    }

    public void ApplyDownloadState(DownloadState state)
    {
        void Update()
        {
            var item = Games.FirstOrDefault(game => game.Id == state.CatalogGameId);
            item?.ApplyDownloadState(state);
        }

        if (Application.Current?.Dispatcher.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }

    private void StartInstall(object? parameter)
    {
        if (parameter is not CatalogGameItemViewModel item)
        {
            return;
        }

        if (item.IsInstalled)
        {
            OpenLibraryRequested?.Invoke();
            return;
        }

        if (!item.CanInstall)
        {
            OpenLibraryRequested?.Invoke();
            return;
        }

        var progress = new Progress<DownloadState>(state =>
        {
            item.ApplyDownloadState(state);
        });

        _ = Task.Run(async () =>
        {
            await _downloadManagerService.InstallAsync(item.ManifestItem, progress);
            await Application.Current.Dispatcher.InvokeAsync(() => LibraryChanged?.Invoke());
        });

        OpenLibraryRequested?.Invoke();
    }

    private void CancelInstall(object? parameter)
    {
        if (parameter is CatalogGameItemViewModel item)
        {
            _downloadManagerService.Cancel(item.Id);
        }
    }
}
