using System.ComponentModel.DataAnnotations;

namespace GameVault.Api.Dtos;

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;

    [StringLength(150)]
    public string FavoriteGame { get; set; } = string.Empty;
}

public record ProfileResponse(
    int Id,
    string Username,
    string Role,
    string DisplayName,
    string Bio,
    string FavoriteGame,
    DateTime CreatedAt);
