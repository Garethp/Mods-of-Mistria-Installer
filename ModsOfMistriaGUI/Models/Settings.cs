using CommunityToolkit.Mvvm.ComponentModel;

namespace Garethp.ModsOfMistriaGUI.Models;

public partial class Settings: ObservableObject
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
                                          File.Exists(Path.Combine(MistriaLocation, "data.win"));
    
    public bool ValidModsLocation() => !string.IsNullOrEmpty(ModsLocation) &&
                                       Directory.Exists(ModsLocation);
}