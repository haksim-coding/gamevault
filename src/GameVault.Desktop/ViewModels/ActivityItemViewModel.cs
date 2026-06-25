using GameVault.Core.Models;

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

    private static string FormatAction(string action) =>
        action switch
        {
            "GameAdded" => "GAME ADDED",
            "GameEdited" => "GAME UPDATED",
            "GameDeleted" => "GAME DELETED",
            "Launched" => "GAME LAUNCHED",
            "GameLibraryJsonExported" => "JSON EXPORT",
            "GameLibraryJsonImported" => "JSON IMPORT",
            "ActivityHistoryOpened" => "ACTIVITY HISTORY",
            "ActivityHistoryOpenFailed" => "HISTORY FAILED",
            "ActivityLogXmlExported" => "ACTIVITY XML EXPORT",
            "ActivityLogXmlImported" => "ACTIVITY XML IMPORT",
            "ActivityLogXmlImportFailed" => "ACTIVITY XML FAILED",
            "ActivityLogBinaryExported" => "ACTIVITY BINARY EXPORT",
            "ActivityLogBinaryImported" => "ACTIVITY BINARY IMPORT",
            "ActivityLogBinaryImportFailed" => "ACTIVITY BINARY FAILED",
            "ActivityLogBinaryOpened" => "ACTIVITY ARCHIVE",
            "ActivityLogBinaryOpenFailed" => "ARCHIVE FAILED",
            "JsonExported" => "JSON EXPORT",
            "XmlExported" => "XML EXPORT",
            "SecureBackupCreated" => "SECURE BACKUP",
            "SecureBackupRestored" => "BACKUP RESTORED",
            _ => action.ToUpperInvariant()
        };

    private static string CreateDescription(string action, string gameTitle) =>
        action switch
        {
            "GameAdded" => $"Added {gameTitle} to the library.",
            "GameEdited" => $"Updated library details for {gameTitle}.",
            "GameDeleted" => $"Removed {gameTitle} from the library.",
            "Launched" => $"Launched {gameTitle} from GameVault.",
            "GameLibraryJsonExported" => "Created a JSON game library export.",
            "GameLibraryJsonImported" => "Imported games from a JSON library file.",
            "ActivityHistoryOpened" => "Opened an activity history summary.",
            "ActivityHistoryOpenFailed" => "Activity history file could not be opened.",
            "ActivityLogXmlExported" => "Created an XML activity log export.",
            "ActivityLogXmlImported" => "Imported activity log entries from XML.",
            "ActivityLogXmlImportFailed" => "Activity log XML import failed.",
            "ActivityLogBinaryExported" => "Created a custom binary activity archive.",
            "ActivityLogBinaryImported" => "Imported activity log entries from a binary archive.",
            "ActivityLogBinaryImportFailed" => "Activity binary archive import failed.",
            "ActivityLogBinaryOpened" => "Opened an activity archive summary.",
            "ActivityLogBinaryOpenFailed" => "Activity archive could not be opened.",
            "JsonExported" => "Created a plain JSON library export.",
            "XmlExported" => "Created a legacy XML library export.",
            "SecureBackupCreated" => "Created an encrypted GameVault backup.",
            "SecureBackupRestored" => "Restored the library from a secure backup.",
            _ when !string.IsNullOrWhiteSpace(gameTitle) => $"{action}: {gameTitle}",
            _ => action
        };
}
