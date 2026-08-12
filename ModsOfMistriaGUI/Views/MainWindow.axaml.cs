using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaGUI.ViewModels;

namespace Garethp.ModsOfMistriaGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FitToWorkingArea();
        Opened += (_, _) => UpdateLanguageCheckmark();
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
            await Launcher.LaunchUriAsync(new Uri(AppInfo.ReleasesUrl));
        }
        catch
        {
            // A missing desktop URI handler must not take down the installer.
        }
    }

    private void LanguageMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string languageCode } && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ChangeLanguage(languageCode);
            UpdateLanguageCheckmark();
        }
    }

    private void UpdateLanguageCheckmark()
    {
        var selected = LocalizationService.Instance.LanguageCode;
        var items = new[]
        {
            LanguageSystemMenuItem, LanguageEnglishMenuItem, LanguageBulgarianMenuItem,
            LanguagePolishMenuItem,
            LanguageGermanMenuItem, LanguageFrenchMenuItem, LanguageDutchMenuItem,
            LanguagePortugueseMenuItem, LanguageRussianMenuItem, LanguageIndonesianMenuItem,
            LanguageSimplifiedChineseMenuItem, LanguageTraditionalChineseMenuItem,
            LanguageKoreanMenuItem, LanguageJapaneseMenuItem, LanguageSpanishMenuItem,
            LanguageUkrainianMenuItem
        };

        foreach (var item in items)
        {
            var isSelected = string.Equals(item.Tag as string, selected, StringComparison.OrdinalIgnoreCase);
            item.Icon = isSelected
                ? new TextBlock
                {
                    Text = "✓",
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
                : null;
        }
    }
}
