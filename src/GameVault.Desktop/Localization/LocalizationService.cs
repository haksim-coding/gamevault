using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace GameVault.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager =
        new("GameVault.Resources.Strings", typeof(LocalizationService).Assembly);
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    private LocalizationService()
    {
    }

    public static LocalizationService Instance { get; } = new();

    public string LanguageCode => _culture.TwoLetterISOLanguageName;

    public string this[string key] => Get(key);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public string Get(string key) =>
        (ResourceManager.GetString(key, _culture) ?? key)
        .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);

    public string GetOrDefault(string key, string fallback) =>
        (ResourceManager.GetString(key, _culture) ?? fallback)
        .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);

    public string Format(string key, params object?[] args) =>
        string.Format(_culture, Get(key), args);

    public void SetLanguage(string? language)
    {
        var culture = string.Equals(language, "HR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "hr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "hr-HR", StringComparison.OrdinalIgnoreCase)
                ? CultureInfo.GetCultureInfo("hr-HR")
                : CultureInfo.GetCultureInfo("en-US");

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (_culture.Name == culture.Name)
        {
            return;
        }

        _culture = culture;
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(LanguageCode));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
