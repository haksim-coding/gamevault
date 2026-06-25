namespace GameVault.Services;

public class DownloadState
{
    public string CatalogGameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Not installed";
    public int ProgressPercent { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public long SpeedBytesPerSecond { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsIndeterminate { get; set; }
}
