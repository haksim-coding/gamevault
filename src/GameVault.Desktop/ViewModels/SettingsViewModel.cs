using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GameVault.Data;
using GameVault.Services;
using Microsoft.Win32;

namespace GameVault.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IniSettingsService _settingsService;
    private readonly LibraryExportService _exportService;
    private readonly ActivityLogArchiveService _activityLogArchiveService;
    private readonly SecureBackupService _secureBackupService;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly PasswordDialogService _passwordDialogService;
    private string _selectedAccentColor = "Purple";
    private string _selectedLanguage = "EN";
    private string _defaultInstallFolder = string.Empty;
    private string _selectedDownloadSpeedLimit = "Unlimited";
    private string _statusMessage = string.Empty;

    public SettingsViewModel(
        IniSettingsService settingsService,
        LibraryExportService exportService,
        ActivityLogArchiveService activityLogArchiveService,
        SecureBackupService secureBackupService,
        ActivityLogRepository activityLogRepository,
        PasswordDialogService passwordDialogService)
    {
        _settingsService = settingsService;
        _exportService = exportService;
        _activityLogArchiveService = activityLogArchiveService;
        _secureBackupService = secureBackupService;
        _activityLogRepository = activityLogRepository;
        _passwordDialogService = passwordDialogService;

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ExportJsonCommand = new RelayCommand(ExportJson);
        ImportJsonCommand = new RelayCommand(ImportJson);
        ExportXmlCommand = new RelayCommand(ExportXml);
        OpenActivityHistoryCommand = new RelayCommand(OpenActivityHistory);
        ExportBinaryActivityCommand = new RelayCommand(ExportBinaryActivity);
        OpenActivityArchiveCommand = new RelayCommand(OpenActivityArchive);
        CreateSecureBackupCommand = new RelayCommand(CreateSecureBackup);
        RestoreSecureBackupCommand = new RelayCommand(RestoreSecureBackup);

        Refresh();
    }

    public ObservableCollection<string> AccentColors { get; } =
        ["Purple", "Blue", "Green", "Orange"];
    public ObservableCollection<string> Languages { get; } = ["EN", "HR"];
    public ObservableCollection<string> DownloadSpeedLimits { get; } =
        ["Unlimited", "5 MB/s", "1 MB/s", "500 KB/s"];

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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand ImportJsonCommand { get; }
    public ICommand ExportXmlCommand { get; }
    public ICommand OpenActivityHistoryCommand { get; }
    public ICommand ExportBinaryActivityCommand { get; }
    public ICommand OpenActivityArchiveCommand { get; }
    public ICommand CreateSecureBackupCommand { get; }
    public ICommand RestoreSecureBackupCommand { get; }

    public event Action? LibraryImported;
    public event Action? BackupRestored;

    public void Refresh()
    {
        var settings = _settingsService.Load();
        SelectedAccentColor = AccentColors.Contains(settings.AccentColor)
            ? settings.AccentColor
            : "Purple";
        SelectedLanguage = Languages.Contains(settings.Language)
            ? settings.Language
            : "EN";
        DefaultInstallFolder = settings.DefaultInstallFolder;
        SelectedDownloadSpeedLimit = DownloadSpeedLimits.Contains(settings.DownloadSpeedLimit)
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
        _settingsService.Save(new AppSettings
        {
            AccentColor = SelectedAccentColor,
            Language = SelectedLanguage,
            DefaultInstallFolder = DefaultInstallFolder,
            DownloadSpeedLimit = SelectedDownloadSpeedLimit
        });
        AccentColorService.Apply(SelectedAccentColor);
        if (showStatus)
        {
            StatusMessage = $"Settings saved to {SettingsPath}";
        }
    }

    private void ExportJson()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog("GameVault JSON Export (*.json)|*.json", "gamevault-library.json");
            if (path is null)
            {
                return null;
            }

            _exportService.ExportJson(path);
            _activityLogRepository.AddEntry("GameLibraryJsonExported");
            return $"Library export created successfully:\n{path}";
        });
    }

    private void ExportXml()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog("GameVault Activity XML (*.xml)|*.xml", $"gamevault-activity-{DateTime.Now:yyyyMMdd}.xml");
            if (path is null)
            {
                return null;
            }

            _activityLogArchiveService.ExportActivityLogXml(path);
            _activityLogRepository.AddEntry("ActivityLogXmlExported");
            return $"Activity history file created successfully:\n{path}";
        });
    }

    private void ImportJson()
    {
        ImportFile(
            "GameVault JSON Export (*.json)|*.json",
            _exportService.ImportJson,
            "GameLibraryJsonImported",
            "new games");
    }

    private void OpenActivityHistory()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog("GameVault Activity History (*.xml)|*.xml");
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
                throw new InvalidDataException("This activity history file could not be opened.");
            }
        });
    }

    private void ExportBinaryActivity()
    {
        RunFileOperation(() =>
        {
            var path = ShowSaveDialog(
                "GameVault Activity Archive (*.gvactivity)|*.gvactivity",
                $"gamevault-activity-{DateTime.Now:yyyyMMdd}.gvactivity");
            if (path is null)
            {
                return null;
            }

            _activityLogArchiveService.ExportActivityLogBinary(path);
            _activityLogRepository.AddEntry("ActivityLogBinaryExported");
            return $"Activity archive created successfully:\n{path}";
        });
    }

    private void OpenActivityArchive()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog("GameVault Activity Archive (*.gvactivity)|*.gvactivity");
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
                    "Activity archive summary:",
                    $"Format version: {summary.FormatVersion}",
                    $"Exported: {summary.ExportedAt:g}",
                    $"Records: {summary.RecordCount}",
                    $"First activity: {FormatOptionalTimestamp(summary.FirstActivityTimestamp)}",
                    $"Last activity: {FormatOptionalTimestamp(summary.LastActivityTimestamp)}",
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
                "GameVault Secure Backup (*.gvb)|*.gvb",
                $"GameVault-Backup-{DateTime.Now:yyyyMMdd}.gvb");
            if (path is null)
            {
                return null;
            }

            var password = _passwordDialogService.AskForPassword("Create Secure Backup");
            if (password is null)
            {
                return null;
            }

            var backupPath = _secureBackupService.CreateBackup(path, password);
            _activityLogRepository.AddEntry("SecureBackupCreated");
            return $"Secure backup created successfully:\n{backupPath}\n\nThe backup is AES encrypted and includes an internal SHA256 integrity check.";
        });
    }

    private void RestoreSecureBackup()
    {
        RunFileOperation(() =>
        {
            var path = ShowOpenDialog(
                "GameVault Secure Backup (*.gvb)|*.gvb");
            if (path is null)
            {
                return null;
            }

            var password = _passwordDialogService.AskForPassword("Restore Secure Backup");
            if (password is null)
            {
                return null;
            }

            var importedCount = _secureBackupService.RestoreBackup(path, password);
            _activityLogRepository.AddEntry("SecureBackupRestored");
            LibraryImported?.Invoke();
            BackupRestored?.Invoke();
            return $"Secure backup restored. {importedCount} new games added; duplicates were skipped.";
        });
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

                return $"Import complete. {importedCount} {itemName} imported; duplicates were skipped.";
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
            StatusMessage = $"Operation failed: {exception.Message}";
            MessageBox.Show(
                StatusMessage,
                "GameVault Error",
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
        timestamp?.ToString("g") ?? "No activity records";

    private static string FormatActivityHistorySummary(
        ActivityLogArchiveService.ActivityHistorySummary summary,
        string path)
    {
        var lines = new List<string>
        {
            "Activity history summary:",
            $"Exported: {summary.ExportedAt:g}",
            $"Format version: {summary.FormatVersion}",
            $"Records: {summary.RecordCount}",
            $"First activity: {FormatActivity(summary.FirstActivity)}",
            $"Last activity: {FormatActivity(summary.LastActivity)}"
        };

        if (summary.Preview.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Preview:");
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
            return "No activity records";
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
