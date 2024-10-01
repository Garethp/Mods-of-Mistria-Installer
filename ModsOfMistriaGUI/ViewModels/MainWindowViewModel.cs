using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaGUI.Lang;
using MsBox.Avalonia;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        MistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        ModsLocation = MistriaLocator.GetModsLocation(_mistriaLocation) ?? "";

        if (Directory.Exists(ModsLocation))
        {
            Mods.Clear();

            var mods = MistriaLocator.GetMods(ModsLocation);

            new ModInstaller(MistriaLocation, ModsLocation).ValidateMods(mods);

            mods.ForEach(mod => Mods.Add(new ModModel(mod)));
        }

        if (MistriaLocation.Equals(""))
        {
            InstallStatus = Resources.CouldNotFindMistria;
        }
        else if (ModsLocation.Equals(""))
        {
            InstallStatus = Resources.CouldNotFindMods;
        }
        else if (Mods.Count == 0)
        {
            InstallStatus = Resources.NoModsToInstall;
        }
        else if (Mods.Any(mod => mod.CanInstall is not null))
        {
            InstallStatus = Resources.ModsRequireNewerVersion;
        }
    }

    [ObservableProperty] private string _installStatus = "";

    [ObservableProperty] private string _modsLocation = "";

    [ObservableProperty] private string _mistriaLocation = "";

    [ObservableProperty] private string _exception = "";
    
    private bool _isInstalling;

    public ObservableCollection<ModModel> Mods { get; } = [];

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        InstallStatus = Resources.InstallInProgress;
        _isInstalling = true;

        Task.Run(BackgroundInstall);
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void UnInstallMods()
    {
        _isInstalling = true;

        InstallStatus = "Uninstalling";

        Task.Run(async () =>
        {
            try
            {
                var installer = new ModInstaller(MistriaLocation, ModsLocation);
                
                var information = installer.PreUninstallInformation();
                if (information.Count > 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => MessageBoxManager.GetMessageBoxStandard(Resources.UninstallInformationTitle, string.Join('\n', information)).ShowAsync());
                }

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

    private async void BackgroundInstall()
    {
        try
        {
            var installer = new ModInstaller(MistriaLocation, ModsLocation);

            var modsToInstall = Mods.Where(model => model.Enabled).Select(model => model.Mod).ToList();

            var information = installer.PreinstallInformation(modsToInstall);
            if (information.Count > 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() => MessageBoxManager.GetMessageBoxStandard(Resources.PreinstallInformationTitle, string.Join('\n', information)).ShowAsync());
            }
            
            installer.InstallMods(modsToInstall, (message, _) =>
            {
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

    private bool CanInstall() => !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Count > 0 &&
                                 !_isInstalling && Mods.All(mod => mod.CanInstall is null);
}