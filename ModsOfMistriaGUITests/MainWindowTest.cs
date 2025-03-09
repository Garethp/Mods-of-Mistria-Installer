using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
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
        var window = new MainWindow() { DataContext = mainViewModel };

        // Open window:
        window.Show();
        
        Assert.AreEqual("Mods Of Mistria Installer", window.Title);
    }
}