using GameVault.Core.Models;
using GameVault.Localization;

namespace GameVault.ViewModels;

public class ActivityItemViewModel
{
    public ActivityItemViewModel(ActivityLog log)
    {
        Action = FormatAction(log.Action);
        GameTitle = log.GameTitle;
        Timestamp = log.Timestamp;
        Description = CreateDescription(log.Action, log.GameTitle);
    }

    public string Action { get; }
    public string GameTitle { get; }
    public DateTime Timestamp { get; }
    public string Description { get; }
    public string TimestampText => Timestamp.ToString("dd MMM yyyy, HH:mm");

    private static readonly Dictionary<string, string> ActionKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GameAdded"] = "ActivityGameAdded", ["GameEdited"] = "ActivityGameEdited",
            ["GameDeleted"] = "ActivityGameDeleted", ["Launched"] = "ActivityLaunched",
            ["GameLibraryJsonExported"] = "ActivityJsonExported",
            ["GameLibraryJsonImported"] = "ActivityJsonImported",
            ["ActivityHistoryOpened"] = "ActivityHistoryOpenedLabel",
            ["ActivityHistoryOpenFailed"] = "ActivityHistoryFailed",
            ["ActivityLogXmlExported"] = "ActivityXmlExported",
            ["ActivityLogBinaryExported"] = "ActivityArchiveCreated",
            ["ActivityLogBinaryOpened"] = "ActivityArchiveOpened",
            ["ActivityLogBinaryOpenFailed"] = "ActivityArchiveFailed",
            ["SecureBackupCreated"] = "ActivityBackupCreated",
            ["SecureBackupRestored"] = "ActivityBackupRestored",
            ["BackupCheckStarted"] = "ActivityBackupCheck",
            ["Catalog refreshed"] = "ActivityCatalogRefreshed",
            ["Catalog signature rejected"] = "ActivitySignatureRejected",
            ["CatalogFileDeleteBlocked"] = "ActivityDeleteBlocked",
            ["CatalogFileDeleteFailed"] = "ActivityDeleteFailed",
            ["CatalogFilesDeleted"] = "ActivityFilesDeleted",
            ["Download cancelled"] = "ActivityDownloadCancelled",
            ["Download completed"] = "ActivityDownloadCompleted",
            ["Download failed"] = "ActivityDownloadFailed",
            ["Download started"] = "ActivityDownloadStarted",
            ["Game installed from server"] = "ActivityGameInstalled",
            ["SHA256 verified"] = "ActivityShaVerified"
        };

    private static string FormatAction(string action) =>
        ActionKeys.TryGetValue(action, out var key)
            ? LocalizationService.Instance.Get(key)
            : action;

    private static string CreateDescription(string action, string gameTitle)
    {
        var label = FormatAction(action);
        return string.IsNullOrWhiteSpace(gameTitle)
            ? label
            : LocalizationService.Instance.Format("ActivityWithGame", label, gameTitle);
    }
}
