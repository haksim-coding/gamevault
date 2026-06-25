using System.ComponentModel.DataAnnotations;

namespace GameVault.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, string Username, string Role);
