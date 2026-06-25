using System.ComponentModel.DataAnnotations.Schema;

namespace GameVault.Core.Models;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Developer { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public int PlayTimeMinutes { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.Now;
    public string CatalogId { get; set; } = string.Empty;
    public string InstallStatus { get; set; } = "Installed";
    public int DownloadProgress { get; set; }
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;

    [NotMapped]
    public long DownloadSpeedBytesPerSecond { get; set; }

    [NotMapped]
    public bool CanPlay =>
        string.IsNullOrWhiteSpace(CatalogId)
        || InstallStatus.Equals("Installed", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public string SourceLabel =>
        string.IsNullOrWhiteSpace(CatalogId)
            ? "Local library"
            : "Server catalog";

    [NotMapped]
    public string InstallStatusLabel =>
        string.IsNullOrWhiteSpace(CatalogId)
            ? "Local"
            : InstallStatus;

    [NotMapped]
    public bool IsDownloadInProgress =>
        !string.IsNullOrWhiteSpace(CatalogId)
        && InstallStatus is "Queued" or "Downloading" or "Installing";

    [NotMapped]
    public string DownloadSpeedLabel =>
        IsDownloadInProgress && DownloadSpeedBytesPerSecond > 0
            ? $"{FormatBytes(DownloadSpeedBytesPerSecond)}/s"
            : string.Empty;

    [NotMapped]
    public string DeleteOrCancelButtonText =>
        IsDownloadInProgress ? "Cancel" : "Delete";

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)Math.Max(0, bytes);
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }
}
