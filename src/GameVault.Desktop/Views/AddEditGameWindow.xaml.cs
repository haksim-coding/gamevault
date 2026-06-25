using System.IO;
using System.Windows;
using GameVault.Core.Models;
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

        Title = "Edit Game";
        HeadingText.Text = "Edit Game";
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
            MessageBox.Show("Title is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(ReleaseYearTextBox.Text, out var releaseYear)
            || releaseYear < 1950
            || releaseYear > DateTime.Now.Year + 1)
        {
            MessageBox.Show("Enter a valid release year.", "Validation",
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
            Title = "Select game executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
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
            Title = "Select game install folder"
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
            Title = "Select cover image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            CoverImagePathTextBox.Text = dialog.FileName;
        }
    }
}
