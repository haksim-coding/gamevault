using System.IO;

namespace GameVault.Services;

public class IniSettingsService
{
    public IniSettingsService(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GameVault",
            "settings.ini");
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        var settings = new AppSettings();
        if (!File.Exists(SettingsPath))
        {
            Save(settings);
            return settings;
        }

        foreach (var line in File.ReadAllLines(SettingsPath))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (key.Equals("AccentColor", StringComparison.OrdinalIgnoreCase))
            {
                settings.AccentColor = value;
            }
            else if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
            {
                settings.Language = value;
            }
            else if (key.Equals("DefaultInstallFolder", StringComparison.OrdinalIgnoreCase))
            {
                settings.DefaultInstallFolder = value;
            }
            else if (key.Equals("DownloadSpeedLimit", StringComparison.OrdinalIgnoreCase))
            {
                settings.DownloadSpeedLimit = value;
            }
            else if (key.Equals("ApiBaseUrl", StringComparison.OrdinalIgnoreCase))
            {
                settings.ApiBaseUrl = value;
            }
            else if (key.Equals("FilesBaseUrl", StringComparison.OrdinalIgnoreCase))
            {
                settings.FilesBaseUrl = value;
            }
            else if (key.Equals("DownloadManifestUrl", StringComparison.OrdinalIgnoreCase))
            {
                settings.DownloadManifestUrl = value;
            }
        }

        NormalizeServerSettings(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        NormalizeServerSettings(settings);
        var folder = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(folder);

        File.WriteAllLines(SettingsPath,
        [
            "[Ui]",
            $"AccentColor={settings.AccentColor}",
            $"Language={settings.Language}",
            "",
            "[Downloads]",
            $"DefaultInstallFolder={settings.DefaultInstallFolder}",
            $"DownloadSpeedLimit={settings.DownloadSpeedLimit}",
            "",
            "[Server]",
            $"ApiBaseUrl={settings.ApiBaseUrl}",
            $"FilesBaseUrl={settings.FilesBaseUrl}",
            $"DownloadManifestUrl={settings.DownloadManifestUrl}"
        ]);
    }

    private static void NormalizeServerSettings(AppSettings settings)
    {
        settings.ApiBaseUrl = NormalizeHttpUrl(
            settings.ApiBaseUrl,
            AppSettings.DefaultApiBaseUrl);
        settings.FilesBaseUrl = NormalizeHttpUrl(
            settings.FilesBaseUrl,
            AppSettings.DefaultFilesBaseUrl);
        settings.DownloadManifestUrl = NormalizeHttpUrl(
            settings.DownloadManifestUrl,
            AppSettings.DefaultDownloadManifestUrl);
    }

    private static string NormalizeHttpUrl(string value, string fallback)
    {
        var trimmed = value.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? trimmed
            : fallback;
    }
}
