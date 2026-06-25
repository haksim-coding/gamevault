using System.Collections.ObjectModel;
using GameVault.Core.Models;
using GameVault.Data;

namespace GameVault.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly ActivityLogRepository _activityLogRepository;

    public HomeViewModel(List<Game> games, ActivityLogRepository activityLogRepository)
    {
        _activityLogRepository = activityLogRepository;
        LoadGames(games);
        LoadRecentActivity();
    }

    public List<Game> Games { get; private set; } = [];
    public ObservableCollection<ActivityItemViewModel> RecentActivities { get; } = [];
    public int GameCount => Games.Count;
    public int TotalHours => Games.Sum(game => game.PlayTimeMinutes) / 60;
    public Game? FeaturedGame => Games.FirstOrDefault();
    public bool HasRecentActivity => RecentActivities.Count > 0;

    public void Refresh(List<Game> games)
    {
        LoadGames(games);
        LoadRecentActivity();
    }

    public void LoadGames(List<Game> games)
    {
        Games = games;
        OnPropertyChanged(nameof(Games));
        OnPropertyChanged(nameof(GameCount));
        OnPropertyChanged(nameof(TotalHours));
        OnPropertyChanged(nameof(FeaturedGame));
    }

    public void LoadRecentActivity()
    {
        RecentActivities.Clear();
        foreach (var log in _activityLogRepository.GetRecent(8))
        {
            RecentActivities.Add(new ActivityItemViewModel(log));
        }

        OnPropertyChanged(nameof(HasRecentActivity));
    }
}
