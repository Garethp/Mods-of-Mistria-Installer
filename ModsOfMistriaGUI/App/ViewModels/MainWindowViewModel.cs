﻿using System.Collections.ObjectModel;
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
        _mistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        
        var detectedModsLocation = _modOverride;

        if (detectedModsLocation is null || !Directory.Exists(detectedModsLocation))
        {
            detectedModsLocation =
                Path.Combine(_mistriaLocation, "mods");
        }

        if (!Directory.Exists(detectedModsLocation))
        {
            detectedModsLocation = Path.Combine(_mistriaLocation, "Mods");
        }

        if (Directory.Exists(detectedModsLocation))
        {
            Mods.Clear();

            mods = Directory
                .GetDirectories(detectedModsLocation)
                .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
                .Select(location => Mod.FromManifest(Path.Combine(location, "manifest.json")))
                .ToList();
            
            mods.ForEach(mod => Mods.Add(new ModModel()
            {
                _name = mod.Name,
                _author = mod.Author
            }));
        }
    }

    [ObservableProperty] string _installStatus = "";
    
    [ObservableProperty] string _mistriaLocation = "";

    private string? _modOverride;

    private List<Mod> mods;

    private bool _isInstalling;
    
    public ObservableCollection<ModModel> Mods { get; } = new ();

    public List<string> GetMods()
    {
        return ["Mod 1", "mod 2"];
    }

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
    
    private bool CanInstall() => true && !_isInstalling;
}