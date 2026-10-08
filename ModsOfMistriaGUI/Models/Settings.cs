using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaInstallerLib;
using Newtonsoft.Json;

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
    
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ModsOfMistria", "settings.json");

    public static Settings Load()
    {
        try { return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(FilePath)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this));
        }
        catch (Exception e)
        {
            Logger.Log(e.Message);
        }
    }
}