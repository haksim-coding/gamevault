using System.ComponentModel.DataAnnotations;

namespace GameVault.Api.Dtos;

public class FriendRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
}

public record PendingFriendRequestResponse(
    int RequestId,
    string Username,
    string DisplayName,
    DateTime CreatedAt);

public record FriendResponse(
    int FriendshipId,
    string Username,
    string DisplayName,
    int TotalGames,
    int TotalPlayTimeMinutes,
    string FavoriteGenre,
    string LastPlayedGame);
