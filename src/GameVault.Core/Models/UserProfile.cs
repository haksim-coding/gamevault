namespace GameVault.Core.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string FavoriteGame { get; set; } = string.Empty;
    public byte[]? AvatarImageBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
