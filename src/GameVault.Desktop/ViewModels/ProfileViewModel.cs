using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameVault.Core.Models;
using GameVault.Data;
using Microsoft.Win32;

namespace GameVault.ViewModels;

public class ProfileViewModel : ViewModelBase
{
    private readonly UserProfileRepository _profileRepository;
    private readonly AchievementRepository _achievementRepository;
    private UserProfile _profile;
    private string _username = string.Empty;
    private string _displayName = string.Empty;
    private string _bio = string.Empty;
    private string _favoriteGame = string.Empty;
    private byte[]? _avatarImageBytes;
    private ImageSource? _avatarImage;
    private string _statusMessage = string.Empty;

    public ProfileViewModel(
        UserProfileRepository profileRepository,
        AchievementRepository achievementRepository)
    {
        _profileRepository = profileRepository;
        _achievementRepository = achievementRepository;
        _profile = _profileRepository.GetProfile();

        SaveProfileCommand = new RelayCommand(SaveProfile);
        UploadAvatarCommand = new RelayCommand(UploadAvatar);

        Refresh();
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Bio
    {
        get => _bio;
        set => SetProperty(ref _bio, value);
    }

    public string FavoriteGame
    {
        get => _favoriteGame;
        set => SetProperty(ref _favoriteGame, value);
    }

    public ImageSource? AvatarImage
    {
        get => _avatarImage;
        private set => SetProperty(ref _avatarImage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<AchievementItemViewModel> Achievements { get; } = [];
    public ICommand SaveProfileCommand { get; }
    public ICommand UploadAvatarCommand { get; }

    public void Refresh()
    {
        _profile = _profileRepository.GetProfile();
        _achievementRepository.EvaluateForUser(_profile.Id);

        Username = _profile.Username;
        DisplayName = _profile.DisplayName;
        Bio = _profile.Bio;
        FavoriteGame = _profile.FavoriteGame;
        _avatarImageBytes = _profile.AvatarImageBytes;
        AvatarImage = CreateImage(_avatarImageBytes);
        LoadAchievements();
    }

    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(DisplayName))
        {
            StatusMessage = "Username and display name are required.";
            return;
        }

        _profile.Username = Username.Trim();
        _profile.DisplayName = DisplayName.Trim();
        _profile.Bio = Bio.Trim();
        _profile.FavoriteGame = FavoriteGame.Trim();
        _profile.AvatarImageBytes = _avatarImageBytes;

        _profileRepository.UpdateProfile(_profile);
        StatusMessage = "Profile saved.";
    }

    private void UploadAvatar()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select avatar image",
            Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _avatarImageBytes = File.ReadAllBytes(dialog.FileName);
            AvatarImage = CreateImage(_avatarImageBytes);
            StatusMessage = "Avatar selected. Click Save Profile to store it in SQLite.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Avatar could not be loaded: {exception.Message}";
        }
    }

    private void LoadAchievements()
    {
        var unlocked = _achievementRepository.GetUnlockedForUser(_profile.Id);

        Achievements.Clear();
        foreach (var achievement in _achievementRepository.GetAll())
        {
            var isUnlocked = unlocked.TryGetValue(achievement.Id, out var unlockedAt);
            Achievements.Add(new AchievementItemViewModel
            {
                Code = achievement.Code,
                Title = achievement.Title,
                Description = achievement.Description,
                IsUnlocked = isUnlocked,
                UnlockedAt = isUnlocked ? unlockedAt : null
            });
        }
    }

    private static ImageSource? CreateImage(byte[]? imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(imageBytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
