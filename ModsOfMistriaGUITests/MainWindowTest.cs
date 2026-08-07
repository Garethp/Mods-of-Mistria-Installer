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

        Assert.That(window.Title, Is.EqualTo($"Mods of Mistria Installer — {AppInfo.DisplayVersion}"));
    }
}
