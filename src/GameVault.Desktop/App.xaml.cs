using System.Windows;
using GameVault.Localization;
using GameVault.Services;

namespace GameVault;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var settings = new IniSettingsService().Load();
        LocalizationService.Instance.SetLanguage(settings.Language);
        AccentColorService.Apply(settings.AccentColor);
        base.OnStartup(e);
    }
}
