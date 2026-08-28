using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Garethp.ModsOfMistriaGUI.Models;

namespace Garethp.ModsOfMistriaGUI.Controls;

public partial class ModListView : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ModListView, IEnumerable?>(nameof(ItemsSource));

    // Invoked with (ModModel Mod, int Slot) — see ModlistPageViewModel.MoveMod.
    public static readonly StyledProperty<ICommand?> MoveModCommandProperty =
        AvaloniaProperty.Register<ModListView, ICommand?>(nameof(MoveModCommand));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? MoveModCommand
    {
        get => GetValue(MoveModCommandProperty);
        set => SetValue(MoveModCommandProperty, value);
    }

    // Drags never leave the process, so the payload is the ModModel itself
    // rather than anything serialized.
    private static readonly DataFormat<ModModel> ModDragFormat =
        DataFormat.CreateInProcessFormat<ModModel>("ModsOfMistria.Mod");

    public ModListView()
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

        MoveModCommand?.Execute((mod, target.Slot));
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
}
