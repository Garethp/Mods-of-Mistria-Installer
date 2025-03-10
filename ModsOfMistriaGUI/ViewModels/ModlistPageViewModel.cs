using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;
using MsBox.Avalonia;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public partial class ModlistPageViewModel : PageViewBase
{
    private bool _updating;
    private readonly Settings _settings;
    
    public ModlistPageViewModel(Settings settings)
    {
        _settings = settings;
        _settings.PropertyChanged += (_, _) =>
        {
            Task.Run(UpdateModlist);
        };

        Task.Run(UpdateModlist);
    }

    private void UpdateModlist()
    {
        if (_updating) return;
        if (MistriaLocation == _settings.MistriaLocation && ModsLocation == _settings.ModsLocation) return;
        _updating = true;
        
        MistriaLocation = _settings.MistriaLocation;
        ModsLocation = _settings.ModsLocation;
        
        Mods.Clear();
        
        if (Directory.Exists(ModsLocation))
        {
            var mods = MistriaLocator.GetMods(MistriaLocation, ModsLocation);

            new ModInstaller(MistriaLocation, ModsLocation).ValidateMods(mods);

            mods.ForEach(mod =>
            {
                Mods.Add(new ModModel(mod));
            });
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InstallStatus = "";
            InstallModsCommand.NotifyCanExecuteChanged();

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
        });

        _updating = false;
    }

    [ObservableProperty] private string _installStatus = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [ObservableProperty] private string _modsLocation = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnInstallModsCommand))]
    [ObservableProperty]
    private string _mistriaLocation = "";

    [ObservableProperty] private string _exception = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnInstallModsCommand))]
    [ObservableProperty]
    private bool _isInstalling;

    public ObservableCollection<ModModel> Mods { get; } = [];

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        InstallStatus = Resources.InstallInProgress;
        IsInstalling = true;

        Task.Run(BackgroundInstall);
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void UnInstallMods()
    {
        IsInstalling = true;

        InstallStatus = "Uninstalling";

        Task.Run(async () =>
        {
            try
            {
                var installer = new ModInstaller(MistriaLocation, ModsLocation);

                var information = installer.PreUninstallInformation();
                if (information.Count > 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        MessageBoxManager
                            .GetMessageBoxStandard(Resources.UninstallInformationTitle, string.Join('\n', information))
                            .ShowAsync());
                }

                installer.Uninstall();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsInstalling = false;
                    InstallStatus = "Uninstall Complete";
                });
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
                await Dispatcher.UIThread.InvokeAsync(() =>
                    MessageBoxManager
                        .GetMessageBoxStandard(Resources.PreinstallInformationTitle, string.Join('\n', information))
                        .ShowAsync());
            }

            installer.InstallMods(modsToInstall, (message, _) => { InstallStatus = message; });

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsInstalling = false;
            });
        }
        catch (Exception e)
        {
            Exception = e.Message;
        }
    }

    private bool CanRemove() => !MistriaLocation.Equals("") && !IsInstalling;

    private bool CanInstall() =>
        !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Count > 0 &&
        !IsInstalling && Mods.All(mod => mod.CanInstall is null);
}