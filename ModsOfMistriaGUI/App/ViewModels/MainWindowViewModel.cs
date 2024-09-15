using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.App.Models;
using Garethp.ModsOfMistriaInstaller;

namespace Garethp.ModsOfMistriaGUI.App.ViewModels;

public partial class MainWindowViewModel: ViewModelBase
{
    public MainWindowViewModel()
    {
        MistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        ModsLocation = MistriaLocator.GetModsLocation(_mistriaLocation) ?? "";
        
        if (Directory.Exists(ModsLocation))
        {
            Mods.Clear();

            mods = Directory
                .GetDirectories(ModsLocation)
                .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
                .Select(location => Mod.FromManifest(Path.Combine(location, "manifest.json")))
                .ToList();
            
            mods.ForEach(mod => Mods.Add(new ModModel()
            {
                _name = mod.Name,
                _author = mod.Author,
                CanInstall = mod.CanInstall()
            }));
        }

        if (MistriaLocation.Equals(""))
        {
            InstallStatus = "Could not find Fields of Mistria location. Try placing this in the same folder as Fields of Mistria.";
        } else if (ModsLocation.Equals(""))
        {
            InstallStatus = "Could not find a mods folder. Try creating a folder called 'mods' in the Fields of Mistria folder.";
        } else if (Mods.Count == 0)
        {
            InstallStatus = "No mods found to install";
        } else if (Mods.Any(mod => mod.CanInstall is not null))
        {
            InstallStatus = "Some mods require a newer version of the installer. Please update the installer.";
        }
    }

    [ObservableProperty] string _installStatus = "";

    [ObservableProperty] private string _modsLocation = "";
    
    [ObservableProperty] string _mistriaLocation = "";

    private string? _modOverride;

    private List<Mod> mods;

    private bool _isInstalling;
    
    public ObservableCollection<ModModel> Mods { get; } = new ();

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        InstallStatus = "Installing mods...";
        _isInstalling = true;
        
        Task.Run(() =>
        {
            backgroundInstall();
        });
    }

    private async void backgroundInstall()
    {
        var installer = new ModInstaller(_mistriaLocation);

        installer.InstallMods(mods, (message, timeTaken) =>
        {
            Console.Write($"Ran {message} in {timeTaken}");
            InstallStatus = message;
        });

        _isInstalling = false;
    }
    
    private bool CanInstall() => !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Count > 0 && !_isInstalling && Mods.All(mod => mod.CanInstall is null);
}