using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaGUI.ViewModels;

namespace Garethp.ModsOfMistriaGUI.Views;

public partial class ModlistPageView : UserControl
{
    // Drags never leave the process, so the payload is the ModModel itself
    // rather than anything serialized.
    private static readonly DataFormat<ModModel> ModDragFormat =
        DataFormat.CreateInProcessFormat<ModModel>("ModsOfMistria.Mod");

    public ModlistPageView()
    {
        InitializeComponent();
    }
    
    private async void OnModHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ModModel mod }) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ModDragFormat, mod));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void OnModsDragOver(object? sender, DragEventArgs dragEvent)
    {
        dragEvent.Handled = true;
        
        if (dragEvent.DataTransfer.TryGetValue(ModDragFormat) is null)
        {
            dragEvent.DragEffects = DragDropEffects.None;
            InsertionLine.IsVisible = false;
            return;
        }
        
        var gap = GetInsertionLinePosition(dragEvent);
        if (gap is not { } target) return;
        
        dragEvent.DragEffects = DragDropEffects.Move;
        
        // We use the InsertionLine Margin to move the line up and down the list
        InsertionLine.Margin    = new Thickness(0, target.Y - 1, 0, 0);
        InsertionLine.IsVisible = true;
    }
    
    private void OnModsDragLeave(object? sender, DragEventArgs dragEvent)
    {
        // The event gets fired when the mouse moves between rows for some reason, so let's check if the mouse is still
        // over the list.
        var relativePosition = dragEvent.GetPosition(ModRepeater);
        if (relativePosition is { X: >= 0, Y: >= 0 } && relativePosition.X <= ModRepeater.Bounds.Width && relativePosition.Y <= ModRepeater.Bounds.Height)
            return;

        InsertionLine.IsVisible = false;
    }

    private void OnModsDrop(object? sender, DragEventArgs e)
    {
        InsertionLine.IsVisible = false;

        if (e.DataTransfer.TryGetValue(ModDragFormat) is not { } mod) return;
        if (GetInsertionLinePosition(e) is not { } target) return;
        if (DataContext is not ModlistPageViewModel viewModel) return;

        viewModel.MoveMod(mod, target.Slot);
        e.Handled = true;
    }

    // The gap the pointer is nearest: its index in the list (0 = above the first
    // mod, ItemCount = below the last) and its Y in ItemsControl coordinates.
    private (int Slot, double Y)? GetInsertionLinePosition(DragEventArgs e)
    {
        var count = ModRepeater.ItemCount;
        if (count == 0) return null;

        var mouseY = e.GetPosition(ModRepeater).Y;
        var rowBottom = 0.0;

        for (var i = 0; i < count; i++)
        {
            if (ModRepeater.ContainerFromIndex(i) is not { } row) continue;

            var top = row.TranslatePoint(default, ModRepeater)?.Y ?? 0;
            rowBottom = top + row.Bounds.Height;

            // Above the row's midpoint means the gap before it.
            if (mouseY < top + row.Bounds.Height / 2) return (i, top);
        }

        // Past every midpoint, so the gap below the last mod.
        return (count, rowBottom);
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
