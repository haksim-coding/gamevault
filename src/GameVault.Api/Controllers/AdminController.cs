using GameVault.Api.Data;
using GameVault.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(ServerDbContext dbContext) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<ProfileResponse>>> GetUsers()
    {
        var users = await dbContext.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new ProfileResponse(
                user.Id,
                user.Username,
                user.Role.ToString(),
                user.DisplayName,
                user.Bio,
                user.FavoriteGame,
                user.CreatedAt))
            .ToListAsync();

        return Ok(users);
    }
}
