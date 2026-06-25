using System.Windows;
using GameVault.Services;

namespace GameVault;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new IniSettingsService().Load();
        AccentColorService.Apply(settings.AccentColor);
    }
}
