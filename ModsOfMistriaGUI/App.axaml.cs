﻿using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Garethp.ModsOfMistriaGUI.ViewModels;
using Garethp.ModsOfMistriaGUI.Views;
using MsBox.Avalonia;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaGUI;

public class App : Application
{
    public static TopLevel? TopLevel { get; private set; }
    
    private readonly MainWindowViewModel _mainViewModel = new ();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };
            
            TopLevel = TopLevel.GetTopLevel(desktop.MainWindow);

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MessageBoxManager.GetMessageBoxStandard(
                        ModsOfMistriaInstallerLib.Lang.Resources.GUIWarning32BitTitle,
                        ModsOfMistriaInstallerLib.Lang.Resources.GUIWarning32Bit
                    ).ShowAsync();
                });
            }
            
            var upgradeMessage = MessageBoxManager.GetMessageBoxStandard(ModsOfMistriaInstallerLib.Lang.Resources.GUIUpdateNagTitle,
                ModsOfMistriaInstallerLib.Lang.Resources.GUIUpdateNagMessage);
        
            Task.Run(async () =>
            {
                try
                {
                    var currentExe = Assembly.GetEntryAssembly();
                    var currentVersionString =
                        currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";
                    var currentVersion = new Version(currentVersionString);
                    
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "request");
                    var json = await client.GetStringAsync(
                        "https://api.github.com/repos/Garethp/Mods-of-Mistria-Installer/releases/latest");

                    var output = JObject.Parse(json);
                    if (output["tag_name"]?.ToString() is not { } tagName) return;
                
                    var latestVersion = new Version(tagName.Replace("v", ""));
                
                    if (latestVersion.CompareTo(currentVersion) > 0)
                    {
                        Dispatcher.UIThread.InvokeAsync(() => { upgradeMessage.ShowAsync(); });
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}