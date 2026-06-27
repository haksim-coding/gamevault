using System.Globalization;
using System.Windows.Data;
using GameVault.Localization;

namespace GameVault.Converters;

public sealed class LocalizedValueConverter : IValueConverter
{
    private static readonly Dictionary<string, string> ResourceKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Local library"] = "LocalLibrary",
            ["Server catalog"] = "ServerCatalog",
            ["Local"] = "Local",
            ["Not installed"] = "NotInstalled",
            ["Installed"] = "Installed",
            ["Queued"] = "Queued",
            ["Downloading"] = "Downloading",
            ["Installing"] = "Installing",
            ["Already downloading"] = "AlreadyDownloading",
            ["Cancelled"] = "Cancelled",
            ["Failed"] = "Failed",
            ["Cancel"] = "Cancel",
            ["Delete"] = "Delete"
        };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? string.Empty;
        return ResourceKeys.TryGetValue(text, out var key)
            ? LocalizationService.Instance.Get(key)
            : text;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
