using GameVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class ActivityLogRepository
{
    public ActivityLogRepository()
    {
        DatabaseInitializer.Initialize();
    }

    public void AddLaunchedEntry(string gameTitle)
    {
        AddEntry("Launched", gameTitle);
    }

    public void AddEntry(string action, string gameTitle = "")
    {
        using var context = new AppDbContext();
        context.ActivityLogs.Add(new ActivityLog
        {
            GameTitle = gameTitle,
            Timestamp = DateTime.Now,
            Action = action
        });
        context.SaveChanges();
    }

    public List<ActivityLog> GetAll()
    {
        using var context = new AppDbContext();
        return context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .ToList();
    }

    public List<ActivityLog> GetRecent(int count)
    {
        using var context = new AppDbContext();
        return context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .Take(count)
            .ToList();
    }

    public int ImportEntries(IEnumerable<ActivityLog> logs)
    {
        using var context = new AppDbContext();
        var existingKeys = context.ActivityLogs
            .AsNoTracking()
            .Select(log => new { log.Timestamp, log.Action, log.GameTitle })
            .ToList()
            .Select(log => CreateDuplicateKey(log.Timestamp, log.Action, log.GameTitle))
            .ToHashSet();

        var importedCount = 0;
        foreach (var log in logs)
        {
            if (string.IsNullOrWhiteSpace(log.Action))
            {
                continue;
            }

            log.Action = log.Action.Trim();
            log.GameTitle = log.GameTitle.Trim();
            if (log.Timestamp == default)
            {
                log.Timestamp = DateTime.Now;
            }

            var key = CreateDuplicateKey(log.Timestamp, log.Action, log.GameTitle);
            if (existingKeys.Contains(key))
            {
                continue;
            }

            context.ActivityLogs.Add(log);
            existingKeys.Add(key);
            importedCount++;
        }

        context.SaveChanges();
        return importedCount;
    }

    private static string CreateDuplicateKey(ActivityLog log) =>
        CreateDuplicateKey(log.Timestamp, log.Action, log.GameTitle);

    private static string CreateDuplicateKey(DateTime timestamp, string action, string gameTitle) =>
        $"{timestamp.Ticks}|{action.Trim().ToUpperInvariant()}|{gameTitle.Trim().ToUpperInvariant()}";
}
