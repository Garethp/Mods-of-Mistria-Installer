using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaGUI.Models;

public partial class ModModel : ObservableObject
{
    public readonly IMod Mod;
    
    private bool _enabledBacking;

    [ObservableProperty] private int _position;

    // Set by UpdateChecker after startup — true when a newer release is available
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string? _latestVersion;
    [ObservableProperty] private string? _updateDownloadUrl;

    public ModModel(IMod mod)
    {
        Mod = mod;
        _enabledBacking = mod.IsInstalled();
    }

    public ModModel()
    {
        Mod = new FolderMod();
    }

    public bool Enabled
    {
        get => !InError && _enabledBacking;
        set
        {
            if (_enabledBacking == value) return;
            _enabledBacking = value;
            Mod.SetInstalled(value);
            OnPropertyChanged();
        }
    }

    public bool InWarning => Mod.GetValidation().Status == ValidationStatus.Warning;
    public bool InError   => Mod.GetValidation().Status == ValidationStatus.Invalid;
    public bool IsValid   => Mod.GetValidation().Status == ValidationStatus.Valid;

    public string Warnings => string.Join("\r\n", Mod.GetValidation().Warnings.Select(w => w.Message));
    public string Errors   => string.Join("\r\n", Mod.GetValidation().Errors.Select(w => w.Message));

    public string Full => string.Format(Resources.GUIModByAuthorWithVersion, Mod.GetName(), Mod.GetAuthor(), Mod.GetVersion());

    public string UpdateTooltip =>
        LatestVersion is null
            ? "Update available"
            : $"Update available: v{LatestVersion} — click to open download page";

    [RelayCommand]
    private void OpenUpdateUrl()
    {
        var url = UpdateDownloadUrl ?? Mod.GetDownloadUrl();
        if (string.IsNullOrEmpty(url)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = url,
            UseShellExecute = true
        });
    }
}
