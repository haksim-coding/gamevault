namespace GameVault.Core.Models;

public class LibraryGameRecord
{
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Developer { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int PlayTimeMinutes { get; set; }
    public DateTime DateAdded { get; set; }
}
