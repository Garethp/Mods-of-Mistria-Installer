using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.App.Models;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
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

            mods = MistriaLocator.GetMods(ModsLocation);
            
            new ModInstaller(MistriaLocation, ModsLocation).ValidateMods(mods);
            
            mods.ForEach(mod => Mods.Add(new ModModel()
            {
                mod = mod,
                CanInstall = mod.CanInstall()
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

    [ObservableProperty] private string _exception = "";

    private string? _modOverride;

    private List<IMod> mods;

    private bool _isInstalling;
    
    public ObservableCollection<ModModel> Mods { get; } = new ();

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        InstallStatus = Resources.InstallInProgress;
        _isInstalling = true;
        
        Task.Run(backgroundInstall);
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void UnInstallMods()
    {
        _isInstalling = false;

        InstallStatus = "Uninstalling";
        
        Task.Run(() =>
        {
            try
            {
                var installer = new ModInstaller(MistriaLocation, ModsLocation);

                installer.Uninstall();

                _isInstalling = false;
                InstallStatus = "Uninstall Complete";
            }
            catch (Exception e)
            {
                Exception = e.Message;
            }

        });
    }

    private async void backgroundInstall()
    {
        try
        {
            var installer = new ModInstaller(_mistriaLocation, ModsLocation);

            installer.InstallMods(Mods.Where(model => model.Enabled).Select(model => model.mod).ToList(),
                (message, timeTaken) =>
                {
                    Console.WriteLine($"Ran {message} in {timeTaken}");
                    InstallStatus = message;
                });

            _isInstalling = false;
        }
        catch (Exception e)
        {
            Exception = e.Message;
        }
    }
    
    private bool CanRemove() => !MistriaLocation.Equals("") && !_isInstalling;

    private bool CanInstall() => !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Count > 0 && !_isInstalling && Mods.All(mod => mod.CanInstall is null);
}