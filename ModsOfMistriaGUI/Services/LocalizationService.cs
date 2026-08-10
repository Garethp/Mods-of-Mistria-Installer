using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaInstallerLib.Lang;

namespace Garethp.ModsOfMistriaGUI.Services;

/// <summary>
/// Runtime resource facade. XAML x:Static values are evaluated once, so the
/// UI binds to this indexer instead and receives a notification when the
/// selected culture changes.
/// </summary>
public sealed class LocalizationService : ObservableObject
{
    public static LocalizationService Instance { get; } = new();

    private static readonly CultureInfo SystemCulture = CultureInfo.CurrentUICulture;
    private string _languageCode = "system";

    private LocalizationService()
    {
    }

    public string LanguageCode => _languageCode;

    public event EventHandler? LanguageChanged;

    public string this[string key] => Resources.ResourceManager.GetString(key, Resources.Culture)
                                      ?? Resources.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
                                      ?? key;

    public void SetLanguage(string? languageCode)
    {
        var normalized = string.IsNullOrWhiteSpace(languageCode) ? "system" : languageCode.Trim().ToLowerInvariant();
        if (normalized is not ("system" or "en" or "bg" or "de" or "fr" or "nl" or "pt-br" or "ru" or "id" or "zh-hans" or "zh-hant" or "ko" or "ja" or "es" or "uk"))
            normalized = "system";

        var culture = normalized switch
        {
            "en" => CultureInfo.GetCultureInfo("en"),
            "bg" => CultureInfo.GetCultureInfo("bg"),
            "de" => CultureInfo.GetCultureInfo("de"),
            "fr" => CultureInfo.GetCultureInfo("fr"),
            "nl" => CultureInfo.GetCultureInfo("nl"),
            "pt-br" => CultureInfo.GetCultureInfo("pt-BR"),
            "ru" => CultureInfo.GetCultureInfo("ru"),
            "id" => CultureInfo.GetCultureInfo("id"),
            "zh-hans" => CultureInfo.GetCultureInfo("zh-Hans"),
            "zh-hant" => CultureInfo.GetCultureInfo("zh-Hant"),
            "ko" => CultureInfo.GetCultureInfo("ko"),
            "ja" => CultureInfo.GetCultureInfo("ja"),
            "es" => CultureInfo.GetCultureInfo("es"),
            "uk" => CultureInfo.GetCultureInfo("uk"),
            _ => SystemCulture
        };

        _languageCode = normalized;
        Resources.Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        OnPropertyChanged(nameof(LanguageCode));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
