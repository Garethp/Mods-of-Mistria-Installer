using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

internal enum Pages
{
    GettingStarted,
    Modlist
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Settings _settings = new();

    private readonly Dictionary<Pages, PageViewBase> _pages;

    private GameRestartMonitor? _restartMonitor;

    [ObservableProperty] private PageViewBase _currentPage;
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string _updateMessage = "";

    public string WindowTitle => $"Mods of Mistria Installer — {AppInfo.DisplayVersion}";

    public void ShowUpdateAvailable(string version)
    {
        UpdateMessage = $"MOMI {version} is available.";
        UpdateAvailable = true;
    }

    public void SaveCurrentState()
    {
        if (CurrentPage is ModlistPageViewModel modlist)
            modlist.SaveCurrentProfileState();
    }

    public MainWindowViewModel()
    {
        _settings.MistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        _settings.ModsLocation = MistriaLocator.GetModsLocation(_settings.MistriaLocation) ?? "";

        _pages = new Dictionary<Pages, PageViewBase>
        {
            { Pages.GettingStarted , new GettingStartedPageViewModel(_settings) },
            { Pages.Modlist, new ModlistPageViewModel(_settings) }
        };

        if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation())
        {
            CurrentPage = _pages[Pages.GettingStarted];
        }
        else
        {
            CurrentPage = _pages[Pages.Modlist];
        }

        _settings.PropertyChanged += (_, _) =>
        {
            if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation()) return;
            CurrentPage = _pages[Pages.Modlist];
            StartRestartMonitor();
        };

        StartRestartMonitor();
    }

    private void StartRestartMonitor()
    {
        _restartMonitor?.Stop();
        if (string.IsNullOrEmpty(_settings.MistriaLocation)) return;
        _restartMonitor = new GameRestartMonitor(_settings.MistriaLocation);
        _restartMonitor.Start();
    }
}
