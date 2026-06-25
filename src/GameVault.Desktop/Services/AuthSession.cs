using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameVault.Services;

public class AuthSession : INotifyPropertyChanged
{
    private string _token = string.Empty;
    private string _username = string.Empty;
    private string _role = string.Empty;

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public string Token
    {
        get => _token;
        private set
        {
            if (_token == value) return;
            _token = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLoggedIn));
        }
    }

    public string Username
    {
        get => _username;
        private set
        {
            if (_username == value) return;
            _username = value;
            OnPropertyChanged();
        }
    }

    public string Role
    {
        get => _role;
        private set
        {
            if (_role == value) return;
            _role = value;
            OnPropertyChanged();
        }
    }

    public void Start(string token, string username, string role)
    {
        Token = token;
        Username = username;
        Role = role;
    }

    public void Clear()
    {
        Token = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
