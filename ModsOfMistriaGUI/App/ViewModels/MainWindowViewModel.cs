using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.App.Models;
using Garethp.ModsOfMistriaInstallerLib;
using ModsOfMistriaGUI.App.Lang;

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
                .Where(folder => Mod.GetModLocation(folder) is not null)
                .Select(location => Mod.FromManifest(Path.Combine(Mod.GetModLocation(location)!, "manifest.json")))
                .ToList();
            
            new ModInstaller(MistriaLocation).ValidateMods(mods);
            
            mods.ForEach(mod => Mods.Add(new ModModel()
            {
                mod = mod,
                CanInstall = mod.CanInstall() ?? ""
            }));
        }
        
        if (MistriaLocation.Equals(""))
        {
            InstallStatus = Resources.CouldNotFindMistria;
        } else if (ModsLocation.Equals(""))
        {
            InstallStatus = Resources.CouldNotFindMods;
        } else if (Mods.Count == 0)
        {
            InstallStatus = Resources.NoModsToInstall;
        } else if (Mods.Any(mod => mod.CanInstall is not null))
        {
            InstallStatus = Resources.ModsRequireNewerVersion;
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
        InstallStatus = Resources.InstallInProgress;
        _isInstalling = true;
        
        Task.Run(backgroundInstall);
    }

    private async void backgroundInstall()
    {
        var installer = new ModInstaller(_mistriaLocation);

        installer.InstallMods(Mods.Where(model => model.Enabled).Select(model => model.mod).ToList(), (message, timeTaken) =>
        {
            Console.WriteLine($"Ran {message} in {timeTaken}");
            InstallStatus = message;
        });

        _isInstalling = false;
    }
    
    private bool CanInstall() => !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Count > 0 && !_isInstalling && Mods.All(mod => mod.CanInstall is null);
}