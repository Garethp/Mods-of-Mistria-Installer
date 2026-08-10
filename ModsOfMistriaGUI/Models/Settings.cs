using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace Garethp.ModsOfMistriaGUI.Models;

public partial class Settings : ObservableObject
{
    public Settings()
    {
    }

    public Settings(string? mistriaLocation, string? modsLocation)
    {
        MistriaLocation = mistriaLocation ?? "";
        ModsLocation = modsLocation ?? "";
    }

    [ObservableProperty] private string _mistriaLocation = "";

    [ObservableProperty] private string _modsLocation = "";

    [ObservableProperty] private bool _launchGameDirectly;

    [ObservableProperty] private string _uiLanguage = "system";

    private static string PreferencesPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MOMI",
        "settings.json");

    partial void OnLaunchGameDirectlyChanged(bool value)
        => SavePreferences();

    partial void OnUiLanguageChanged(string value)
        => SavePreferences();

    private void SavePreferences()
    {
        try
        {
            var directory = Path.GetDirectoryName(PreferencesPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(new LaunchPreferences(LaunchGameDirectly, UiLanguage)));
        }
        catch
        {
            // A preference must never prevent MOMI from starting or launching the game.
        }
    }

    public void LoadPreferences()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return;
            var preferences = JsonSerializer.Deserialize<LaunchPreferences>(File.ReadAllText(PreferencesPath));
            if (preferences is not null)
            {
                LaunchGameDirectly = preferences.LaunchGameDirectly;
                UiLanguage = string.IsNullOrWhiteSpace(preferences.UiLanguage) ? "system" : preferences.UiLanguage;
            }
        }
        catch
        {
            LaunchGameDirectly = false;
        }
    }

    public static string LoadSavedUiLanguage()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return "system";
            var preferences = JsonSerializer.Deserialize<LaunchPreferences>(File.ReadAllText(PreferencesPath));
            return string.IsNullOrWhiteSpace(preferences?.UiLanguage) ? "system" : preferences.UiLanguage;
        }
        catch { return "system"; }
    }

    private sealed record LaunchPreferences(bool LaunchGameDirectly, string UiLanguage = "system");

    public bool ValidMistriaLocation() => !string.IsNullOrEmpty(MistriaLocation) &&
                                          Directory.Exists(MistriaLocation) &&
                                          (File.Exists(Path.Combine(MistriaLocation, "assets.zip")) ||
                                           Directory.Exists(Path.Combine(MistriaLocation, "assets")));

    public bool ValidModsLocation() => !string.IsNullOrEmpty(ModsLocation) &&
                                       Directory.Exists(ModsLocation);

    public bool WrongMistriaVersion() => !string.IsNullOrEmpty(MistriaLocation) && Directory.Exists(MistriaLocation) &&
                                         (File.Exists(Path.Combine(MistriaLocation, "FieldsOfMistria.exe")) ||
                                          File.Exists(Path.Combine(MistriaLocation, "FieldsOfMistria"))) &&
                                         !File.Exists(Path.Combine(MistriaLocation, "assets.zip"));
}
