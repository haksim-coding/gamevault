using System.IO;
using System.Windows;
using GameVault.Core.Models;
using GameVault.Localization;
using Microsoft.Win32;

namespace GameVault.Views;

public partial class AddEditGameWindow : Window
{
    private readonly Game? _existingGame;

    public AddEditGameWindow(Game? existingGame = null)
    {
        InitializeComponent();
        _existingGame = existingGame;

        if (existingGame is null)
        {
            return;
        }

        Title = LocalizationService.Instance.Get("EditGame");
        HeadingText.Text = LocalizationService.Instance.Get("EditGame");
        TitleTextBox.Text = existingGame.Title;
        GenreTextBox.Text = existingGame.Genre;
        ReleaseYearTextBox.Text = existingGame.ReleaseYear.ToString();
        DeveloperTextBox.Text = existingGame.Developer;
        ExecutablePathTextBox.Text = existingGame.ExecutablePath;
        InstallPathTextBox.Text = existingGame.InstallPath;
        CoverImagePathTextBox.Text = existingGame.CoverImagePath;
    }

    public Game? Game { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            MessageBox.Show(LocalizationService.Instance.Get("TitleRequired"),
                LocalizationService.Instance.Get("Validation"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(ReleaseYearTextBox.Text, out var releaseYear)
            || releaseYear < 1950
            || releaseYear > DateTime.Now.Year + 1)
        {
            MessageBox.Show(LocalizationService.Instance.Get("ValidReleaseYearRequired"),
                LocalizationService.Instance.Get("Validation"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Game = new Game
        {
            Id = _existingGame?.Id ?? 0,
            Title = TitleTextBox.Text.Trim(),
            Genre = GenreTextBox.Text.Trim(),
            ReleaseYear = releaseYear,
            Developer = DeveloperTextBox.Text.Trim(),
            ExecutablePath = ExecutablePathTextBox.Text.Trim(),
            InstallPath = InstallPathTextBox.Text.Trim(),
            CoverImagePath = CoverImagePathTextBox.Text.Trim(),
            PlayTimeMinutes = _existingGame?.PlayTimeMinutes ?? 0,
            DateAdded = _existingGame?.DateAdded ?? DateTime.Now
        };

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BrowseExecutableButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Instance.Get("SelectGameExecutable"),
            Filter = LocalizationService.Instance.Get("ExecutableFilesFilter")
        };

        if (dialog.ShowDialog(this) == true)
        {
            ExecutablePathTextBox.Text = dialog.FileName;

            if (string.IsNullOrWhiteSpace(InstallPathTextBox.Text))
            {
                InstallPathTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }
        }
    }

    private void BrowseInstallPathButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Instance.Get("SelectInstallFolder")
        };

        if (dialog.ShowDialog(this) == true)
        {
            InstallPathTextBox.Text = dialog.FolderName;
        }
    }

    private void BrowseCoverButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Instance.Get("SelectCoverImage"),
            Filter = LocalizationService.Instance.Get("ImageFilesFilter")
        };

        if (dialog.ShowDialog(this) == true)
        {
            CoverImagePathTextBox.Text = dialog.FileName;
        }
    }
}
