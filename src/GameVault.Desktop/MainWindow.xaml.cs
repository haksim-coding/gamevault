using System.Windows;
using GameVault.ViewModels;

namespace GameVault;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
