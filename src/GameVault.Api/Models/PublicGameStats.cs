namespace GameVault.Api.Models;

public class PublicGameStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int TotalGames { get; set; }
    public int TotalPlayTimeMinutes { get; set; }
    public string FavoriteGenre { get; set; } = string.Empty;
    public string LastPlayedGame { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
