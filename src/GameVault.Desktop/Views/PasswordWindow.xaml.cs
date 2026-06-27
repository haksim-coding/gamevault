using System.Windows;
using GameVault.Localization;

namespace GameVault.Views;

public partial class PasswordWindow : Window
{
    public PasswordWindow(string title)
    {
        InitializeComponent();
        Title = title;
        HeadingText.Text = title;
        PasswordInput.Focus();
    }

    public string Password => PasswordInput.Password;

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordInput.Password))
        {
            MessageBox.Show(
                LocalizationService.Instance.Get("PasswordRequired"),
                LocalizationService.Instance.Get("Password"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
