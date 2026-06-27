using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GameVault.Data;
using GameVault.Localization;
using GameVault.Services;
using Microsoft.Win32;

namespace GameVault.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IniSettingsService _settingsService;
    private readonly LibraryExportService _exportService;
    private readonly ActivityLogArchiveService _activityLogArchiveService;
    private readonly SecureBackupService _secureBackupService;
    private readonly BackupToolVerificationService _backupToolVerificationService;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly PasswordDialogService _passwordDialogService;
    private readonly IServerDiscoveryClient _serverDiscoveryClient;
    private readonly IExternalRestService _externalRestService;
    private readonly GameVaultApiClient _apiClient;
    private DiscoveredServer? _lastDiscoveredServer;
    private string _selectedAccentColor = "Purple";
    private string _selectedLanguage = "EN";
    private string _defaultInstallFolder = string.Empty;
    private string _selectedDownloadSpeedLimit = "Unlimited";
    private string _apiBaseUrl = string.Empty;
    private string _filesBaseUrl = string.Empty;
    private string _downloadManifestUrl = string.Empty;
    private string _serverDiscoveryStatus = string.Empty;
    private string _statusMessage = string.Empty;
    private string? _diagnosticsDiscoveryStatus = LocalizationService.Instance.Get("NotChecked");
    private string? _diagnosticsLanServerIp;
    private string? _diagnosticsServerName = LocalizationService.Instance.Get("NotAvailable");
    private string? _diagnosticsServerVersion = LocalizationService.Instance.Get("NotAvailable");
    private string? _diagnosticsServerStatus = LocalizationService.Instance.Get("NotChecked");
    private string? _diagnosticsApiBaseUrl;
    private string? _diagnosticsFilesBaseUrl;
    private string? _diagnosticsManifestUrl;
    private string? _diagnosticsPublicClientIp = LocalizationService.Instance.Get("NotChecked");
    private string? _diagnosticsLastChecked = LocalizationService.Instance.Get("Never");
    private bool _isDiagnosticsRunning;

    public SettingsViewModel(
        IniSettingsService settingsService,
        LibraryExportService exportService,
        ActivityLogArchiveService activityLogArchiveService,
        SecureBackupService secureBackupService,
        BackupToolVerificationService backupToolVerificationService,
        ActivityLogRepository activityLogRepository,
        PasswordDialogService passwordDialogService,
        IServerDiscoveryClient serverDiscoveryClient,
        IExternalRestService externalRestService,
        GameVaultApiClient apiClient)
    {
        _settingsService = settingsService;
        _exportService = exportService;
        _activityLogArchiveService = activityLogArchiveService;
        _secureBackupService = secureBackupService;
        _backupToolVerificationService = backupToolVerificationService;
        _activityLogRepository = activityLogRepository;
        _passwordDialogService = passwordDialogService;
        _serverDiscoveryClient = serverDiscoveryClient;
        _externalRestService = externalRestService;
        _apiClient = apiClient;

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ExportJsonCommand = new RelayCommand(ExportJson);
        ImportJsonCommand = new RelayCommand(ImportJson);
        ExportXmlCommand = new RelayCommand(ExportXml);
        OpenActivityHistoryCommand = new RelayCommand(OpenActivityHistory);
        ExportBinaryActivityCommand = new RelayCommand(ExportBinaryActivity);
        OpenActivityArchiveCommand = new RelayCommand(OpenActivityArchive);
        CreateSecureBackupCommand = new RelayCommand(CreateSecureBackup);
        CheckSecureBackupCommand = new AsyncRelayCommand(CheckSecureBackupAsync);
        RestoreSecureBackupCommand = new RelayCommand(RestoreSecureBackup);
        FindGameVaultServerCommand = new AsyncRelayCommand(
            FindGameVaultServerAsync,
            () => !IsDiagnosticsRunning);
        RunNetworkDiagnosticsCommand = new AsyncRelayCommand(
            RunNetworkDiagnosticsAsync,
            () => !IsDiagnosticsRunning);

        Refresh();
    }

    public ObservableCollection<LocalizedOption> AccentColors { get; } =
    [
        new("Purple", "Purple"), new("Blue", "Blue"),
        new("Green", "Green"), new("Orange", "Orange")
    ];
    public ObservableCollection<LocalizedOption> Languages { get; } =
        [new("EN", "English"), new("HR", "Croatian")];
    public ObservableCollection<LocalizedOption> DownloadSpeedLimits { get; } =
    [
        new("Unlimited", "Unlimited"), new("5 MB/s", "5 MB/s"),
        new("1 MB/s", "1 MB/s"), new("500 KB/s", "500 KB/s")
    ];

    public string SelectedAccentColor
    {
        get => _selectedAccentColor;
        set => SetProperty(ref _selectedAccentColor, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    public string DefaultInstallFolder
    {
        get => _defaultInstallFolder;
        set => SetProperty(ref _defaultInstallFolder, value);
    }

    public string SelectedDownloadSpeedLimit
    {
        get => _selectedDownloadSpeedLimit;
        set
        {
            if (SetProperty(ref _selectedDownloadSpeedLimit, value))
            {
                SaveSettings(showStatus: false);
            }
        }
    }

    public string SettingsPath => _settingsService.SettingsPath;

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string FilesBaseUrl
    {
        get => _filesBaseUrl;
        set => SetProperty(ref _filesBaseUrl, value);
    }

    public string DownloadManifestUrl
    {
        get => _downloadManifestUrl;
        set => SetProperty(ref _downloadManifestUrl, value);
    }

    public string ServerDiscoveryStatus
    {
        get => _serverDiscoveryStatus;
        private set => SetProperty(ref _serverDiscoveryStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? DiagnosticsDiscoveryStatus
    {
        get => _diagnosticsDiscoveryStatus;
        private set => SetProperty(ref _diagnosticsDiscoveryStatus, value);
    }

    public string? DiagnosticsLanServerIp
    {
        get => _diagnosticsLanServerIp;
        private set => SetProperty(ref _diagnosticsLanServerIp, value);
    }

    public string? DiagnosticsServerName
    {
        get => _diagnosticsServerName;
        private set => SetProperty(ref _diagnosticsServerName, value);
    }

    public string? DiagnosticsServerVersion
    {
        get => _diagnosticsServerVersion;
        private set => SetProperty(ref _diagnosticsServerVersion, value);
    }

    public string? DiagnosticsServerStatus
    {
        get => _diagnosticsServerStatus;
        private set => SetProperty(ref _diagnosticsServerStatus, value);
    }

    public string? DiagnosticsApiBaseUrl
    {
        get => _diagnosticsApiBaseUrl;
        private set => SetProperty(ref _diagnosticsApiBaseUrl, value);
    }

    public string? DiagnosticsFilesBaseUrl
    {
        get => _diagnosticsFilesBaseUrl;
        private set => SetProperty(ref _diagnosticsFilesBaseUrl, value);
    }

    public string? DiagnosticsManifestUrl
    {
        get => _diagnosticsManifestUrl;
        private set => SetProperty(ref _diagnosticsManifestUrl, value);
    }

    public string? DiagnosticsPublicClientIp
    {
        get => _diagnosticsPublicClientIp;
        private set => SetProperty(ref _diagnosticsPublicClientIp, value);
    }

    public string? DiagnosticsLastChecked
    {
        get => _diagnosticsLastChecked;
        private set => SetProperty(ref _diagnosticsLastChecked, value);
    }

    public bool IsDiagnosticsRunning
    {
        get => _isDiagnosticsRunning;
        private set
        {
            if (SetProperty(ref _isDiagnosticsRunning, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand ImportJsonCommand { get; }
    public ICommand ExportXmlCommand { get; }
    public ICommand OpenActivityHistoryCommand { get; }
    public ICommand ExportBinaryActivityCommand { get; }
    public ICommand OpenActivityArchiveCommand { get; }
    public ICommand CreateSecureBackupCommand { get; }
    public ICommand CheckSecureBackupCommand { get; }
    public ICommand RestoreSecureBackupCommand { get; }
    public ICommand FindGameVaultServerCommand { get; }
    public ICommand RunNetworkDiagnosticsCommand { get; }

    public event Action? LibraryImported;
    public event Action? BackupRestored;

    public void Refresh()
    {
        var settings = _settingsService.Load();
        SelectedAccentColor = AccentColors.Any(option => option.Value == settings.AccentColor)
            ? settings.AccentColor
            : "Purple";
        SelectedLanguage = Languages.Any(option => option.Value == settings.Language)
            ? settings.Language
            : "EN";
        DefaultInstallFolder = settings.DefaultInstallFolder;
        ApiBaseUrl = settings.ApiBaseUrl;
        FilesBaseUrl = settings.FilesBaseUrl;
        DownloadManifestUrl = settings.DownloadManifestUrl;
        RefreshDiagnosticsFromSavedSettings(settings);
        SelectedDownloadSpeedLimit = DownloadSpeedLimits.Any(option => option.Value == settings.DownloadSpeedLimit)
            ? settings.DownloadSpeedLimit
            : "Unlimited";
        OnPropertyChanged(nameof(SettingsPath));
    }

    private void SaveSettings()
    {
        SaveSettings(showStatus: true);
    }

    private void SaveSettings(bool showStatus)
    {
        var currentSettings = _settingsService.Load();
        _settingsService.Save(new AppSettings
        {
            AccentColor = SelectedAccentColor,
            Language = SelectedLanguage,
            DefaultInstallFolder = DefaultInstallFolder,
            DownloadSpeedLimit = SelectedDownloadSpeedLimit,
            ApiBaseUrl = currentSettings.ApiBaseUrl,
            FilesBaseUrl = currentSettings.FilesBaseUrl,
            DownloadManifestUrl = currentSettings.DownloadManifestUrl
        });
        ApiBaseUrl = currentSettings.ApiBaseUrl;
        FilesBaseUrl = currentSettings.FilesBaseUrl;
        DownloadManifestUrl = currentSettings.DownloadManifestUrl;
        _apiClient.SetBaseUrl(currentSettings.ApiBaseUrl);
        AccentColorService.Apply(SelectedAccentColor);
        LocalizationService.Instance.SetLanguage(SelectedLanguage);
        if (showStatus)
        {
            StatusMessage = LocalizationService.Instance.Format("SettingsSavedTo", SettingsPath);
        }
    }

    private async Task FindGameVaultServerAsync()
    {
        if (IsDiagnosticsRunning)
        {
            return;
        }

        IsDiagnosticsRunning = true;
        ServerDiscoveryStatus = LocalizationService.Instance.Get("Searching");
        DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("Searching");
        StatusMessage = LocalizationService.Instance.Get("SearchingForServer");

        try
        {
            var result = await _serverDiscoveryClient.FindAndApplyServerSettingsAsync();
            DiagnosticsLastChecked = DateTime.Now.ToString("g");

            if (result.DiscoveredServer is not null)
            {
                _lastDiscoveredServer = result.DiscoveredServer;
                DiagnosticsLanServerIp = result.DiscoveredServer.ServerIp;
                DiagnosticsServerName = result.DiscoveredServer.ServerName;
            }

            if (!result.Succeeded || result.ServerStatus is null)
            {
                ServerDiscoveryStatus = result.Message;
                DiagnosticsDiscoveryStatus = result.DiscoveredServer is null
                    ? LocalizationService.Instance.Get("ServerUnavailable")
                    : LocalizationService.Instance.Get("ServerFoundStatusUnavailable");
                DiagnosticsServerStatus = LocalizationService.Instance.Get("ServerStatusUnavailable");
                StatusMessage = result.Message;
                return;
            }

            Refresh();
            _apiClient.SetBaseUrl(ApiBaseUrl);
            ApplyServerDiagnostics(result.DiscoveredServer!, result.ServerStatus);
            ServerDiscoveryStatus = LocalizationService.Instance.Format("ServerFound", result.ServerStatus.ServerName);
            StatusMessage = LocalizationService.Instance.Get("ServerSettingsUpdated");
        }
        catch
        {
            ServerDiscoveryStatus = LocalizationService.Instance.Get("GameVaultServerUnavailable");
            DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("ServerUnavailable");
            DiagnosticsServerStatus = LocalizationService.Instance.Get("ServerStatusUnavailable");
            DiagnosticsLastChecked = DateTime.Now.ToString("g");
            StatusMessage = LocalizationService.Instance.Get("GameVaultServerUnavailable");
        }
        finally
        {
            IsDiagnosticsRunning = false;
        }
    }

    private async Task RunNetworkDiagnosticsAsync()
    {
        if (IsDiagnosticsRunning)
        {
            return;
        }

        IsDiagnosticsRunning = true;
        DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("Checking");
        StatusMessage = LocalizationService.Instance.Get("RunningDiagnostics");
        TryLogActivity("NetworkDiagnosticsStarted");

        try
        {
            RefreshDiagnosticsFromSavedSettings(_settingsService.Load());

            var publicIpTask = TryGetPublicIpAddressAsync();
            var serverStatusTask = TryGetServerDiagnosticsAsync();
            await Task.WhenAll(publicIpTask, serverStatusTask);

            var publicIp = await publicIpTask;
            var (discoveredServer, serverStatus) = await serverStatusTask;

            var externalRestSucceeded = !string.IsNullOrWhiteSpace(publicIp);
            if (externalRestSucceeded)
            {
                DiagnosticsPublicClientIp = publicIp;
                TryLogActivity("ExternalRestPublicIpFetched");
            }
            else
            {
                DiagnosticsPublicClientIp = LocalizationService.Instance.Get("Unavailable");
            }

            var serverCheckSucceeded = serverStatus is not null;
            if (serverCheckSucceeded)
            {
                ApplyServerDiagnostics(discoveredServer!, serverStatus!);
            }
            else if (discoveredServer is not null)
            {
                DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("ServerFoundStatusUnavailable");
                DiagnosticsLanServerIp = discoveredServer.ServerIp;
                DiagnosticsServerName = discoveredServer.ServerName;
                DiagnosticsServerStatus = LocalizationService.Instance.Get("ServerStatusUnavailable");
            }
            else
            {
                DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("ServerUnavailable");
                DiagnosticsServerStatus = LocalizationService.Instance.Get("ServerStatusUnavailable");
            }

            DiagnosticsLastChecked = DateTime.Now.ToString("g");

            if (externalRestSucceeded && serverCheckSucceeded)
            {
                StatusMessage = LocalizationService.Instance.Get("DiagnosticsCompleted");
                TryLogActivity("NetworkDiagnosticsSucceeded");
            }
            else
            {
                StatusMessage = BuildDiagnosticsWarning(
                    externalRestSucceeded,
                    serverCheckSucceeded);
                TryLogActivity("NetworkDiagnosticsFailed");
            }
        }
        catch
        {
            DiagnosticsLastChecked = DateTime.Now.ToString("g");
            StatusMessage = LocalizationService.Instance.Get("DiagnosticsWarnings");
            TryLogActivity("NetworkDiagnosticsFailed");
        }
        finally
        {
            IsDiagnosticsRunning = false;
        }
    }

    private async Task<string?> TryGetPublicIpAddressAsync()
    {
        try
        {
            return await _externalRestService.GetPublicIpAddressAsync();
        }
        catch
        {
            return null;
        }
    }

    private async Task<(DiscoveredServer? DiscoveredServer, GameVaultServerStatus? Status)>
        TryGetServerDiagnosticsAsync()
    {
        try
        {
            var discoveredServer = _lastDiscoveredServer
                ?? await _serverDiscoveryClient.DiscoverServerAsync();
            if (discoveredServer is null)
            {
                return (null, null);
            }

            _lastDiscoveredServer = discoveredServer;
            var status = await _serverDiscoveryClient.GetServerStatusAsync(discoveredServer);
            return (discoveredServer, status);
        }
        catch
        {
            return (_lastDiscoveredServer, null);
        }
    }

    private void ApplyServerDiagnostics(
        DiscoveredServer discoveredServer,
        GameVaultServerStatus status)
    {
        _lastDiscoveredServer = discoveredServer;
        DiagnosticsDiscoveryStatus = LocalizationService.Instance.Get("Connected");
        DiagnosticsLanServerIp = discoveredServer.ServerIp;
        DiagnosticsServerName = status.ServerName;
        DiagnosticsServerVersion = status.ServerVersion;
        DiagnosticsServerStatus = status.Status;
        DiagnosticsApiBaseUrl = status.ApiBaseUrl;
        DiagnosticsFilesBaseUrl = status.FilesBaseUrl;
        DiagnosticsManifestUrl = status.ManifestUrl;
    }

    private void RefreshDiagnosticsFromSavedSettings(AppSettings settings)
    {
        DiagnosticsApiBaseUrl = settings.ApiBaseUrl;
        DiagnosticsFilesBaseUrl = settings.FilesBaseUrl;
        DiagnosticsManifestUrl = settings.DownloadManifestUrl;

        if (_lastDiscoveredServer is null)
        {
            DiagnosticsLanServerIp = TryGetHost(settings.ApiBaseUrl)
                ?? LocalizationService.Instance.Get("NotAvailable");
        }
    }

    private static string? TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;

    private static string BuildDiagnosticsWarning(
        bool externalRestSucceeded,
        bool serverCheckSucceeded)
    {
        if (!externalRestSucceeded && !serverCheckSucceeded)
        {
            return LocalizationService.Instance.Get("DiagnosticsBothFailed");
        }

        return !externalRestSucceeded
            ? LocalizationService.Instance.Get("DiagnosticsExternalFailed")
            : LocalizationService.Instance.Get("DiagnosticsServerFailed");
    }

    private void ExportJson()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog(LocalizationService.Instance.Get("JsonExportFilter"), "gamevault-library.json");
            if (path is null)
            {
                return null;
            }

            _exportService.ExportJson(path);
            _activityLogRepository.AddEntry("GameLibraryJsonExported");
            return LocalizationService.Instance.Format("LibraryExportCreated", path);
        });
    }

    private void ExportXml()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog(LocalizationService.Instance.Get("ActivityXmlFilter"),
                $"gamevault-activity-{DateTime.Now:yyyyMMdd}.xml");
            if (path is null)
            {
                return null;
            }

            _activityLogArchiveService.ExportActivityLogXml(path);
            _activityLogRepository.AddEntry("ActivityLogXmlExported");
            return LocalizationService.Instance.Format("ActivityHistoryCreated", path);
        });
    }

    private void ImportJson()
    {
        ImportFile(
            LocalizationService.Instance.Get("JsonExportFilter"),
            _exportService.ImportJson,
            "GameLibraryJsonImported",
            string.Empty);
    }

    private void OpenActivityHistory()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog(LocalizationService.Instance.Get("ActivityHistoryFilter"));
            if (path is null)
            {
                return null;
            }

            try
            {
                var summary = _activityLogArchiveService.ReadActivityHistorySummary(path);
                _activityLogRepository.AddEntry("ActivityHistoryOpened");
                return FormatActivityHistorySummary(summary, path);
            }
            catch
            {
                TryLogActivity("ActivityHistoryOpenFailed");
                throw new InvalidDataException(LocalizationService.Instance.Get("ActivityHistoryOpenFailed"));
            }
        });
    }

    private void ExportBinaryActivity()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog(
                LocalizationService.Instance.Get("ActivityArchiveFilter"),
                $"gamevault-activity-{DateTime.Now:yyyyMMdd}.gvactivity");
            if (path is null)
            {
                return null;
            }

            _activityLogArchiveService.ExportActivityLogBinary(path);
            _activityLogRepository.AddEntry("ActivityLogBinaryExported");
            return LocalizationService.Instance.Format("ActivityArchiveCreatedPath", path);
        });
    }

    private void OpenActivityArchive()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog(LocalizationService.Instance.Get("ActivityArchiveFilter"));
            if (path is null)
            {
                return null;
            }

            try
            {
                var summary = _activityLogArchiveService.ReadActivityArchiveSummary(path);
                _activityLogRepository.AddEntry("ActivityLogBinaryOpened");

                return string.Join(
                    Environment.NewLine,
                    LocalizationService.Instance.Get("ArchiveSummary"),
                    LocalizationService.Instance.Format("FormatVersion", summary.FormatVersion),
                    LocalizationService.Instance.Format("ExportedAt", summary.ExportedAt.ToString("g")),
                    LocalizationService.Instance.Format("RecordsCount", summary.RecordCount),
                    LocalizationService.Instance.Format("FirstActivity", FormatOptionalTimestamp(summary.FirstActivityTimestamp)),
                    LocalizationService.Instance.Format("LastActivity", FormatOptionalTimestamp(summary.LastActivityTimestamp)),
                    string.Empty,
                    path);
            }
            catch
            {
                TryLogActivity("ActivityLogBinaryOpenFailed");
                throw;
            }
        });
    }

    private void CreateSecureBackup()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog(
                LocalizationService.Instance.Get("SecureBackupFilter"),
                $"GameVault-Backup-{DateTime.Now:yyyyMMdd}.gvb");
            if (path is null)
            {
                return null;
            }

            var password = _passwordDialogService.AskForPassword(
                LocalizationService.Instance.Get("CreateSecureBackup"));
            if (password is null)
            {
                return null;
            }

            var backupPath = _secureBackupService.CreateBackup(path, password);
            _activityLogRepository.AddEntry("SecureBackupCreated");
            return LocalizationService.Instance.Format("SecureBackupCreatedDetails", backupPath);
        });
    }

    private async Task CheckSecureBackupAsync()
    {
        var path = ShowOpenDialog(LocalizationService.Instance.Get("SecureBackupFilter"));
        if (path is null)
        {
            return;
        }

        var password = _passwordDialogService.AskForPassword(
            LocalizationService.Instance.Get("CheckBackupFile"));
        if (password is null)
        {
            return;
        }

        try
        {
            _activityLogRepository.AddEntry("BackupCheckStarted");
            var result = await _backupToolVerificationService.VerifyAsync(path, password);
            var message = FormatBackupToolResult(result);

            _activityLogRepository.AddEntry(
                result.ExitCode == BackupToolExitCode.Valid
                    ? "BackupCheckSucceeded"
                    : "BackupCheckFailed");

            StatusMessage = message;
            MessageBox.Show(
                message,
                LocalizationService.Instance.Get("BackupCheckTitle"),
                MessageBoxButton.OK,
                result.ExitCode == BackupToolExitCode.Valid
                    ? MessageBoxImage.Information
                    : MessageBoxImage.Warning);
        }
        catch (Exception exception)
        {
            TryLogActivity("BackupCheckFailed");
            StatusMessage = LocalizationService.Instance.Format("UnexpectedErrorDetails", exception.Message);
            MessageBox.Show(
                StatusMessage,
                LocalizationService.Instance.Get("GameVaultError"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RestoreSecureBackup()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog(
                LocalizationService.Instance.Get("SecureBackupFilter"));
            if (path is null)
            {
                return null;
            }

            var password = _passwordDialogService.AskForPassword(
                LocalizationService.Instance.Get("RestoreSecureBackup"));
            if (password is null)
            {
                return null;
            }

            var importedCount = _secureBackupService.RestoreBackup(path, password);
            _activityLogRepository.AddEntry("SecureBackupRestored");
            LibraryImported?.Invoke();
            BackupRestored?.Invoke();
            return LocalizationService.Instance.Format("SecureBackupRestoredCount", importedCount);
        });
    }

    private static string FormatBackupToolResult(BackupToolResult result)
    {
        return result.ExitCode switch
        {
            BackupToolExitCode.Valid => LocalizationService.Instance.Get("ValidBackup"),
            BackupToolExitCode.FileNotFound => LocalizationService.Instance.Get("FileNotFound"),
            BackupToolExitCode.InvalidBackupFormat => LocalizationService.Instance.Get("InvalidBackupFormat"),
            BackupToolExitCode.WrongPasswordOrDecryptionFailed => LocalizationService.Instance.Get("WrongPasswordIntegrity"),
            BackupToolExitCode.IntegrityCheckFailed => LocalizationService.Instance.Get("WrongPasswordIntegrity"),
            BackupToolExitCode.ToolNotFound => LocalizationService.Instance.Get("BackupToolNotFound"),
            BackupToolExitCode.InvalidArguments => LocalizationService.Instance.Get("BackupToolInvalidArguments"),
            _ => string.IsNullOrWhiteSpace(result.CombinedOutput)
                ? LocalizationService.Instance.Get("UnexpectedError")
                : LocalizationService.Instance.Format("UnexpectedErrorDetails", result.CombinedOutput)
        };
    }

    private void ImportFile(
        string filter,
        Func<string, int> importer,
        string successEvent,
        string itemName,
        string? failedEvent = null)
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog(filter);
            if (path is null)
            {
                return null;
            }

            try
            {
                var importedCount = importer(path);
                _activityLogRepository.AddEntry(successEvent);
                if (successEvent == "GameLibraryJsonImported")
                {
                    LibraryImported?.Invoke();
                }

                return LocalizationService.Instance.Format("ImportComplete", importedCount);
            }
            catch
            {
                if (failedEvent is not null)
                {
                    _activityLogRepository.AddEntry(failedEvent);
                }

                throw;
            }
        });
    }

    private void RunFileOperation(Func<string?> operation)
    {
        try
        {
            var message = operation();
            if (message is null)
            {
                return;
            }

            StatusMessage = message;
            MessageBox.Show(
                message,
                "GameVault",
                MessageBoxButton.OK,
                message.Contains("failed", StringComparison.OrdinalIgnoreCase)
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            StatusMessage = exception switch
            {
                InvalidDataException => LocalizationService.Instance.Get("InvalidFile"),
                FileNotFoundException => LocalizationService.Instance.Get("FileNotFound"),
                _ => LocalizationService.Instance.Format("OperationFailed", exception.Message)
            };
            MessageBox.Show(
                StatusMessage,
                LocalizationService.Instance.Get("GameVaultError"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string? ShowSaveDialog(string filter, string fileName)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GameVault",
            "Exports");
        Directory.CreateDirectory(folder);

        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = fileName,
            InitialDirectory = folder,
            AddExtension = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string? ShowOpenDialog(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string FormatOptionalTimestamp(DateTime? timestamp) =>
        timestamp?.ToString("g") ?? LocalizationService.Instance.Get("NoActivityRecords");

    private static string FormatActivityHistorySummary(
        ActivityLogArchiveService.ActivityHistorySummary summary,
        string path)
    {
        var lines = new List<string>
        {
            LocalizationService.Instance.Get("HistorySummary"),
            LocalizationService.Instance.Format("ExportedAt", summary.ExportedAt.ToString("g")),
            LocalizationService.Instance.Format("FormatVersion", summary.FormatVersion),
            LocalizationService.Instance.Format("RecordsCount", summary.RecordCount),
            LocalizationService.Instance.Format("FirstActivity", FormatActivity(summary.FirstActivity)),
            LocalizationService.Instance.Format("LastActivity", FormatActivity(summary.LastActivity))
        };

        if (summary.Preview.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add(LocalizationService.Instance.Get("Preview"));
            lines.AddRange(summary.Preview.Select(FormatActivity));
        }

        lines.Add(string.Empty);
        lines.Add(path);
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatActivity(ActivityLogArchiveService.ActivityLogXmlItem? activity)
    {
        if (activity is null)
        {
            return LocalizationService.Instance.Get("NoActivityRecords");
        }

        var parts = new[]
        {
            activity.Timestamp.ToString("g"),
            activity.Action,
            activity.GameTitle,
            activity.Details
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        return string.Join(" - ", parts);
    }

    private void TryLogActivity(string action)
    {
        try
        {
            _activityLogRepository.AddEntry(action);
        }
        catch
        {
            // Opening a file should not fail because activity logging failed.
        }
    }
}
