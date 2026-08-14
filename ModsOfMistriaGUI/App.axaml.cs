using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Diagnostics;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaGUI.ViewModels;
using Garethp.ModsOfMistriaGUI.Views;
using Garethp.ModsOfMistriaInstallerLib;
using MsBox.Avalonia;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaGUI;

public class App : Application
{
    public static TopLevel? TopLevel { get; private set; }

    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource? _updateCheckCancellation;

    public App()
    {
        var stopwatch = Stopwatch.StartNew();
        LocalizationService.Instance.SetLanguage(Settings.LoadSavedUiLanguage());
        _mainViewModel = new MainWindowViewModel();
        PerformanceDiagnostics.Log($"Startup: App + MainWindowViewModel construction={stopwatch.ElapsedMilliseconds} ms");
    }

    public override void Initialize()
    {
        var stopwatch = Stopwatch.StartNew();
        AvaloniaXamlLoader.Load(this);
        PerformanceDiagnostics.Log($"Startup: Avalonia resources={stopwatch.ElapsedMilliseconds} ms");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var stopwatch = Stopwatch.StartNew();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow { DataContext = _mainViewModel };
            desktop.MainWindow = mainWindow;
            TopLevel = TopLevel.GetTopLevel(mainWindow);

            _updateCheckCancellation = new CancellationTokenSource();
            mainWindow.Closed += (_, _) =>
            {
                _mainViewModel.SaveCurrentState();
                _updateCheckCancellation.Cancel();
            };

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

            // Disabled in this isolated Nexus/sandbox test build. The normal
            // AIM build keeps the GitHub Releases update check enabled.
        }

        PerformanceDiagnostics.Log($"Startup: framework initialization={stopwatch.ElapsedMilliseconds} ms");

        base.OnFrameworkInitializationCompleted();
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentVersion = Version.Parse(AppInfo.Version);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AIM");
            using var response = await client.GetAsync(AppInfo.ReleaseApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var releases = JArray.Parse(json);
            var aimRelease = releases.FirstOrDefault(release =>
            {
                var name = release["name"]?.ToString() ?? "";
                var tag = release["tag_name"]?.ToString() ?? "";
                return name.StartsWith("AIM ", StringComparison.OrdinalIgnoreCase)
                       || tag.StartsWith("aim-", StringComparison.OrdinalIgnoreCase);
            });
            var tagName = aimRelease?["tag_name"]?.ToString();
            if (tagName is null) return;

            var latestVersion = Version.Parse(tagName.TrimStart('v'));
            if (latestVersion <= currentVersion || cancellationToken.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    _mainViewModel.ShowUpdateAvailable(latestVersion.ToString(3));
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when the main window closes during the request.
        }
        catch (Exception)
        {
            // Update checks are advisory and must never prevent startup.
        }
    }
}
