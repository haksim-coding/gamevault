using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class UserProfileRepository
{
    public UserProfileRepository()
    {
        DatabaseInitializer.Initialize();
    }

    public UserProfile GetProfile()
    {
        using var context = new AppDbContext();
        return context.UserProfiles.AsNoTracking().First();
    }

    public void UpdateProfile(UserProfile profile)
    {
        using var context = new AppDbContext();
        context.UserProfiles.Update(profile);
        context.SaveChanges();
    }
}
