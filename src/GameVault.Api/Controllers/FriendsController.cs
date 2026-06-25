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
[Route("api/friends")]
public class FriendsController(ServerDbContext dbContext) : ControllerBase
{
    [HttpPost("request")]
    public async Task<ActionResult> SendRequest(FriendRequest request)
    {
        var currentUserId = User.GetUserId();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var receiver = await dbContext.AppUsers
            .SingleOrDefaultAsync(user => user.Username == normalizedUsername);

        if (receiver is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        if (receiver.Id == currentUserId)
        {
            return BadRequest(new { message = "You cannot send a friend request to yourself." });
        }

        var existing = await dbContext.Friendships.SingleOrDefaultAsync(friendship =>
            (friendship.RequesterUserId == currentUserId && friendship.ReceiverUserId == receiver.Id) ||
            (friendship.RequesterUserId == receiver.Id && friendship.ReceiverUserId == currentUserId));

        if (existing is not null)
        {
            return Conflict(new
            {
                message = "A friendship or friend request already exists between these users.",
                friendshipId = existing.Id,
                status = existing.Status.ToString()
            });
        }

        var friendship = new Friendship
        {
            RequesterUserId = currentUserId,
            ReceiverUserId = receiver.Id,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Friendships.Add(friendship);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRequests), new { }, new
        {
            friendship.Id,
            receiver.Username,
            status = friendship.Status.ToString(),
            friendship.CreatedAt
        });
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<PendingFriendRequestResponse>>> GetRequests()
    {
        var currentUserId = User.GetUserId();
        var requests = await dbContext.Friendships
            .AsNoTracking()
            .Where(friendship =>
                friendship.ReceiverUserId == currentUserId &&
                friendship.Status == FriendshipStatus.Pending)
            .OrderBy(friendship => friendship.CreatedAt)
            .Select(friendship => new PendingFriendRequestResponse(
                friendship.Id,
                friendship.RequesterUser.Username,
                friendship.RequesterUser.DisplayName,
                friendship.CreatedAt))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("accept/{requestId:int}")]
    public async Task<ActionResult> AcceptRequest(int requestId)
    {
        var currentUserId = User.GetUserId();
        var friendship = await dbContext.Friendships.SingleOrDefaultAsync(candidate =>
            candidate.Id == requestId &&
            candidate.ReceiverUserId == currentUserId &&
            candidate.Status == FriendshipStatus.Pending);

        if (friendship is null)
        {
            return NotFound(new { message = "Pending friend request was not found." });
        }

        friendship.Status = FriendshipStatus.Accepted;
        await dbContext.SaveChangesAsync();

        return Ok(new { friendship.Id, status = friendship.Status.ToString() });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendResponse>>> GetFriends()
    {
        var currentUserId = User.GetUserId();
        var friendships = await dbContext.Friendships
            .AsNoTracking()
            .Where(friendship =>
                friendship.Status == FriendshipStatus.Accepted &&
                (friendship.RequesterUserId == currentUserId ||
                 friendship.ReceiverUserId == currentUserId))
            .Include(friendship => friendship.RequesterUser)
                .ThenInclude(user => user.PublicGameStats)
            .Include(friendship => friendship.ReceiverUser)
                .ThenInclude(user => user.PublicGameStats)
            .OrderBy(friendship => friendship.Id)
            .ToListAsync();

        var friends = friendships.Select(friendship =>
        {
            var friend = friendship.RequesterUserId == currentUserId
                ? friendship.ReceiverUser
                : friendship.RequesterUser;
            var stats = friend.PublicGameStats;

            return new FriendResponse(
                friendship.Id,
                friend.Username,
                friend.DisplayName,
                stats?.TotalGames ?? 0,
                stats?.TotalPlayTimeMinutes ?? 0,
                stats?.FavoriteGenre ?? string.Empty,
                stats?.LastPlayedGame ?? string.Empty);
        });

        return Ok(friends);
    }

    [HttpDelete("{friendshipId:int}")]
    public async Task<ActionResult> DeleteFriendship(int friendshipId)
    {
        var currentUserId = User.GetUserId();
        var friendship = await dbContext.Friendships.SingleOrDefaultAsync(candidate =>
            candidate.Id == friendshipId &&
            (candidate.RequesterUserId == currentUserId ||
             candidate.ReceiverUserId == currentUserId));

        if (friendship is null)
        {
            return NotFound(new { message = "Friendship was not found." });
        }

        dbContext.Friendships.Remove(friendship);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
