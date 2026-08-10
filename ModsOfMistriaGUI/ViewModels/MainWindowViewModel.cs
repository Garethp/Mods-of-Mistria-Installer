using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaInstallerLib;

using CommunityToolkit.Mvvm.Input;

using System.Diagnostics;

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
    private string? _availableVersion;

    public bool IsBusy => CurrentPage switch
    {
        ModlistPageViewModel modlist => modlist.IsInstalling,
        GettingStartedPageViewModel gettingStarted => gettingStarted.IsBusy,
        _ => false
    };

    public string WindowTitle => $"{Localization["GUIApplicationTitle"]} — {AppInfo.DisplayVersion}";

    [RelayCommand]
    private void SetLanguage(string? languageCode)
        => ChangeLanguage(languageCode);

    public void ChangeLanguage(string? languageCode)
    {
        _settings.UiLanguage = string.IsNullOrWhiteSpace(languageCode) ? "system" : languageCode;
        Localization.SetLanguage(_settings.UiLanguage);
    }

    public void ShowUpdateAvailable(string version)
    {
        _availableVersion = version;
        UpdateMessage = string.Format(Localization["GUIUpdateAvailable"], _availableVersion);
        UpdateAvailable = true;
    }

    public void SaveCurrentState()
    {
        if (CurrentPage is ModlistPageViewModel modlist)
            modlist.SaveCurrentProfileState();
    }

    public MainWindowViewModel()
    {
        var stopwatch = Stopwatch.StartNew();
        Localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WindowTitle));
            if (UpdateAvailable && _availableVersion is not null)
                UpdateMessage = string.Format(Localization["GUIUpdateAvailable"], _availableVersion);
        };
        _settings.LoadPreferences();
        var locationStopwatch = Stopwatch.StartNew();
        _settings.MistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        _settings.ModsLocation = MistriaLocator.GetModsLocation(_settings.MistriaLocation) ?? "";
        PerformanceDiagnostics.Log($"Startup: location detection={locationStopwatch.ElapsedMilliseconds} ms, gameFound={!string.IsNullOrEmpty(_settings.MistriaLocation)}, modsFound={!string.IsNullOrEmpty(_settings.ModsLocation)}");

        _pages = new Dictionary<Pages, PageViewBase>
        {
            { Pages.GettingStarted , new GettingStartedPageViewModel(_settings) },
            { Pages.Modlist, new ModlistPageViewModel(_settings) }
        };

        foreach (var page in _pages.Values)
        {
            page.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ModlistPageViewModel.IsInstalling)
                    or nameof(GettingStartedPageViewModel.IsBusy))
                    OnPropertyChanged(nameof(IsBusy));
            };
        }

        if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation())
        {
            CurrentPage = _pages[Pages.GettingStarted];
        }
        else
        {
            CurrentPage = _pages[Pages.Modlist];
        }
        OnPropertyChanged(nameof(IsBusy));

        _settings.PropertyChanged += (_, _) =>
        {
            if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation()) return;
            CurrentPage = _pages[Pages.Modlist];
            OnPropertyChanged(nameof(IsBusy));
            StartRestartMonitor();
        };

        StartRestartMonitor();
        PerformanceDiagnostics.Log($"Startup: MainWindowViewModel total={stopwatch.ElapsedMilliseconds} ms, page={CurrentPage.GetType().Name}");
    }

    private void StartRestartMonitor()
    {
        _restartMonitor?.Stop();
        if (string.IsNullOrEmpty(_settings.MistriaLocation)) return;
        _restartMonitor = new GameRestartMonitor(_settings.MistriaLocation);
        _restartMonitor.Start();
    }
}
