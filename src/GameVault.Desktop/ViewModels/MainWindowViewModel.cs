using System.Windows.Input;
using System.Windows;
using System.Net.Http;
using GameVault.Data;
using GameVault.Services;

namespace GameVault.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    private readonly AuthSession _authSession;

    public MainWindowViewModel()
    {
        var repository = new GameRepository();
        var activityLogRepository = new ActivityLogRepository();
        var dialogService = new GameDialogService();
        var launcherService = new GameLauncherService(activityLogRepository);
        var profileRepository = new UserProfileRepository();
        var achievementRepository = new AchievementRepository();
        var reportDataService = new ReportDataService(
            repository,
            profileRepository,
            achievementRepository);
        var settingsService = new IniSettingsService();
        settingsService.Load();
        _authSession = new AuthSession();
        var apiClient = new GameVaultApiClient(AppSettings.ApiBaseUrl, _authSession);
        var statsSyncService = new PublicStatsSyncService(
            apiClient,
            _authSession,
            repository,
            activityLogRepository);
        var exportService = new LibraryExportService(repository);
        var encryptionService = new AesEncryptionService();
        var httpClient = new HttpClient();
        var catalogService = new CatalogService(httpClient);
        var downloadManagerService = new DownloadManagerService(
            httpClient,
            repository,
            activityLogRepository,
            settingsService,
            new HashService());

        HomeViewModel = new HomeViewModel(repository.GetAllGames(), activityLogRepository);
        LibraryViewModel = new LibraryViewModel(
            repository,
            activityLogRepository,
            dialogService,
            launcherService,
            downloadManagerService);
        CatalogViewModel = new CatalogViewModel(
            catalogService,
            downloadManagerService,
            repository,
            activityLogRepository);
        ProfileViewModel = new ProfileViewModel(profileRepository, achievementRepository);
        ReportsViewModel = new ReportsViewModel(reportDataService, new PdfReportService());
        AuthViewModel = new AuthViewModel(apiClient, _authSession, statsSyncService);
        FriendsViewModel = new FriendsViewModel(
            apiClient,
            _authSession,
            statsSyncService);
        SettingsViewModel = new SettingsViewModel(
            settingsService,
            exportService,
            new ActivityLogArchiveService(activityLogRepository),
            new SecureBackupService(exportService, encryptionService),
            activityLogRepository,
            new PasswordDialogService());
        _authSession.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AccountName));
            OnPropertyChanged(nameof(AccountStatus));
        };
        LibraryViewModel.GamesChanged += () =>
        {
            HomeViewModel.Refresh(repository.GetAllGames());
            ProfileViewModel.Refresh();
            ReportsViewModel.Refresh();
        };
        LibraryViewModel.GameLaunched += () => _ = statsSyncService.TrySyncAsync();
        CatalogViewModel.LibraryChanged += RefreshGameData;
        CatalogViewModel.OpenLibraryRequested += () => CurrentViewModel = LibraryViewModel;
        downloadManagerService.StateChanged += state =>
        {
            void ApplyState()
            {
                LibraryViewModel.ApplyDownloadState(state);
                CatalogViewModel.ApplyDownloadState(state);
                if (state.Status is "Installed" or "Failed" or "Cancelled")
                {
                    HomeViewModel.Refresh(repository.GetAllGames());
                    ProfileViewModel.Refresh();
                    ReportsViewModel.Refresh();
                }
            }

            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(ApplyState);
            }
            else
            {
                ApplyState();
            }
        };
        SettingsViewModel.LibraryImported += RefreshGameData;
        SettingsViewModel.BackupRestored += () => _ = statsSyncService.TrySyncAsync();
        _currentViewModel = HomeViewModel;

        ShowHomeCommand = new RelayCommand(() =>
        {
            HomeViewModel.Refresh(repository.GetAllGames());
            CurrentViewModel = HomeViewModel;
        });
        ShowLibraryCommand = new RelayCommand(() => CurrentViewModel = LibraryViewModel);
        ShowCatalogCommand = new AsyncRelayCommand(async () =>
        {
            CurrentViewModel = CatalogViewModel;
            await CatalogViewModel.RefreshCatalogAsync();
        });
        ShowProfileCommand = new RelayCommand(() =>
        {
            ProfileViewModel.Refresh();
            CurrentViewModel = ProfileViewModel;
        });
        ShowFriendsCommand = new AsyncRelayCommand(async () =>
        {
            CurrentViewModel = FriendsViewModel;
            await FriendsViewModel.RefreshAsync();
        });
        ShowAuthCommand = new RelayCommand(() => CurrentViewModel = AuthViewModel);
        ShowReportsCommand = new RelayCommand(() =>
        {
            ReportsViewModel.Refresh();
            CurrentViewModel = ReportsViewModel;
        });
        ShowSettingsCommand = new RelayCommand(() =>
        {
            SettingsViewModel.Refresh();
            CurrentViewModel = SettingsViewModel;
        });
    }

    public HomeViewModel HomeViewModel { get; }
    public LibraryViewModel LibraryViewModel { get; }
    public CatalogViewModel CatalogViewModel { get; }
    public ProfileViewModel ProfileViewModel { get; }
    public ReportsViewModel ReportsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public AuthViewModel AuthViewModel { get; }
    public FriendsViewModel FriendsViewModel { get; }

    public string AccountName => _authSession.IsLoggedIn
        ? _authSession.Username
        : "Not logged in";

    public string AccountStatus => _authSession.IsLoggedIn
        ? $"{_authSession.Role} · online features enabled"
        : "Open Account to log in";

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public ICommand ShowHomeCommand { get; }
    public ICommand ShowLibraryCommand { get; }
    public ICommand ShowCatalogCommand { get; }
    public ICommand ShowProfileCommand { get; }
    public ICommand ShowFriendsCommand { get; }
    public ICommand ShowAuthCommand { get; }
    public ICommand ShowReportsCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    private void RefreshGameData()
    {
        LibraryViewModel.Refresh();
        HomeViewModel.Refresh(new GameRepository().GetAllGames());
        ProfileViewModel.Refresh();
        ReportsViewModel.Refresh();
    }
}
