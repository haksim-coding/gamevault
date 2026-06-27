using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameVault.Core.Models;
using GameVault.Data;
using GameVault.Localization;
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
            StatusMessage = LocalizationService.Instance.Get("ProfileFieldsRequired");
            return;
        }

        _profile.Username = Username.Trim();
        _profile.DisplayName = DisplayName.Trim();
        _profile.Bio = Bio.Trim();
        _profile.FavoriteGame = FavoriteGame.Trim();
        _profile.AvatarImageBytes = _avatarImageBytes;

        _profileRepository.UpdateProfile(_profile);
        StatusMessage = LocalizationService.Instance.Get("ProfileSaved");
    }

    private void UploadAvatar()
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Instance.Get("SelectAvatarImage"),
            Filter = LocalizationService.Instance.Get("AvatarImageFilesFilter")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _avatarImageBytes = File.ReadAllBytes(dialog.FileName);
            AvatarImage = CreateImage(_avatarImageBytes);
            StatusMessage = LocalizationService.Instance.Get("AvatarSelected");
        }
        catch (Exception exception)
        {
            StatusMessage = LocalizationService.Instance.Format("AvatarLoadFailed", exception.Message);
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
                Title = LocalizationService.Instance.GetOrDefault(
                    $"Achievement_{achievement.Code}_Title", achievement.Title),
                Description = LocalizationService.Instance.GetOrDefault(
                    $"Achievement_{achievement.Code}_Description", achievement.Description),
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
