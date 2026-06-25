namespace GameVault.Api.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string FavoriteGame { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PublicGameStats? PublicGameStats { get; set; }
}
