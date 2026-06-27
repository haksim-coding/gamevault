using System.ComponentModel;

namespace GameVault.Localization;

public sealed class LocalizedOption : INotifyPropertyChanged
{
    public LocalizedOption(string value, string resourceKey)
    {
        Value = value;
        ResourceKey = resourceKey;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    public string Value { get; }
    public string ResourceKey { get; }
    public string DisplayName => LocalizationService.Instance.Get(ResourceKey);

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => DisplayName;

    private void OnLanguageChanged(object? sender, EventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
}
