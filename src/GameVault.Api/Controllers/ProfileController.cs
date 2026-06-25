using GameVault.Api.Data;
using GameVault.Api.Dtos;
using GameVault.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(ServerDbContext dbContext) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> GetMyProfile()
    {
        var user = await dbContext.AppUsers.FindAsync(User.GetUserId());
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileResponse>> UpdateMyProfile(UpdateProfileRequest request)
    {
        var user = await dbContext.AppUsers.FindAsync(User.GetUserId());
        if (user is null)
        {
            return NotFound();
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Bio = request.Bio.Trim();
        user.FavoriteGame = request.FavoriteGame.Trim();
        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    private static ProfileResponse ToResponse(Models.AppUser user) =>
        new(
            user.Id,
            user.Username,
            user.Role.ToString(),
            user.DisplayName,
            user.Bio,
            user.FavoriteGame,
            user.CreatedAt);
}
