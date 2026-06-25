using System.Windows;
using GameVault.Views;

namespace GameVault.Services;

public class PasswordDialogService
{
    public string? AskForPassword(string title)
    {
        var dialog = new PasswordWindow(title)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.Password : null;
    }
}
