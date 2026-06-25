using System.ComponentModel.DataAnnotations;

namespace GameVault.Api.Dtos;

public class UpdateStatsRequest
{
    [Range(0, int.MaxValue)]
    public int TotalGames { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalPlayTimeMinutes { get; set; }

    [StringLength(100)]
    public string FavoriteGenre { get; set; } = string.Empty;

    [StringLength(150)]
    public string LastPlayedGame { get; set; } = string.Empty;
}

public record PublicStatsResponse(
    string Username,
    int TotalGames,
    int TotalPlayTimeMinutes,
    string FavoriteGenre,
    string LastPlayedGame,
    DateTime UpdatedAt);
