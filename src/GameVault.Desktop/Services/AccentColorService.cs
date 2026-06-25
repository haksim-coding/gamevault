using System.Windows;
using System.Windows.Media;

namespace GameVault.Services;

public static class AccentColorService
{
    private static readonly Dictionary<string, Color> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Purple"] = (Color)ColorConverter.ConvertFromString("#7C5CFC"),
        ["Blue"] = (Color)ColorConverter.ConvertFromString("#3B82F6"),
        ["Green"] = (Color)ColorConverter.ConvertFromString("#22A06B"),
        ["Orange"] = (Color)ColorConverter.ConvertFromString("#E8793E")
    };

    public static void Apply(string accentColor)
    {
        if (!Colors.TryGetValue(accentColor, out var color))
        {
            color = Colors["Purple"];
        }

        Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(color);
    }
}
