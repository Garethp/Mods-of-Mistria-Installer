using CommunityToolkit.Mvvm.ComponentModel;

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