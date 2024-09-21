using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Garethp.ModsOfMistriaGUI.App.ViewModels;
using Garethp.ModsOfMistriaGUI.App.Views;
using HarfBuzzSharp;

namespace Garethp.ModsOfMistriaGUI.App;

public partial class App : Application
{
    private readonly MainWindowViewModel _mainViewModel = new MainWindowViewModel();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = _mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}