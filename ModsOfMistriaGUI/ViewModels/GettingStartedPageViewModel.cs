﻿using System.Runtime.InteropServices;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public partial class GettingStartedPageViewModel(Settings settings) : PageViewBase
{
    [ObservableProperty] private Settings _settings = settings;

    public bool CanCreateModsFolder =>  Settings.ValidMistriaLocation() && !Settings.ValidModsLocation();

    [RelayCommand]
    private async Task SelectMistriaLocation()
    {
        var topLevel = App.TopLevel;
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open FieldsOfMistria.exe",
            FileTypeFilter =
            [
                new FilePickerFileType("FieldsOfMistria.exe")
                {
                    Patterns = ["FieldsOfMistria.exe"]
                }
            ],
            AllowMultiple = false
        });

        if (files.Count == 1)
        {
            var path = files[0].TryGetLocalPath();
            if (path is null) return;
            
            Settings.MistriaLocation = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";

            if (Settings.ValidMistriaLocation() && !Settings.ValidModsLocation())
            {
                Settings.ModsLocation = MistriaLocator.GetModsLocation(Settings.MistriaLocation) ?? "";
            }
        }
    }
    
    [RelayCommand]
    private async Task SelectModsLocation()
    {
        var topLevel = App.TopLevel;
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Mods Folder",
            AllowMultiple = false
        });

        if (files.Count == 1)
        {
            var path = files[0].TryGetLocalPath();
            if (path is null) return;
            
            Settings.ModsLocation = Path.GetFullPath(path);
        }
    }

    [RelayCommand]
    private void CreateModsFolder()
    {
        if (Settings.ValidModsLocation()) return;

        string path;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mistria-mods");
        }
        else
        {
            if (!Settings.ValidMistriaLocation()) return;
            path = Path.Combine(Settings.MistriaLocation, "mods");
        }
        
        Directory.CreateDirectory(path);
        Settings.ModsLocation = path;
    }
}