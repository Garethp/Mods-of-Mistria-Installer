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

        Assert.That(window.Title, Is.EqualTo($"AIM — Alternative Installer for Mistria — {AppInfo.DisplayVersion}"));
    }

    [AvaloniaTest]
    public void Should_Localize_Available_Update_Message_When_Language_Changes()
    {
        LocalizationService.Instance.SetLanguage("en");
        var mainViewModel = new MainWindowViewModel();
        mainViewModel.ShowUpdateAvailable("0.15.8");

        LocalizationService.Instance.SetLanguage("bg");

        Assert.That(mainViewModel.UpdateMessage, Is.EqualTo("Налична е нова версия на AIM: 0.15.8."));
        LocalizationService.Instance.SetLanguage("en");
    }

    [Test]
    public void Should_Use_The_Complete_Polish_Resource_Set()
    {
        LocalizationService.Instance.SetLanguage("pl");

        Assert.Multiple(() =>
        {
            Assert.That(LocalizationService.Instance["GUIInstallButtonText"], Is.EqualTo("Instaluj"));
            Assert.That(LocalizationService.Instance["CoreCosmeticUiSubCategoryWrong"],
                Does.StartWith("Kosmetyk {0} ma nieprawidłowe ui_sub_category."));
        });

        LocalizationService.Instance.SetLanguage("en");
    }

    [Test]
    public void Should_Not_Broadcast_When_Selecting_The_Active_Language()
    {
        LocalizationService.Instance.SetLanguage("en");
        var notifications = 0;
        EventHandler handler = (_, _) => notifications++;
        LocalizationService.Instance.LanguageChanged += handler;

        try
        {
            LocalizationService.Instance.SetLanguage("en");
            Assert.That(notifications, Is.Zero);
        }
        finally
        {
            LocalizationService.Instance.LanguageChanged -= handler;
        }
    }
}
