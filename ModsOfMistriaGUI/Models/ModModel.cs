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
    AlreadyInstalled,
}

public partial class ModModel : ObservableObject
{
    public LocalizationService Localization => LocalizationService.Instance;
    public LocalizedTexts Texts => LocalizedTexts.Instance;
    public readonly IMod Mod;
    private IReadOnlyList<IMod> _duplicateCopies = [];
    private IReadOnlyList<string> _conflictWarnings = [];
    private IReadOnlyList<string> _compatibilityWarnings = [];
    
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
            OnPropertyChanged(nameof(HasDuplicateWarning));
            OnPropertyChanged(nameof(InWarning));
            OnPropertyChanged(nameof(Warnings));
            OnPropertyChanged(nameof(ShowPlainRow));
            OnPropertyChanged(nameof(ShowStatusRow));
        }
    }

    public bool HasDuplicateWarning => Enabled && _duplicateCopies.Count > 1;
    // Compatibility warnings describe the mod itself and must remain visible
    // before selection. Ordinary conflict warnings describe the current
    // selection and are shown only for enabled mods.
    public bool HasConflictWarning =>
        (Enabled && _conflictWarnings.Count > 0) || _compatibilityWarnings.Count > 0;
    public bool InWarning => Mod.GetValidation().Status == ValidationStatus.Warning || HasDuplicateWarning || HasConflictWarning;
    public bool InError   => Mod.GetValidation().Status == ValidationStatus.Invalid;
    public bool IsValid   => Mod.GetValidation().Status == ValidationStatus.Valid;

    public string Warnings
    {
        get
        {
            var warnings = Mod.GetValidation().Warnings.Select(w => w.Message).ToList();
            if (HasDuplicateWarning)
            {
                var copies = string.Join("\r\n", _duplicateCopies
                    .Select(copy =>
                    {
                        var marker = ReferenceEquals(copy, Mod) ? "[selected] " : "";
                        return $"• {marker}{copy.GetVersion()} — {copy.GetSourcePath()}";
                    }));
                warnings.Add(string.Format(Texts.GUIModDuplicateCopies, copies));
            }
            warnings.AddRange(_conflictWarnings);
            warnings.AddRange(_compatibilityWarnings);
            return string.Join("\r\n", warnings);
        }
    }
    public string Errors   => string.Join("\r\n", Mod.GetValidation().Errors.Select(w => w.Message));

    public void SetDuplicateCopies(IReadOnlyList<IMod> copies)
    {
        _duplicateCopies = copies;
        OnPropertyChanged(nameof(HasDuplicateWarning));
        OnPropertyChanged(nameof(HasConflictWarning));
        OnPropertyChanged(nameof(InWarning));
        OnPropertyChanged(nameof(Warnings));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    public void SetConflictWarnings(IReadOnlyList<string> warnings)
    {
        _conflictWarnings = warnings;
        OnPropertyChanged(nameof(HasConflictWarning));
        OnPropertyChanged(nameof(InWarning));
        OnPropertyChanged(nameof(Warnings));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    public void SetCompatibilityWarnings(IReadOnlyList<string> warnings)
    {
        _compatibilityWarnings = warnings;
        OnPropertyChanged(nameof(HasConflictWarning));
        OnPropertyChanged(nameof(InWarning));
        OnPropertyChanged(nameof(Warnings));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    // ── Install outcome ───────────────────────────────────────────────────────

    private ModInstallState _installState = ModInstallState.None;
    private bool _installDetailIsSuccessMessage;

    // What the expander says about the outcome: "Installed successfully." or
    // the skip reasons
    public string InstallDetail { get; private set; } = "";

    public bool WasInstalled      => _installState is ModInstallState.Installed or ModInstallState.AlreadyInstalled;
    public bool WasAlreadyInstalled => _installState == ModInstallState.AlreadyInstalled;
    public string InstallStatusTooltip => WasAlreadyInstalled ? Texts.GUIModAlreadyInstalled : Texts.GUIModInstalled;
    public bool WasSkipped        => _installState == ModInstallState.Skipped;
    public bool WasFailed         => _installState == ModInstallState.Failed;
    public bool HasInstallOutcome => _installState != ModInstallState.None;

    // A skipped mod's reasons also land as validation errors; the red X and
    // InstallDetail already carry them, so the error triangle and error text
    // stand down while the skip is showing
    public bool ShowErrorIcon => InError && !WasSkipped;

    // The plain checkbox row is for a valid mod with nothing to report; any
    // validation message or install outcome swaps in the expander
    public bool ShowPlainRow  => IsValid && !HasInstallOutcome && !HasDuplicateWarning && !InWarning;
    public bool ShowStatusRow => !ShowPlainRow;

    public void SetInstallOutcome(ModInstallState state, string detail = "")
    {
        _installState = state;
        InstallDetail = detail;
        _installDetailIsSuccessMessage = state == ModInstallState.Installed;
        OnPropertyChanged(nameof(WasInstalled));
        OnPropertyChanged(nameof(WasAlreadyInstalled));
        OnPropertyChanged(nameof(InstallStatusTooltip));
        OnPropertyChanged(nameof(WasSkipped));
        OnPropertyChanged(nameof(WasFailed));
        OnPropertyChanged(nameof(HasInstallOutcome));
        OnPropertyChanged(nameof(InstallDetail));
        OnPropertyChanged(nameof(ShowErrorIcon));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    public void SetAlreadyInstalled(bool value)
    {
        if (value && _installState == ModInstallState.None)
        {
            _installState = ModInstallState.AlreadyInstalled;
            InstallDetail = Texts.GUIModAlreadyInstalled;
        }
        else if (!value && _installState == ModInstallState.AlreadyInstalled)
        {
            _installState = ModInstallState.None;
            InstallDetail = "";
        }
        else return;

        OnPropertyChanged(nameof(WasInstalled));
        OnPropertyChanged(nameof(WasAlreadyInstalled));
        OnPropertyChanged(nameof(InstallStatusTooltip));
        OnPropertyChanged(nameof(HasInstallOutcome));
        OnPropertyChanged(nameof(InstallDetail));
        OnPropertyChanged(nameof(ShowPlainRow));
        OnPropertyChanged(nameof(ShowStatusRow));
    }

    // An install can add validation messages (a skipped mod's reasons land as
    // errors); the expander re-reads them when told
    public void RefreshValidation()
    {
        OnPropertyChanged(nameof(InWarning));
        OnPropertyChanged(nameof(HasDuplicateWarning));
        OnPropertyChanged(nameof(HasConflictWarning));
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
        else if (WasAlreadyInstalled)
        {
            InstallDetail = Texts.GUIModAlreadyInstalled;
            OnPropertyChanged(nameof(InstallDetail));
        }
        OnPropertyChanged(nameof(InstallStatusTooltip));
        // Validation state does not change merely because the display
        // language changed. Avoid notifying every status binding here; with
        // a large mod list those redundant notifications force Avalonia to
        // measure and arrange every row repeatedly.
        OnPropertyChanged(nameof(Full));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(UpdateTooltip));
        OnPropertyChanged(nameof(Warnings));
    }

    public void RevalidateForLocalization()
    {
        Mod.Validate();
        ModInstaller.ValidateMods(new List<IMod> { Mod });
    }

    public string Full => string.Format(Resources.GUIModByAuthorWithVersion,
        Mod.GetDisplayName(Localization.LanguageCode), Mod.GetAuthor(), Mod.GetVersion());

    public string Description => Mod.GetDisplayDescription(Localization.LanguageCode);

    public string UpdateTooltip =>
        LatestVersion is null
            ? Texts.GUIUpdateMod
            : $"{Texts.GUIUpdateMod}: v{LatestVersion}";

    [RelayCommand]
    private void OpenUpdateUrl()
    {
        var url = UpdateDownloadUrl ?? Mod.GetDownloadUrl();
        if (!ExternalUrl.IsAllowed(url)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = url,
            UseShellExecute = true
        });
    }
}
