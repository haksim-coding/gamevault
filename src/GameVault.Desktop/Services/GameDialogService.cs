using System.Windows;
using GameVault.Core.Models;
using GameVault.Localization;
using GameVault.Views;

namespace GameVault.Services;

public enum CatalogDeleteChoice
{
    Cancel,
    LibraryOnly,
    DeleteFilesToo
}

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
            LocalizationService.Instance.Format("ConfirmDeleteGame", title),
            LocalizationService.Instance.Get("DeleteGame"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    public CatalogDeleteChoice ConfirmCatalogDelete(string title)
    {
        var result = MessageBox.Show(
            LocalizationService.Instance.Format("ConfirmCatalogDelete", title),
            LocalizationService.Instance.Get("DeleteGame"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        return result switch
        {
            MessageBoxResult.Yes => CatalogDeleteChoice.DeleteFilesToo,
            MessageBoxResult.No => CatalogDeleteChoice.LibraryOnly,
            _ => CatalogDeleteChoice.Cancel
        };
    }

    public bool ConfirmLibraryOnlyAfterUnsafePath(string reason)
    {
        var result = MessageBox.Show(
            LocalizationService.Instance.Format("UnsafeDeletePrompt", reason),
            LocalizationService.Instance.Get("FilesNotDeleted"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    public void ShowDeleteError(string message)
    {
        MessageBox.Show(
            message,
            LocalizationService.Instance.Get("UnableDeleteFiles"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public void ShowError(string message)
    {
        MessageBox.Show(
            message,
            LocalizationService.Instance.Get("UnableLaunchGame"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
