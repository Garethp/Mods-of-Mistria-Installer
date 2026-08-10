using Avalonia.Headless.NUnit;
using Garethp.ModsOfMistriaGUI;
using Garethp.ModsOfMistriaGUI.ViewModels;
using Garethp.ModsOfMistriaGUI.Views;
using Garethp.ModsOfMistriaGUI.Services;

namespace ModsOfMistriaGUITests;

public class Tests
{
    [AvaloniaTest]
    public void Should_Type_Text_Into_TextBox()
    {
        // Keep the title assertion independent of the machine's system locale
        // and any persisted UI-language preference.
        LocalizationService.Instance.SetLanguage("en");
        var mainViewModel = new MainWindowViewModel();

        // Setup controls:
        var window = new MainWindow() {DataContext = mainViewModel};

        // Open window:
        window.Show();

        Assert.That(window.Title, Is.EqualTo($"Mods of Mistria Installer — {AppInfo.DisplayVersion}"));
    }

    [AvaloniaTest]
    public void Should_Localize_Available_Update_Message_When_Language_Changes()
    {
        LocalizationService.Instance.SetLanguage("en");
        var mainViewModel = new MainWindowViewModel();
        mainViewModel.ShowUpdateAvailable("0.15.8");

        LocalizationService.Instance.SetLanguage("bg");

        Assert.That(mainViewModel.UpdateMessage, Is.EqualTo("Налична е нова версия на MOMI: 0.15.8."));
        LocalizationService.Instance.SetLanguage("en");
    }
}
