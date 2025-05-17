using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Lang;
using Garethp.ModsOfMistriaGUI.Lang;
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
        _settings.PropertyChanged += (_, _) => { Task.Run(UpdateModlist); };

        Task.Run(UpdateModlist);
    }

    private void UpdateModlist()
    {
        UpdateModlist(false);
    }
    
    private void UpdateModlist(bool force)
    {
        if (_updating) return;
        if (MistriaLocation == _settings.MistriaLocation && ModsLocation == _settings.ModsLocation && !force) return;
        _updating = true;

        MistriaLocation = _settings.MistriaLocation;
        ModsLocation = _settings.ModsLocation;

        Mods.Clear();

        if (Directory.Exists(ModsLocation))
        {
            var mods = MistriaLocator.GetMods(MistriaLocation, ModsLocation);

            new ModInstaller(MistriaLocation, ModsLocation).ValidateMods(mods);

            var allModsDisabled = mods.All(mod => !mod.IsInstalled());
            if (allModsDisabled)
            {
                mods.ForEach(mod => mod.SetInstalled(true));
            }

            mods.ForEach(mod => { Mods.Add(new ModModel(mod)); });
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InstallStatus = "";
            InstallModsCommand.NotifyCanExecuteChanged();

            if (MistriaLocation.Equals(""))
            {
                InstallStatus = Resources.GUICouldNotFindMistria;
            }
            else if (ModsLocation.Equals(""))
            {
                InstallStatus = Resources.GUICouldNotFindMods;
            }
            else if (Mods.Count == 0)
            {
                InstallStatus = Resources.GUINoModsToInstall;
            }
            else if (Mods.Any(mod => mod.CanInstall is not null))
            {
                InstallStatus = Resources.GUIModsRequireNewerVersion;
            }
        });

        _updating = false;
    }

    [ObservableProperty] private string _installStatus = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))] [ObservableProperty]
    private string _modsLocation = "";

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
        InstallStatus = Resources.GUIInstallInProgress;
        IsInstalling = true;

        Task.Run(BackgroundInstall);
    }

    [RelayCommand]
    private async Task SaveLogFile()
    {
        var topLevel = App.TopLevel;
        if (topLevel is null) return;

        var logs = Logger.GetLogs();

        var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = Resources.GUIPickLogFile,
            FileTypeChoices = [FilePickerFileTypes.TextPlain]
        });

        if (files is not null)
        {
            await File.WriteAllTextAsync(files.Path.AbsolutePath, string.Join("\r\n", logs));
        }
    }

    [RelayCommand]
    private void ReloadModlist()
    {
        UpdateModlist(true);
    }

    [RelayCommand]
    private void EnableAllMods()
    {
        var allMods = Mods.ToList();
        Mods.Clear();
        allMods.ForEach(mod =>
        {
            mod.Enabled = true;
            Mods.Add(mod);
        });
        InstallModsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void DisableAllMods()
    {
        var allMods = Mods.ToList();
        Mods.Clear();
        allMods.ForEach(mod =>
        {
            mod.Enabled = false;
            Mods.Add(mod);
        });

        InstallModsCommand.NotifyCanExecuteChanged();
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
                            .GetMessageBoxStandard(Resources.GUIUninstallInformationTitle, string.Join('\n', information))
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
                        .GetMessageBoxStandard(Resources.GUIPreinstallInformationTitle, string.Join('\n', information))
                        .ShowAsync());
            }

            installer.InstallMods(modsToInstall, (message, _) =>
            {
                Logger.Log(message);
                InstallStatus = message;
            });

            Dispatcher.UIThread.InvokeAsync(() => { IsInstalling = false; });
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