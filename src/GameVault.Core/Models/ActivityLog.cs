namespace GameVault.Core.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Action { get; set; } = string.Empty;
}
