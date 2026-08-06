using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Garethp.ModsOfMistriaGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FitToWorkingArea();
    }

    private void FitToWorkingArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        var scale = screen.Scaling;
        var area = screen.WorkingArea;
        var maxWidth = Math.Max(640, area.Width / scale - 40);
        var maxHeight = Math.Max(480, area.Height / scale - 40);

        Width = Math.Min(Width, maxWidth);
        Height = Math.Min(Height, maxHeight);
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);

        Position = new PixelPoint(
            area.X + (int)((area.Width - Width * scale) / 2),
            area.Y + (int)((area.Height - Height * scale) / 2));
    }

    private async void OpenGitHubClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri(AppInfo.GitHubUrl));
        }
        catch
        {
            // A missing desktop URI handler must not take down the installer.
        }
    }
}
