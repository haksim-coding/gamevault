using System.Windows;
using System.Windows.Input;
using GameVault.Localization;
using GameVault.Services;

namespace GameVault.ViewModels;

public class AuthViewModel : ViewModelBase
{
    private readonly GameVaultApiClient _apiClient;
    private readonly AuthSession _session;
    private readonly PublicStatsSyncService _statsSyncService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _displayName = string.Empty;
    private string _statusMessage = string.Empty;

    public AuthViewModel(
        GameVaultApiClient apiClient,
        AuthSession session,
        PublicStatsSyncService statsSyncService)
    {
        _apiClient = apiClient;
        _session = session;
        _statsSyncService = statsSyncService;
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        LogoutCommand = new RelayCommand(Logout);
        _session.PropertyChanged += (_, _) => RefreshSessionProperties();
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoggedIn => _session.IsLoggedIn;
    public Visibility LoginFormVisibility => IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
    public Visibility LoggedInVisibility => IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
    public string SessionUsername => _session.Username;
    public string SessionRole => _session.Role;

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand LogoutCommand { get; }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = LocalizationService.Instance.Get("EnterUsernamePassword");
            return;
        }

        await RunAuthAsync(
            () => _apiClient.LoginAsync(Username, Password),
            LocalizationService.Instance.Get("LoginSuccessful"));
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(DisplayName))
        {
            StatusMessage = LocalizationService.Instance.Get("EnterRegistrationFields");
            return;
        }

        await RunAuthAsync(
            () => _apiClient.RegisterAsync(Username, Password, DisplayName),
            LocalizationService.Instance.Get("RegistrationSuccessful"));
    }

    private async Task RunAuthAsync(
        Func<Task<LoginResponse>> action,
        string successMessage)
    {
        try
        {
            StatusMessage = LocalizationService.Instance.Get("Connecting");
            var result = await action();
            _session.Start(result.Token, result.Username, result.Role);
            Password = string.Empty;
            StatusMessage = successMessage;
            _ = _statsSyncService.TrySyncAsync();
        }
        catch (ApiException exception)
        {
            StatusMessage = ApiErrorLocalizer.Get(exception.Message);
        }
        catch (Exception)
        {
            StatusMessage = LocalizationService.Instance.Get("LoginFailed");
        }
    }

    private void Logout()
    {
        _session.Clear();
        Password = string.Empty;
        StatusMessage = LocalizationService.Instance.Get("LoggedOut");
    }

    private void RefreshSessionProperties()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(LoginFormVisibility));
        OnPropertyChanged(nameof(LoggedInVisibility));
        OnPropertyChanged(nameof(SessionUsername));
        OnPropertyChanged(nameof(SessionRole));
    }
}
