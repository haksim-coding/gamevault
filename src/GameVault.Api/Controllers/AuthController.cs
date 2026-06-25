using GameVault.Api.Data;
using GameVault.Api.Dtos;
using GameVault.Api.Models;
using GameVault.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ServerDbContext dbContext, JwtTokenService jwtTokenService) : ControllerBase
{
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(user => user.Username == normalizedUsername))
        {
            return Conflict(new { message = "Username is already taken." });
        }

        var user = new AppUser
        {
            Username = normalizedUsername,
            DisplayName = request.DisplayName.Trim(),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();

        return Ok(CreateLoginResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await dbContext.AppUsers
            .SingleOrDefaultAsync(candidate => candidate.Username == normalizedUsername);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync();
        }

        return Ok(CreateLoginResponse(user));
    }

    private LoginResponse CreateLoginResponse(AppUser user) =>
        new(jwtTokenService.CreateToken(user), user.Username, user.Role.ToString());
}
