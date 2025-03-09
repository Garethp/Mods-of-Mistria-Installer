using Avalonia;
using Avalonia.Headless.NUnit;
using Garethp.ModsOfMistriaGUI;
using Garethp.ModsOfMistriaGUI.ViewModels;
using Garethp.ModsOfMistriaGUI.Views;

namespace ModsOfMistriaGUITests;

public class Tests
{
    [AvaloniaTest]
    public void Should_Type_Text_Into_TextBox()
    {
        var mainViewModel = new MainWindowViewModel();

        // Setup controls:
        var window = new MainWindow() {DataContext = mainViewModel};

        // Open window:
        window.Show();

        Assert.AreEqual("Mods Of Mistria Installer", window.Title);
    }

    [TestCase]
    public void Should_Be_Able_To_Setup()
    {
        Assert.DoesNotThrow(() =>
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .SetupWithClassicDesktopLifetime([]);
        });
    }
}