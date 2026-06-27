using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Input;
using GameVault.Localization;
using GameVault.Services;

namespace GameVault.ViewModels;

public class FriendsViewModel : ViewModelBase
{
    private readonly GameVaultApiClient _apiClient;
    private readonly AuthSession _session;
    private readonly PublicStatsSyncService _statsSyncService;
    private string _friendUsername = string.Empty;
    private string _statusMessage = string.Empty;

    public FriendsViewModel(
        GameVaultApiClient apiClient,
        AuthSession session,
        PublicStatsSyncService statsSyncService)
    {
        _apiClient = apiClient;
        _session = session;
        _statsSyncService = statsSyncService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SendRequestCommand = new AsyncRelayCommand(SendRequestAsync);
        AcceptRequestCommand = new AsyncRelayCommand(AcceptRequestAsync);
        RemoveFriendCommand = new AsyncRelayCommand(RemoveFriendAsync);

        _session.PropertyChanged += (_, _) => SessionChanged();
        _statsSyncService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(PublicStatsSyncService.StatusText))
            {
                OnPropertyChanged(nameof(SyncStatus));
            }
        };
    }

    public ObservableCollection<PendingFriendRequest> PendingRequests { get; } = [];
    public ObservableCollection<FriendInfo> Friends { get; } = [];

    public string FriendUsername
    {
        get => _friendUsername;
        set => SetProperty(ref _friendUsername, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoggedIn => _session.IsLoggedIn;
    public Visibility LoggedInVisibility => IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LoggedOutVisibility => IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
    public string SyncStatus => _statsSyncService.StatusText;

    public ICommand RefreshCommand { get; }
    public ICommand SendRequestCommand { get; }
    public ICommand AcceptRequestCommand { get; }
    public ICommand RemoveFriendCommand { get; }

    public async Task RefreshAsync()
    {
        if (!IsLoggedIn)
        {
            PendingRequests.Clear();
            Friends.Clear();
            return;
        }

        try
        {
            StatusMessage = LocalizationService.Instance.Get("Refreshing");
            var requestsTask = _apiClient.GetFriendRequestsAsync();
            var friendsTask = _apiClient.GetFriendsAsync();
            await Task.WhenAll(requestsTask, friendsTask);

            ReplaceItems(PendingRequests, await requestsTask);
            ReplaceItems(Friends, await friendsTask);
            StatusMessage = LocalizationService.Instance.Get("FriendsUpdated");
        }
        catch (ApiException exception)
        {
            HandleApiError(exception);
        }
        catch (Exception)
        {
            StatusMessage = LocalizationService.Instance.Get("FriendsLoadFailed");
        }
    }

    private async Task SendRequestAsync()
    {
        if (string.IsNullOrWhiteSpace(FriendUsername))
        {
            StatusMessage = LocalizationService.Instance.Get("EnterUsername");
            return;
        }

        await RunActionAsync(
            () => _apiClient.SendFriendRequestAsync(FriendUsername),
            LocalizationService.Instance.Get("FriendRequestSent"));
        FriendUsername = string.Empty;
    }

    private async Task AcceptRequestAsync(object? parameter)
    {
        if (parameter is not PendingFriendRequest request) return;

        await RunActionAsync(
            () => _apiClient.AcceptFriendRequestAsync(request.RequestId),
            LocalizationService.Instance.Format("FriendAccepted", request.Username));
        await RefreshAsync();
    }

    private async Task RemoveFriendAsync(object? parameter)
    {
        if (parameter is not FriendInfo friend) return;

        await RunActionAsync(
            () => _apiClient.RemoveFriendAsync(friend.FriendshipId),
            LocalizationService.Instance.Format("FriendRemoved", friend.Username));
        await RefreshAsync();
    }

    private async Task RunActionAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            StatusMessage = successMessage;
        }
        catch (ApiException exception)
        {
            HandleApiError(exception);
        }
        catch (Exception)
        {
            StatusMessage = LocalizationService.Instance.Get("OperationCouldNotComplete");
        }
    }

    private void HandleApiError(ApiException exception)
    {
        StatusMessage = ApiErrorLocalizer.Get(exception.Message);
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            SessionChanged();
        }
    }

    private void SessionChanged()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(LoggedInVisibility));
        OnPropertyChanged(nameof(LoggedOutVisibility));
        if (!IsLoggedIn)
        {
            PendingRequests.Clear();
            Friends.Clear();
        }
    }

    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
