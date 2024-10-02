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
    
    [ObservableProperty] private PageViewBase _currentPage;

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
        };
    }
}