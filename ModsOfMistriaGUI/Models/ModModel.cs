using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaGUI.Models;

// The outcome of this session's most recent install action. Resets to None
// when the mod list reloads or an uninstall runs; it is never persisted.
public enum ModInstallState
{
    None,
    Installed,
    Skipped,
    Failed,
}

public partial class ModModel : ObservableObject
{
    public LocalizationService Localization => LocalizationService.Instance;
    public LocalizedTexts Texts => LocalizedTexts.Instance;
    public readonly IMod Mod;
    
    private bool _enabledBacking;

    [ObservableProperty] private int _position;

    public bool IsAlternateRow => Position % 2 == 0;

    partial void OnPositionChanged(int value)
        => OnPropertyChanged(nameof(IsAlternateRow));

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

    // ── Install outcome ───────────────────────────────────────────────────────

    private ModInstallState _installState = ModInstallState.None;
    private bool _installDetailIsSuccessMessage;

    // What the expander says about the outcome: "Installed successfully." or
    // the skip reasons
    public string InstallDetail { get; private set; } = "";

    public bool WasInstalled      => _installState == ModInstallState.Installed;
    public bool WasSkipped        => _installState == ModInstallState.Skipped;
    public bool WasFailed         => _installState == ModInstallState.Failed;
    public bool HasInstallOutcome => _installState != ModInstallState.None;

    // A skipped mod's reasons also land as validation errors; the red X and
    // InstallDetail already carry them, so the error triangle and error text
    // stand down while the skip is showing
    public bool ShowErrorIcon => InError && !WasSkipped;

    // The plain checkbox row is for a valid mod with nothing to report; any
    // validation message or install outcome swaps in the expander
    public bool ShowPlainRow  => IsValid && !HasInstallOutcome;
    public bool ShowStatusRow => !ShowPlainRow;

    public void SetInstallOutcome(ModInstallState state, string detail = "")
    {
        _installState = state;
        InstallDetail = detail;
        _installDetailIsSuccessMessage = state == ModInstallState.Installed;
        OnPropertyChanged(nameof(WasInstalled));
        OnPropertyChanged(nameof(WasSkipped));
        OnPropertyChanged(nameof(WasFailed));
        OnPropertyChanged(nameof(HasInstallOutcome));
        OnPropertyChanged(nameof(InstallDetail));
        OnPropertyChanged(nameof(ShowErrorIcon));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    // An install can add validation messages (a skipped mod's reasons land as
    // errors); the expander re-reads them when told
    public void RefreshValidation()
    {
        OnPropertyChanged(nameof(InWarning));
        OnPropertyChanged(nameof(InError));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(Warnings));
        OnPropertyChanged(nameof(Errors));
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(ShowErrorIcon));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    public bool NeedsLocalizedValidation => InError || InWarning;

    public void RefreshLocalizedText()
    {
        if (_installDetailIsSuccessMessage)
        {
            InstallDetail = Resources.GUIModInstalled;
            OnPropertyChanged(nameof(InstallDetail));
        }
        // Validation state does not change merely because the display
        // language changed. Avoid notifying every status binding here; with
        // a large mod list those redundant notifications force Avalonia to
        // measure and arrange every row repeatedly.
        OnPropertyChanged(nameof(Full));
        OnPropertyChanged(nameof(UpdateTooltip));
    }

    public void RevalidateForLocalization()
    {
        Mod.Validate();
        ModInstaller.ValidateMods(new List<IMod> { Mod });
    }

    public string Full => string.Format(Resources.GUIModByAuthorWithVersion, Mod.GetName(), Mod.GetAuthor(), Mod.GetVersion());

    public string UpdateTooltip =>
        LatestVersion is null
            ? Texts.GUIUpdateMod
            : $"{Texts.GUIUpdateMod}: v{LatestVersion}";

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
