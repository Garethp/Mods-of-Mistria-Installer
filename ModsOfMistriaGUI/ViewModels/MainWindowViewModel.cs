using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaInstallerLib;

using CommunityToolkit.Mvvm.Input;

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
        Localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WindowTitle));
            if (UpdateAvailable && _availableVersion is not null)
                UpdateMessage = string.Format(Localization["GUIUpdateAvailable"], _availableVersion);
        };
        _settings.LoadPreferences();
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
