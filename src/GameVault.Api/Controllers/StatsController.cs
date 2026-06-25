using GameVault.Api.Data;
using GameVault.Api.Dtos;
using GameVault.Api.Extensions;
using GameVault.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stats")]
public class StatsController(ServerDbContext dbContext) : ControllerBase
{
    [HttpPost("me")]
    public async Task<ActionResult<PublicStatsResponse>> UpdateMyStats(UpdateStatsRequest request)
    {
        var userId = User.GetUserId();
        var user = await dbContext.AppUsers.FindAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var stats = await dbContext.PublicGameStats
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId);

        if (stats is null)
        {
            stats = new PublicGameStats { UserId = userId };
            dbContext.PublicGameStats.Add(stats);
        }

        stats.TotalGames = request.TotalGames;
        stats.TotalPlayTimeMinutes = request.TotalPlayTimeMinutes;
        stats.FavoriteGenre = request.FavoriteGenre.Trim();
        stats.LastPlayedGame = request.LastPlayedGame.Trim();
        stats.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(user.Username, stats));
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<PublicStatsResponse>> GetStats(string username)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var result = await dbContext.AppUsers
            .AsNoTracking()
            .Where(user => user.Username == normalizedUsername)
            .Select(user => new
            {
                user.Username,
                Stats = user.PublicGameStats
            })
            .SingleOrDefaultAsync();

        if (result is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        if (result.Stats is null)
        {
            return NotFound(new { message = "This user has not published game statistics yet." });
        }

        return Ok(ToResponse(result.Username, result.Stats));
    }

    private static PublicStatsResponse ToResponse(string username, PublicGameStats stats) =>
        new(
            username,
            stats.TotalGames,
            stats.TotalPlayTimeMinutes,
            stats.FavoriteGenre,
            stats.LastPlayedGame,
            stats.UpdatedAt);
}
