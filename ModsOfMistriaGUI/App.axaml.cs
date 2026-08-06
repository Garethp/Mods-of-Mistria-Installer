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

    private readonly MainWindowViewModel _mainViewModel = new();
    private CancellationTokenSource? _updateCheckCancellation;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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

            _ = CheckForUpdatesAsync(_updateCheckCancellation.Token);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentVersion = Version.Parse(AppInfo.Version);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MOMI");
            using var response = await client.GetAsync(AppInfo.ReleaseApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tagName = JObject.Parse(json)["tag_name"]?.ToString();
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
