using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using GameVault.Core.Models;
using GameVault.Data;
using GameVault.Services;

namespace GameVault.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    private readonly GameRepository _repository;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly GameDialogService _dialogService;
    private readonly GameLauncherService _launcherService;
    private readonly DownloadManagerService _downloadManagerService;
    private string _searchText = string.Empty;
    private string _selectedGenre = "All genres";
    private string _selectedSort = "Title A-Z";

    public LibraryViewModel(
        GameRepository repository,
        ActivityLogRepository activityLogRepository,
        GameDialogService dialogService,
        GameLauncherService launcherService,
        DownloadManagerService downloadManagerService)
    {
        _repository = repository;
        _activityLogRepository = activityLogRepository;
        _dialogService = dialogService;
        _launcherService = launcherService;
        _downloadManagerService = downloadManagerService;

        GamesView = CollectionViewSource.GetDefaultView(Games);
        GamesView.Filter = FilterGame;

        AddGameCommand = new RelayCommand(AddGame);
        EditGameCommand = new RelayCommand(EditGame);
        DeleteGameCommand = new RelayCommand(DeleteGame);
        PlayGameCommand = new RelayCommand(PlayGame);

        LoadGames();
    }

    public ObservableCollection<Game> Games { get; } = [];
    public ObservableCollection<string> Genres { get; } = ["All genres"];
    public ObservableCollection<string> SortOptions { get; } =
        ["Title A-Z", "Release Year", "Play Time", "Date Added"];
    public ICollectionView GamesView { get; }
    public int GameCount => GamesView.Cast<Game>().Count();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshFilter();
            }
        }
    }

    public string SelectedGenre
    {
        get => _selectedGenre;
        set
        {
            if (SetProperty(ref _selectedGenre, value))
            {
                RefreshFilter();
            }
        }
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (SetProperty(ref _selectedSort, value))
            {
                ApplySort();
            }
        }
    }

    public ICommand AddGameCommand { get; }
    public ICommand EditGameCommand { get; }
    public ICommand DeleteGameCommand { get; }
    public ICommand PlayGameCommand { get; }

    public event Action? GamesChanged;
    public event Action? GameLaunched;

    public void Refresh()
    {
        LoadGames();
    }

    public void ApplyDownloadState(DownloadState state)
    {
        var game = Games.FirstOrDefault(item => item.CatalogId == state.CatalogGameId);
        if (game is null)
        {
            if (state.Status is "Queued" or "Downloading" or "Installing")
            {
                LoadGames();
            }

            return;
        }

        if (state.Status is "Installed" or "Failed" or "Cancelled")
        {
            LoadGames();
            return;
        }

        game.InstallStatus = state.Status;
        game.DownloadProgress = state.ProgressPercent;
        game.DownloadSpeedBytesPerSecond = state.SpeedBytesPerSecond;
        var index = Games.IndexOf(game);
        Games[index] = game;
        GamesView.Refresh();
    }

    private void LoadGames()
    {
        Games.Clear();

        foreach (var game in _repository.GetAllGames())
        {
            Games.Add(game);
        }

        LoadGenres();
        ApplySort();
        RefreshFilter();
        GamesChanged?.Invoke();
    }

    private void LoadGenres()
    {
        var selectedGenre = SelectedGenre;

        Genres.Clear();
        Genres.Add("All genres");

        foreach (var genre in Games
                     .Select(game => game.Genre)
                     .Where(genre => !string.IsNullOrWhiteSpace(genre))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(genre => genre))
        {
            Genres.Add(genre);
        }

        SelectedGenre = Genres.Contains(selectedGenre) ? selectedGenre : "All genres";
    }

    private bool FilterGame(object item)
    {
        if (item is not Game game)
        {
            return false;
        }

        var matchesTitle = string.IsNullOrWhiteSpace(SearchText)
                           || game.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        var matchesGenre = SelectedGenre == "All genres"
                           || game.Genre.Equals(SelectedGenre, StringComparison.OrdinalIgnoreCase);

        return matchesTitle && matchesGenre;
    }

    private void RefreshFilter()
    {
        GamesView.Refresh();
        OnPropertyChanged(nameof(GameCount));
    }

    private void AddGame()
    {
        var game = _dialogService.ShowGameDialog();

        if (game is null)
        {
            return;
        }

        _repository.AddGame(game);
        _activityLogRepository.AddEntry("GameAdded", game.Title);
        LoadGames();
    }

    private void EditGame(object? parameter)
    {
        if (parameter is not Game existingGame)
        {
            return;
        }

        var game = _dialogService.ShowGameDialog(existingGame);

        if (game is null)
        {
            return;
        }

        _repository.UpdateGame(game);
        _activityLogRepository.AddEntry("GameEdited", game.Title);
        LoadGames();
    }

    private void DeleteGame(object? parameter)
    {
        if (parameter is not Game game)
        {
            return;
        }

        if (game.IsDownloadInProgress && !string.IsNullOrWhiteSpace(game.CatalogId))
        {
            _downloadManagerService.Cancel(game.CatalogId);
            return;
        }

        if (!_dialogService.ConfirmDelete(game.Title))
        {
            return;
        }

        _repository.DeleteGame(game.Id);
        _activityLogRepository.AddEntry("GameDeleted", game.Title);
        LoadGames();
    }

    private void PlayGame(object? parameter)
    {
        if (parameter is not Game game)
        {
            return;
        }

        if (!_launcherService.TryLaunch(game, out var errorMessage))
        {
            _dialogService.ShowError(errorMessage);
            return;
        }

        GameLaunched?.Invoke();
    }

    private void ApplySort()
    {
        GamesView.SortDescriptions.Clear();

        var sort = SelectedSort switch
        {
            "Release Year" => new SortDescription(
                nameof(Game.ReleaseYear),
                ListSortDirection.Descending),
            "Play Time" => new SortDescription(
                nameof(Game.PlayTimeMinutes),
                ListSortDirection.Descending),
            "Date Added" => new SortDescription(
                nameof(Game.DateAdded),
                ListSortDirection.Descending),
            _ => new SortDescription(
                nameof(Game.Title),
                ListSortDirection.Ascending)
        };

        GamesView.SortDescriptions.Add(sort);
        GamesView.Refresh();
    }
}
