namespace GameVault.Services;

public record LoginResponse(string Token, string Username, string Role);

public record ProfileResponse(
    int Id,
    string Username,
    string Role,
    string DisplayName,
    string Bio,
    string FavoriteGame,
    DateTime CreatedAt);

public record PendingFriendRequest(
    int RequestId,
    string Username,
    string DisplayName,
    DateTime CreatedAt);

public record FriendInfo(
    int FriendshipId,
    string Username,
    string DisplayName,
    int TotalGames,
    int TotalPlayTimeMinutes,
    string FavoriteGenre,
    string LastPlayedGame)
{
    public string PlayTimeText => TotalPlayTimeMinutes < 60
        ? $"{TotalPlayTimeMinutes} min"
        : $"{TotalPlayTimeMinutes / 60} h {TotalPlayTimeMinutes % 60} min";
}

public record PublicStats(
    string Username,
    int TotalGames,
    int TotalPlayTimeMinutes,
    string FavoriteGenre,
    string LastPlayedGame,
    DateTime UpdatedAt);

public record UpdateStatsRequest(
    int TotalGames,
    int TotalPlayTimeMinutes,
    string FavoriteGenre,
    string LastPlayedGame);
