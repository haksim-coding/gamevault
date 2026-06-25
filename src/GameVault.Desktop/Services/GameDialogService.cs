using System.Windows;
using GameVault.Core.Models;
using GameVault.Views;

namespace GameVault.Services;

public class GameDialogService
{
    public Game? ShowGameDialog(Game? existingGame = null)
    {
        var dialog = new AddEditGameWindow(existingGame)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.Game : null;
    }

    public bool ConfirmDelete(string title)
    {
        var result = MessageBox.Show(
            $"Delete \"{title}\" from your library?",
            "Delete game",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(
            message,
            "Unable to launch game",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
