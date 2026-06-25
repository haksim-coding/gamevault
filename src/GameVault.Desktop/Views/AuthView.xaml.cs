using System.Windows;
using System.Windows.Controls;
using GameVault.ViewModels;

namespace GameVault.Views;

public partial class AuthView : UserControl
{
    public AuthView()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuthViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }
}
