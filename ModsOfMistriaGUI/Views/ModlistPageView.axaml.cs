using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.ViewModels;

namespace Garethp.ModsOfMistriaGUI.Views;

public partial class ModlistPageView : UserControl
{
    public ModlistPageView()
    {
        InitializeComponent();
    }

    // Route ComboBox SelectionChanged to SwitchProfileCommand.
    // The ComboBox binding is Mode=OneWay so the ViewModel's CurrentProfile is
    // NOT updated by user selection — we must explicitly call the command and let
    // it update CurrentProfile on success (or restore ComboBox on cancel).
    private async void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb) return;
        if (cb.SelectedItem is not string newProfile) return;
        if (DataContext is not ModlistPageViewModel vm) return;
        if (newProfile == vm.CurrentProfile) return; // programmatic update, not user action

        if (vm.SwitchProfileCommand is IAsyncRelayCommand<string> asyncCmd)
            await asyncCmd.ExecuteAsync(newProfile);
        else
            vm.SwitchProfileCommand.Execute(newProfile);

        // If the switch was cancelled, reset the ComboBox back to the actual current profile
        if ((string?)cb.SelectedItem != vm.CurrentProfile)
            cb.SelectedItem = vm.CurrentProfile;
    }
}
