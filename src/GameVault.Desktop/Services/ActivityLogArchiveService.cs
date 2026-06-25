using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameVault.Core.Models;
using GameVault.Data;

namespace GameVault.Services;

public class ActivityLogArchiveService
{
    private const string ApplicationName = "GameVault";
    private const int FormatVersion = 1;
    private const string BinaryMagic = "GVACT";

    private readonly ActivityLogRepository _activityLogRepository;

    public ActivityLogArchiveService(ActivityLogRepository activityLogRepository)
    {
        _activityLogRepository = activityLogRepository;
    }

    public void ExportActivityLogXml(string filePath)
    {
        var export = new ActivityLogXmlExport
        {
            ExportedAt = DateTime.Now,
            Activities = GetActivityItems()
        };

        var serializer = new XmlSerializer(typeof(ActivityLogXmlExport));
        using var stream = File.Create(filePath);
        serializer.Serialize(stream, export);
    }

    public int ImportActivityLogXml(string filePath)
    {
        var serializer = new XmlSerializer(typeof(ActivityLogXmlExport));
        using var stream = File.OpenRead(filePath);
        var export = serializer.Deserialize(stream) as ActivityLogXmlExport
                     ?? throw new InvalidDataException("XML file does not contain a GameVault activity log export.");

        if (export.FormatVersion != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported activity XML format version: {export.FormatVersion}.");
        }

        return _activityLogRepository.ImportEntries(export.Activities.Select(item => new ActivityLog
        {
            Timestamp = item.Timestamp == default ? DateTime.Now : item.Timestamp,
            Action = item.Action.Trim(),
            GameTitle = item.GameTitle.Trim()
        }));
    }

    public ActivityHistorySummary ReadActivityHistorySummary(string filePath)
    {
        if (new FileInfo(filePath).Length == 0)
        {
            throw new InvalidDataException("Activity history file is empty.");
        }

        var document = XDocument.Load(filePath);
        if (document.Root?.Name.LocalName != "GameVaultActivityLogExport")
        {
            throw new InvalidDataException("Activity history root element is invalid.");
        }

        if (document.Root.Element("FormatVersion") is null)
        {
            throw new InvalidDataException("Activity history format version is missing.");
        }

        if (document.Root.Element("Activities") is null)
        {
            throw new InvalidDataException("Activity history activities list is missing.");
        }

        var serializer = new XmlSerializer(typeof(ActivityLogXmlExport));
        using var stream = File.OpenRead(filePath);
        var export = serializer.Deserialize(stream) as ActivityLogXmlExport
                     ?? throw new InvalidDataException("XML file does not contain a GameVault activity history export.");

        if (export.FormatVersion <= 0)
        {
            throw new InvalidDataException("Activity history format version is missing.");
        }

        if (export.Activities is null)
        {
            throw new InvalidDataException("Activity history file does not contain an activities list.");
        }

        var activities = export.Activities
            .OrderBy(activity => activity.Timestamp)
            .ToList();

        return new ActivityHistorySummary(
            export.ExportedAt,
            export.FormatVersion,
            activities.Count,
            activities.FirstOrDefault(),
            activities.LastOrDefault(),
            activities.Take(8).ToList());
    }

    public void ExportActivityLogBinary(string filePath)
    {
        var activities = GetActivityItems();

        // Custom .gvactivity format:
        // magic string, version, export timestamp ticks, record count,
        // then one row per ActivityLog: timestamp ticks, action, game title, details.
        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(BinaryMagic);
        writer.Write(FormatVersion);
        writer.Write(DateTime.UtcNow.Ticks);
        writer.Write(activities.Count);

        foreach (var activity in activities)
        {
            writer.Write(activity.Timestamp.ToUniversalTime().Ticks);
            writer.Write(activity.Action);
            writer.Write(activity.GameTitle);
            writer.Write(activity.Details);
        }
    }

    public int ImportActivityLogBinary(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        var header = ReadBinaryHeader(reader);
        var logs = new List<ActivityLog>();
        for (var index = 0; index < header.RecordCount; index++)
        {
            var timestampTicks = reader.ReadInt64();
            var action = reader.ReadString();
            var gameTitle = reader.ReadString();
            _ = reader.ReadString();

            logs.Add(new ActivityLog
            {
                Timestamp = new DateTime(timestampTicks, DateTimeKind.Utc).ToLocalTime(),
                Action = action,
                GameTitle = gameTitle
            });
        }

        return _activityLogRepository.ImportEntries(logs);
    }

    public ActivityArchiveSummary ReadActivityArchiveSummary(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        var header = ReadBinaryHeader(reader);
        DateTime? firstTimestamp = null;
        DateTime? lastTimestamp = null;

        for (var index = 0; index < header.RecordCount; index++)
        {
            var timestamp = new DateTime(reader.ReadInt64(), DateTimeKind.Utc).ToLocalTime();
            _ = reader.ReadString();
            _ = reader.ReadString();
            _ = reader.ReadString();

            firstTimestamp = firstTimestamp is null || timestamp < firstTimestamp
                ? timestamp
                : firstTimestamp;
            lastTimestamp = lastTimestamp is null || timestamp > lastTimestamp
                ? timestamp
                : lastTimestamp;
        }

        return new ActivityArchiveSummary(
            header.FormatVersion,
            header.ExportedAtUtc.ToLocalTime(),
            header.RecordCount,
            firstTimestamp,
            lastTimestamp);
    }

    private static BinaryActivityArchiveHeader ReadBinaryHeader(BinaryReader reader)
    {
        var magic = reader.ReadString();
        if (!string.Equals(magic, BinaryMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException("This is not a GameVault activity archive.");
        }

        var version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported activity archive version: {version}.");
        }

        var exportedAtTicks = reader.ReadInt64();
        var recordCount = reader.ReadInt32();
        if (recordCount < 0)
        {
            throw new InvalidDataException("Activity archive record count is invalid.");
        }

        return new BinaryActivityArchiveHeader(
            version,
            new DateTime(exportedAtTicks, DateTimeKind.Utc),
            recordCount);
    }

    private List<ActivityLogXmlItem> GetActivityItems() =>
        _activityLogRepository.GetAll()
            .OrderBy(log => log.Timestamp)
            .Select(log => new ActivityLogXmlItem
            {
                Timestamp = log.Timestamp,
                Action = log.Action,
                GameTitle = log.GameTitle,
                Details = string.Empty
            })
            .ToList();

    [XmlRoot("GameVaultActivityLogExport")]
    public class ActivityLogXmlExport
    {
        public DateTime ExportedAt { get; set; } = DateTime.Now;
        public string ApplicationName { get; set; } = ActivityLogArchiveService.ApplicationName;
        public int FormatVersion { get; set; } = ActivityLogArchiveService.FormatVersion;

        [XmlArray("Activities")]
        [XmlArrayItem("Activity")]
        public List<ActivityLogXmlItem> Activities { get; set; } = [];
    }

    public class ActivityLogXmlItem
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public record ActivityArchiveSummary(
        int FormatVersion,
        DateTime ExportedAt,
        int RecordCount,
        DateTime? FirstActivityTimestamp,
        DateTime? LastActivityTimestamp);

    public record ActivityHistorySummary(
        DateTime ExportedAt,
        int FormatVersion,
        int RecordCount,
        ActivityLogXmlItem? FirstActivity,
        ActivityLogXmlItem? LastActivity,
        List<ActivityLogXmlItem> Preview);

    private record BinaryActivityArchiveHeader(
        int FormatVersion,
        DateTime ExportedAtUtc,
        int RecordCount);
}
