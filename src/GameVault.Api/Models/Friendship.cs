namespace GameVault.Api.Models;

public class Friendship
{
    public int Id { get; set; }
    public int RequesterUserId { get; set; }
    public AppUser RequesterUser { get; set; } = null!;
    public int ReceiverUserId { get; set; }
    public AppUser ReceiverUser { get; set; } = null!;
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
