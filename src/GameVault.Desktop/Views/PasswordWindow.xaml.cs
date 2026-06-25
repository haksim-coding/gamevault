using System.Windows;

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
                "Password cannot be empty.",
                "Password",
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
