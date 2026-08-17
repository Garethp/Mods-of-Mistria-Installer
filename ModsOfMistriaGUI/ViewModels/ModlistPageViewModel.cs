using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaGUI.Services;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Garethp.ModsOfMistriaInstallerLib.Worker;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using UpdateChecker = Garethp.ModsOfMistriaInstallerLib.UpdateChecker;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public partial class ModlistPageViewModel : PageViewBase
{
    private int _modlistLoadInProgress;
    private int _modlistReloadRequested;
    private DispatcherTimer? _installUiHeartbeat;
    private long _lastHeartbeatTimestamp;
    private readonly Settings _settings;
    private ProfileManager? _profileManager;
    private int _localizationRefreshVersion;
    private int _conflictRefreshVersion;

    // True when in-GUI state differs from what is saved in the current profile
    private bool _isDirty;
    // Suppresses dirty-marking during programmatic enabled-state changes (profile apply)
    private bool _suppressDirty;
    // Prevents re-entrant cascades when auto-enabling/disabling dependents
    private bool _cascading;

    private string? _archiveStatusKey;
    private int _archiveStatusModCount;

    public ModlistPageViewModel(Settings settings)
    {
        _settings = settings;
        SetLanguageCommand = new RelayCommand<string?>(SetLanguage);
        Localization.LanguageChanged += OnLocalizationChanged;
        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.LaunchGameDirectly))
                OnPropertyChanged(nameof(LaunchGameDirectly));
            Task.Run(UpdateModlist);
        };
        Task.Run(UpdateModlist);
    }

    public IRelayCommand<string?> SetLanguageCommand { get; }

    private void SetLanguage(string? languageCode)
    {
        _settings.UiLanguage = string.IsNullOrWhiteSpace(languageCode) ? "system" : languageCode;
        Localization.SetLanguage(_settings.UiLanguage);
    }

    private void OnLocalizationChanged(object? sender, EventArgs e)
    {
        RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        var stopwatch = Stopwatch.StartNew();
        var refreshVersion = Interlocked.Increment(ref _localizationRefreshVersion);
        var modsNeedingValidation = Mods.Where(model => model.NeedsLocalizedValidation).ToList();

        GreetingText = isAprilFools ? Resources.GUIGreetingText_April : Resources.GUIGreetingText;
        InstallButtonText = isAprilFools ? Resources.GUIInstallButtonText_April : Resources.GUIInstallButtonText;
        InstallInProgressText = isAprilFools ? Resources.GUIInstallInProgress_April : Resources.GUIInstallInProgress;
        NoModsToInstallText = isAprilFools ? Resources.GUINoModsToInstall_April : Resources.GUINoModsToInstall;
        ModsWillBeInstalledText = isAprilFools ? Resources.GUIModsWillBeInstalled_April : Resources.GUIModsWillBeInstalled;
        RefreshCachedArchiveStatusText();
        var modelRefreshStopwatch = Stopwatch.StartNew();
        foreach (var model in Mods)
            model.RefreshLocalizedText();
        PerformanceDiagnostics.Log($"Language refresh: mod row notifications={modelRefreshStopwatch.ElapsedMilliseconds} ms, mods={Mods.Count}");

        var uiRefreshMilliseconds = stopwatch.ElapsedMilliseconds;
        if (modsNeedingValidation.Count == 0)
        {
            PerformanceDiagnostics.Log($"Language refresh: UI={uiRefreshMilliseconds} ms, validation=0 ms, mods={Mods.Count}");
            return;
        }

        _ = Task.Run(() =>
        {
            var validationStopwatch = Stopwatch.StartNew();
            foreach (var model in modsNeedingValidation)
                model.RevalidateForLocalization();

            var validationMilliseconds = validationStopwatch.ElapsedMilliseconds;
            Dispatcher.UIThread.Post(() =>
            {
                if (refreshVersion != Volatile.Read(ref _localizationRefreshVersion)) return;

                foreach (var model in modsNeedingValidation)
                    model.RefreshValidation();

                PerformanceDiagnostics.Log($"Language refresh: UI={uiRefreshMilliseconds} ms, validation={validationMilliseconds} ms, mods={Mods.Count}, revalidated={modsNeedingValidation.Count}");
            });
        });
    }

    private void RefreshCachedArchiveStatusText()
    {
        if (_archiveStatusKey is null) return;
        ArchiveStatus = _archiveStatusKey == "GUIArchiveMatch"
            ? string.Format(Localized(_archiveStatusKey), _archiveStatusModCount)
            : Localized(_archiveStatusKey);
    }

    public bool LaunchGameDirectly
    {
        get => _settings.LaunchGameDirectly;
        set => _settings.LaunchGameDirectly = value;
    }

    // ── Profile management ────────────────────────────────────────────────────────

    public ObservableCollection<string> Profiles { get; } = [];

    // Starts empty so the first profile refresh raises a notification even when
    // the clean-folder fallback profile is Default; otherwise the ComboBox can
    // contain Default while rendering no selected item.
    [ObservableProperty] private string _currentProfile = "";

    [RelayCommand]
    private async Task SwitchProfile(string profileName)
    {
        if (profileName == CurrentProfile) return;

        if (_isDirty && _profileManager is not null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Texts.GUIConfirmSaveProfileTitle,
                string.Format(Texts.GUIConfirmSaveProfileMessage, CurrentProfile),
                ButtonEnum.YesNoCancel);
            var result = await box.ShowAsync();

            if (result == ButtonResult.Cancel) return;
            if (result == ButtonResult.Yes)
                SaveCurrentProfileState();
        }

        _profileManager?.SwitchProfile(profileName);
        CurrentProfile = profileName;
        ApplyProfileToMods();    // sets _isDirty = false internally
    }

    [RelayCommand]
    private async Task CreateProfile()
    {
        var name = string.Format(Texts.GUIProfileName, Profiles.Count + 1);
        _profileManager?.CreateProfile(name);
        _profileManager?.SwitchProfile(name);
        CurrentProfile = name;
        RefreshProfileList();
        ApplyProfileToMods();
    }

    [RelayCommand]
    private async Task DeleteCurrentProfile()
    {
        if (CurrentProfile == "Default") return;

        var box = MessageBoxManager.GetMessageBoxStandard(
            Texts.GUIConfirmDeleteProfileTitle,
            string.Format(Texts.GUIConfirmDeleteProfileMessage, CurrentProfile),
            ButtonEnum.YesNo);
        var result = await box.ShowAsync();
        if (result != ButtonResult.Yes) return;

        _profileManager?.DeleteProfile(CurrentProfile);
        CurrentProfile = "Default";
        RefreshProfileList();
        ApplyProfileToMods();
    }

    public void SaveCurrentProfileState()
    {
        if (_profileManager is null) return;
        var enabled   = Mods.Where(m => m.Enabled).Select(m => m.Mod.GetId()).ToList();
        var enabledSources = Mods.Where(m => m.Enabled)
            .Select(m => DuplicateModDetector.NormalizeSource(m.Mod.GetSourcePath())).ToList();
        var loadOrder = Mods.Select(m => m.Mod.GetId()).ToList();
        _profileManager.SaveCurrentProfile(enabled, loadOrder, enabledSources);
        _isDirty = false;
    }

    private void RefreshProfileList()
    {
        var names = _profileManager?.GetProfileNames() ?? ["Default"];
        var active = _profileManager?.CurrentProfileName ?? "Default";
        var selected = names.Contains(active) ? active : "Default";

        // Clearing the collection clears ComboBox.SelectedItem. Resetting the
        // backing value first is important when the same profile remains
        // active (especially Default), otherwise no property notification is
        // raised and the ComboBox stays visually unselected.
        CurrentProfile = "";
        Profiles.Clear();
        foreach (var n in names) Profiles.Add(n);
        CurrentProfile = selected;
    }

    private void ApplyProfileToMods()
    {
        if (_profileManager is null || Mods.Count == 0) return;

        _suppressDirty = true;
        try
        {
            var (enabledIds, loadOrder) = _profileManager.GetCurrentProfile();

            // If profile has never been saved (both empty), default to all enabled
            var allMods    = Mods.Select(m => m.Mod).ToList();
            var duplicateCopies = BuildDuplicateCopyMap(allMods);
            var enabledSources = _profileManager.GetCurrentProfileEnabledSources()
                .Select(DuplicateModDetector.NormalizeSource)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var freshProfile = enabledIds.Count == 0 && loadOrder.Count == 0;
            var enabledSet = freshProfile
                ? allMods.Select(m => m.GetId()).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : enabledIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (enabledSources.Count == 0)
                enabledSources = DefaultDuplicateSources(allMods, duplicateCopies, enabledSet, freshProfile);

            var sorted = ProfileManager.SortByLoadOrder(allMods, loadOrder);

            var newModels = sorted.Select((mod, idx) =>
            {
                // Match the physical mod object, not only its logical ID. Two
                // copies can intentionally share the same author/name ID.
                var model = Mods.FirstOrDefault(m => ReferenceEquals(m.Mod, mod))
                            ?? new ModModel(mod);
                if (duplicateCopies.TryGetValue(DuplicateModDetector.NormalizeSource(mod.GetSourcePath()), out var copies))
                    model.SetDuplicateCopies(copies);
                model.Enabled = IsProfileSelected(mod, enabledSet, enabledSources, duplicateCopies);
                model.Position = idx + 1;
                return model;
            }).ToList();

            Mods.Clear();
            foreach (var m in newModels) Mods.Add(m);
        }
        finally
        {
            _suppressDirty = false;
            _isDirty = false;
            RefreshArchiveStatus();
            InstallModsCommand.NotifyCanExecuteChanged();
        }
    }

    // ── Load order ────────────────────────────────────────────────────────────────

    public void MoveMod(ModModel draggedMod, ModModel targetMod, bool insertBeforeTarget)
    {
        var sourceIndex = Mods.IndexOf(draggedMod);
        var targetIndex = Mods.IndexOf(targetMod);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        // ObservableCollection.Move expects the final index after the source item has
        // been removed, so a downward move needs to account for that removal.
        var destinationIndex = insertBeforeTarget ? targetIndex : targetIndex + 1;
        if (sourceIndex < destinationIndex) destinationIndex--;
        if (sourceIndex == destinationIndex) return;

        Mods.Move(sourceIndex, destinationIndex);
        RefreshPositions();
        _isDirty = true;
    }

    private void RefreshPositions()
    {
        for (var i = 0; i < Mods.Count; i++)
            Mods[i].Position = i + 1;
    }

    // When a mod is enabled, walk its requirements and enable them transitively.
    // Returns requirements that could not be found in the current mod list.
    // _cascading is already true when this is called, so their PropertyChanged won't re-enter.
    private List<ModRequirement> EnableDependenciesOf(ModModel mod)
    {
        var missing = new List<ModRequirement>();
        foreach (var req in mod.Mod.GetRequirements())
        {
            var dep = Mods.FirstOrDefault(m => m.Mod.GetId() == req.GetId());
            if (dep is null)
            {
                missing.Add(req);
                continue;
            }
            if (dep.Enabled) continue;
            dep.Enabled = true;
            missing.AddRange(EnableDependenciesOf(dep));
        }
        return missing;
    }

    // When a mod is disabled, find every enabled mod that (directly or transitively)
    // requires it and disable those too.
    private void DisableDependentsOf(ModModel mod)
    {
        var modId = mod.Mod.GetId();
        foreach (var other in Mods.ToList())
        {
            if (!other.Enabled) continue;
            if (!other.Mod.GetRequirements().Any(r => r.GetId() == modId)) continue;
            other.Enabled = false;
            DisableDependentsOf(other);
        }
    }

    // ── Mod list loading ──────────────────────────────────────────────────────────

    private static Dictionary<string, IReadOnlyList<IMod>> BuildDuplicateCopyMap(IEnumerable<IMod> mods)
    {
        var map = new Dictionary<string, IReadOnlyList<IMod>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in DuplicateModDetector.Find(mods))
        {
            foreach (var copy in group.Copies)
                map[DuplicateModDetector.NormalizeSource(copy.GetSourcePath())] = group.Copies;
        }
        return map;
    }

    private static HashSet<string> DefaultDuplicateSources(
        IEnumerable<IMod> mods,
        Dictionary<string, IReadOnlyList<IMod>> duplicateCopies,
        HashSet<string> enabledIds,
        bool freshProfile)
    {
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            var source = DuplicateModDetector.NormalizeSource(mod.GetSourcePath());
            if (!duplicateCopies.TryGetValue(source, out var copies) ||
                (ReferenceEquals(copies[0], mod) && (freshProfile || enabledIds.Contains(mod.GetId()))))
                sources.Add(source);
        }
        return sources;
    }

    private static bool IsProfileSelected(
        IMod mod,
        HashSet<string> enabledIds,
        HashSet<string> enabledSources,
        Dictionary<string, IReadOnlyList<IMod>> duplicateCopies)
    {
        var source = DuplicateModDetector.NormalizeSource(mod.GetSourcePath());
        if (duplicateCopies.ContainsKey(source))
            return enabledSources.Contains(source);
        return enabledIds.Contains(mod.GetId());
    }

    private void UpdateModlist() => _ = UpdateModlistAsync(false);

    private void UpdateModlist(bool force) => _ = UpdateModlistAsync(force);

    private async Task UpdateModlistAsync(bool force)
    {
        if (Interlocked.Exchange(ref _modlistLoadInProgress, 1) != 0)
        {
            if (force) Volatile.Write(ref _modlistReloadRequested, 1);
            return;
        }

        try
        {
            var snapshot = await Dispatcher.UIThread.InvokeAsync(() =>
                new ModlistSnapshot(_settings.MistriaLocation, _settings.ModsLocation, MistriaLocation, ModsLocation));

            if (!force && snapshot.MistriaLocation == snapshot.CurrentMistriaLocation &&
                snapshot.ModsLocation == snapshot.CurrentModsLocation)
                return;

            var result = await Task.Run(() => LoadModlist(snapshot.MistriaLocation, snapshot.ModsLocation));
            await Dispatcher.UIThread.InvokeAsync(() => ApplyModlist(result));
        }
        finally
        {
            Volatile.Write(ref _modlistLoadInProgress, 0);
            if (Interlocked.Exchange(ref _modlistReloadRequested, 0) != 0)
                _ = UpdateModlistAsync(true);
        }
    }

    private ModlistLoadResult LoadModlist(string mistriaLocation, string modsLocation)
    {
        var totalStopwatch = Stopwatch.StartNew();
        ProfileManager? profileManager = null;
        List<IMod> rawMods = [];
        var discoveryMilliseconds = 0L;
        var validationMilliseconds = 0L;
        var duplicateCopies = new Dictionary<string, IReadOnlyList<IMod>>(StringComparer.OrdinalIgnoreCase);
        List<IMod> orderedMods = [];
        HashSet<string> enabledIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> enabledSources = new(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(modsLocation))
        {
            try { profileManager = new ProfileManager(modsLocation); }
            catch { profileManager = null; }

            var discoveryStopwatch = Stopwatch.StartNew();
            rawMods = MistriaLocator.GetMods(mistriaLocation, modsLocation);
            discoveryMilliseconds = discoveryStopwatch.ElapsedMilliseconds;

            var validationStopwatch = Stopwatch.StartNew();
            ModInstaller.ValidateMods(rawMods);
            validationMilliseconds = validationStopwatch.ElapsedMilliseconds;
            duplicateCopies = BuildDuplicateCopyMap(rawMods);

            if (profileManager is not null)
            {
                var (profileEnabledIds, loadOrder) = profileManager.GetCurrentProfile();
                var resolvedEnabled = profileEnabledIds.Count == 0 && loadOrder.Count == 0
                    ? rawMods.Select(m => m.GetId()).ToList()
                    : ProfileManager.ResolveEnabledWithDeps(rawMods, profileEnabledIds);
                enabledIds = resolvedEnabled.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var configuredSources = profileManager.GetCurrentProfileEnabledSources()
                    .Select(DuplicateModDetector.NormalizeSource)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                enabledSources = configuredSources.Count > 0
                    ? configuredSources
                    : DefaultDuplicateSources(rawMods, duplicateCopies, enabledIds, false);
                orderedMods = ProfileManager.SortByLoadOrder(rawMods, loadOrder);
            }
            else
            {
                var allDisabled = rawMods.All(m => !m.IsInstalled());
                if (allDisabled) rawMods.ForEach(m => m.SetInstalled(true));
                orderedMods = rawMods;
            }
        }

        PerformanceDiagnostics.Log($"Modlist load: total={totalStopwatch.ElapsedMilliseconds} ms, discovery={discoveryMilliseconds} ms, validation={validationMilliseconds} ms, mods={orderedMods.Count}");
        return new ModlistLoadResult(mistriaLocation, modsLocation, profileManager, orderedMods, enabledIds, enabledSources, duplicateCopies);
    }

    private void ApplyModlist(ModlistLoadResult result)
    {
        {
            MistriaLocation = result.MistriaLocation;
            ModsLocation = result.ModsLocation;
            _profileManager = result.ProfileManager;
            Mods.Clear();

            for (var i = 0; i < result.OrderedMods.Count; i++)
            {
                var mod = result.OrderedMods[i];
                var model = new ModModel(mod)
                {
                    Enabled = IsProfileSelected(mod, result.EnabledIds, result.EnabledSources, result.DuplicateCopies),
                    Position = i + 1
                };
                if (result.DuplicateCopies.TryGetValue(
                        DuplicateModDetector.NormalizeSource(mod.GetSourcePath()), out var copies))
                    model.SetDuplicateCopies(copies);
                AttachModPropertyHandlers(model);
                Mods.Add(model);
            }

            RefreshProfileList();
            _isDirty = false;
            var modSnapshot = Mods.ToList();
            _ = Task.Run(() => CheckModUpdatesAsync(modSnapshot));

            InstallStatus = "";
            RefreshGameReady();
            InstallModsCommand.NotifyCanExecuteChanged();
            UnInstallModsCommand.NotifyCanExecuteChanged();
            if (MistriaLocation.Equals("")) InstallStatus = Resources.GUICouldNotFindMistria;
            else if (ModsLocation.Equals("")) InstallStatus = Resources.GUICouldNotFindMods;
            else if (Mods.Count == 0) InstallStatus = NoModsToInstallText;
            RefreshSelectedModConflicts();
        }
    }

    private void AttachModPropertyHandlers(ModModel model)
    {
        model.PropertyChanged += async (sender, e) =>
        {
            if (e.PropertyName != nameof(ModModel.Enabled) || _suppressDirty) return;
            _isDirty = true;
            RefreshArchiveStatus();
            RefreshSelectedModConflicts();
            if (_cascading) return;
            _cascading = true;
            List<ModRequirement> missing;
            try
            {
                var changed = (ModModel)sender!;
                if (changed.Enabled) missing = EnableDependenciesOf(changed);
                else
                {
                    DisableDependentsOf(changed);
                    missing = [];
                }
            }
            finally { _cascading = false; }
            InstallModsCommand.NotifyCanExecuteChanged();
            UnInstallModsCommand.NotifyCanExecuteChanged();

            if (missing.Count == 0) return;

            var lines = string.Join("\n\n", missing.Select(r =>
            {
                var line = $"• \"{r.Name}\" by {r.Author}";
                if (!string.IsNullOrEmpty(r.DownloadUrl)) line += $"\n  {r.DownloadUrl}";
                return line;
            }));
            var urls = missing.Where(r => !string.IsNullOrEmpty(r.DownloadUrl))
                .Select(r => r.DownloadUrl!).ToList();

            if (urls.Count > 0)
            {
                var ask = await MessageBoxManager.GetMessageBoxStandard(
                    Texts.GUIMissingRequirementsTitle,
                    string.Format(Texts.GUIMissingRequirementsMessage, lines),
                    ButtonEnum.YesNo).ShowAsync();
                if (ask == ButtonResult.Yes)
                {
                    var urlList = string.Join("\n", urls.Select(u => $"• {u}"));
                    var confirm = await MessageBoxManager.GetMessageBoxStandard(
                        Texts.GUIOpenExternalLinksTitle,
                        string.Format(Texts.GUIOpenExternalLinksMessage, urlList),
                        ButtonEnum.YesNo).ShowAsync();
                    if (confirm == ButtonResult.Yes)
                        foreach (var url in urls.Where(ExternalUrl.IsAllowed))
                            System.Diagnostics.Process.Start(new ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                }
            }
            else
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    Texts.GUIMissingRequirementsTitle,
                    string.Format(Texts.GUIMissingRequirementsManual, lines),
                    ButtonEnum.Ok).ShowAsync();
            }
        };
    }

    // This runs after loading, selection changes, and archive operations. The
    // file scan is off the UI thread and never touches the game archive.
    private void RefreshSelectedModConflicts()
    {
        var refreshVersion = Interlocked.Increment(ref _conflictRefreshVersion);
        var models = Mods.ToList();
        var selectedModels = models.Where(m => m.Enabled).ToList();
        var selected = selectedModels.Select(m => m.Mod).ToList();

        // Compatibility checks run for every discovered mod when the list is
        // loaded and are refreshed after selection changes. They are cheap
        // metadata/code-signature checks and do not touch the game archive.
        _ = Task.Run(() =>
        {
            var detected = new Dictionary<ModModel, IReadOnlyList<string>>();
            foreach (var model in models)
            {
                try { detected[model] = LegacyGameCompatibilityDetector.Find(model.Mod); }
                catch (Exception exception)
                {
                    Logger.Log($"Modlist compatibility check skipped for {model.Mod.GetId()}: {exception.Message}");
                    detected[model] = [];
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (refreshVersion != Volatile.Read(ref _conflictRefreshVersion)) return;
                foreach (var model in models)
                {
                    if (detected.TryGetValue(model, out var findings) && findings.Count > 0)
                    {
                        // Legacy signatures are advisory. They must be visible
                        // before installation but must never disable a mod.
                        model.SetCompatibilityWarnings([string.Join("\r\n", new[]
                        {
                            Texts.GUIModLegacyGamePatch,
                            $"  • {string.Join("\r\n  • ", findings)}"
                        })]);
                    }
                    else if (model.Mod.GetAllFiles(".gml").Count > 0 &&
                             model.Mod.GetRequiredHooks().Count == 0)
                    {
                        model.SetCompatibilityWarnings([Texts.GUIModLegacyGml]);
                    }
                    else
                    {
                        model.SetCompatibilityWarnings([]);
                    }
                }
            });
        });

        _ = Task.Run(() =>
        {
            IReadOnlyList<ModConflict> conflicts;
            try { conflicts = ModConflictDetector.Find(selected); }
            catch (Exception exception)
            {
                Logger.Log($"Selection conflict check skipped: {exception.Message}");
                conflicts = [];
            }

            IReadOnlyList<ModFileConflict> fileConflicts;
            try { fileConflicts = ModFileConflictDetector.Find(selected); }
            catch (Exception exception)
            {
                Logger.Log($"Selection file-conflict check skipped: {exception.Message}");
                fileConflicts = [];
            }

            IReadOnlyList<ModHotkeyConflict> hotkeyConflicts;
            try { hotkeyConflicts = HotkeyConflictDetector.Find(selected); }
            catch (Exception exception)
            {
                Logger.Log($"Selection hotkey-conflict check skipped: {exception.Message}");
                hotkeyConflicts = [];
            }

            foreach (var conflict in fileConflicts)
                Logger.Log($"Selection file conflict: {conflict.Kind}; {conflict.Path}; mods={string.Join(", ", conflict.ModIds)}");

            Dispatcher.UIThread.Post(() =>
            {
                if (refreshVersion != Volatile.Read(ref _conflictRefreshVersion)) return;

                foreach (var model in models)
                {
                    var warnings = conflicts
                        .Where(conflict => conflict.ModIds.Contains(model.Mod.GetId(), StringComparer.OrdinalIgnoreCase))
                        .Select(conflict => string.Format(Texts.GUIModConflicts, $"• {conflict.Description}"))
                        .ToList();
                    var sharedPaths = fileConflicts
                        .Where(conflict => conflict.ModIds.Contains(model.Mod.GetId(), StringComparer.OrdinalIgnoreCase))
                        .Select(conflict => FormatFileConflict(conflict, selected))
                        .ToList();
                    if (sharedPaths.Count > 0)
                        warnings.Add(string.Format(Texts.GUIModFileConflicts, string.Join("\r\n", sharedPaths)));
                    var hotkeys = hotkeyConflicts
                        .Where(conflict => conflict.Usages.Any(usage =>
                            usage.ModId.Equals(model.Mod.GetId(), StringComparison.OrdinalIgnoreCase)))
                        .Select(conflict => FormatHotkeyConflict(conflict, selected))
                        .ToList();
                    if (hotkeys.Count > 0)
                        warnings.Add(string.Format(Texts.GUIModHotkeyConflicts, string.Join("\r\n", hotkeys)));
                    model.SetConflictWarnings(warnings);
                }
            });
        });
    }

    private string FormatFileConflict(ModFileConflict conflict, IReadOnlyList<IMod> selected)
    {
        var description = conflict.Kind switch
        {
            ModFileConflictKind.HardReplacement => Texts.GUIFileConflictReplacement,
            ModFileConflictKind.MergeableMetadata => Texts.GUIFileConflictMerge,
            ModFileConflictKind.SharedLocalization => Texts.GUIFileConflictLocalization,
            _ => Texts.GUIFileConflictShared
        };
        var owners = selected
            .Where(mod => conflict.ModIds.Contains(mod.GetId(), StringComparer.OrdinalIgnoreCase))
            .Select(mod => $"{mod.GetName()} v{mod.GetVersion()} [{mod.GetSourcePath()}]")
            .ToList();
        return $"• {conflict.Path}\r\n  {description}\r\n  {string.Join("\r\n  ", owners)}";
    }

    private string FormatHotkeyConflict(ModHotkeyConflict conflict, IReadOnlyList<IMod> selected)
    {
        var owners = selected
            .Where(mod => conflict.Usages.Any(usage =>
                usage.ModId.Equals(mod.GetId(), StringComparison.OrdinalIgnoreCase)))
            .Select(mod =>
            {
                var usage = conflict.Usages.First(usage =>
                    usage.ModId.Equals(mod.GetId(), StringComparison.OrdinalIgnoreCase));
                var suffix = usage.Rebindable ? Texts.GUIHotkeyRebindable : "";
                return $"{mod.GetName()} v{mod.GetVersion()} [{mod.GetSourcePath()}]{suffix}";
            })
            .ToList();
        return $"• {conflict.Key}\r\n  {string.Join("\r\n  ", owners)}";
    }

    private sealed record ModlistSnapshot(
        string MistriaLocation,
        string ModsLocation,
        string CurrentMistriaLocation,
        string CurrentModsLocation);

    private sealed record ModlistLoadResult(
        string MistriaLocation,
        string ModsLocation,
        ProfileManager? ProfileManager,
        List<IMod> OrderedMods,
        HashSet<string> EnabledIds,
        HashSet<string> EnabledSources,
        Dictionary<string, IReadOnlyList<IMod>> DuplicateCopies);

    // Checks all mods for updates in parallel; each result updates the matching
    // ModModel on the UI thread so the update badge appears as responses arrive.
    private static async Task CheckModUpdatesAsync(List<ModModel> models)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = models.Select(async model =>
        {
            try
            {
                var info = await UpdateChecker.CheckAsync(model.Mod);
                if (info?.IsNewer != true) return;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    model.UpdateAvailable    = true;
                    model.LatestVersion      = info.LatestVersion;
                    model.UpdateDownloadUrl  = info.DownloadUrl;
                });
            }
            catch { /* network failures are silent */ }
        });
        await Task.WhenAll(tasks);
        PerformanceDiagnostics.Log($"Mod update checks: {stopwatch.ElapsedMilliseconds} ms, mods={models.Count}");
    }

    // ── Observable properties ─────────────────────────────────────────────────────

    [ObservableProperty] private string _installStatus = "";

    // Describes the archive on disk separately from the selected profile.
    [ObservableProperty] private string _archiveStatus = "";

    private static readonly bool isAprilFools = DateTime.Today.Month == 4 && DateTime.Today.Day == 1;

    [ObservableProperty] private string _greetingText =
        isAprilFools ? Resources.GUIGreetingText_April : Resources.GUIGreetingText;

    [ObservableProperty] private string _installButtonText =
        isAprilFools ? Resources.GUIInstallButtonText_April : Resources.GUIInstallButtonText;

    [ObservableProperty] private string _installInProgressText =
        isAprilFools ? Resources.GUIInstallInProgress_April : Resources.GUIInstallInProgress;

    [ObservableProperty] private string _noModsToInstallText =
        isAprilFools ? Resources.GUINoModsToInstall_April : Resources.GUINoModsToInstall;

    [ObservableProperty] private string _modsWillBeInstalledText =
        isAprilFools ? Resources.GUIModsWillBeInstalled_April : Resources.GUIModsWillBeInstalled;

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [ObservableProperty] private string _modsLocation = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnInstallModsCommand))]
    [ObservableProperty] private string _mistriaLocation = "";

    [ObservableProperty] private string _exception = "";

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnInstallModsCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [NotifyPropertyChangedFor(nameof(CanReorderMods))]
    [NotifyPropertyChangedFor(nameof(CanChangeModSelection))]
    [ObservableProperty] private bool _isInstalling;

    // Load order is part of the same profile state as the checkboxes. Do not let a
    // drag operation alter it while an archive operation is using that state.
    public bool CanReorderMods => !IsInstalling;

    // Keep checkbox changes out of an in-progress archive operation as well.
    public bool CanChangeModSelection => !IsInstalling;

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [ObservableProperty] private bool _installationNeedsRebuild;

    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [ObservableProperty] private bool _gameReady;

    public ObservableCollection<ModModel> Mods { get; } = [];

    // ── Commands ──────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        var duplicate = FindSelectedDuplicateGroup();
        if (duplicate is not null)
        {
            var message = Localized("GUIDuplicateModInstallBlocked");
            Exception = message;
            InstallStatus = message;
            return;
        }

        var hardConflict = FindSelectedHardReplacementConflict();
        if (hardConflict is not null)
        {
            var selected = Mods.Where(model => model.Enabled).Select(model => model.Mod).ToList();
            var message = Localized("GUIHardFileConflictBlocked") + "\n\n" + FormatFileConflict(hardConflict, selected);
            Exception = message;
            InstallStatus = message;
            return;
        }

        Exception = "";

        // Auto-save profile state before installing so load order is persisted
        SaveCurrentProfileState();

        // The icons describe the install that is about to run, not the last one
        foreach (var mod in Mods) mod.SetInstallOutcome(ModInstallState.None);

        InstallStatus = InstallInProgressText;
        IsInstalling  = true;
        StartInstallUiDiagnostics();
        _ = BackgroundInstall();
    }

    [RelayCommand]
    private async Task SaveLogFile()
    {
        var topLevel = App.TopLevel;
        if (topLevel is null) return;

        var logs  = Logger.GetLogs();
        var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = Resources.GUIPickLogFile,
            SuggestedFileName = $"aim-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            DefaultExtension  = "txt",
            FileTypeChoices   = [FilePickerFileTypes.TextPlain]
        });

        if (files is not null)
            await File.WriteAllTextAsync(files.Path.AbsolutePath, string.Join("\r\n", logs));
    }

    [RelayCommand]
    private void DismissException() => Exception = "";

    [RelayCommand]
    private void ReloadModlist()
    {
        Exception = "";
        UpdateModlist(true);
    }

    [RelayCommand]
    private void EnableAllMods()
    {
        foreach (var m in Mods) m.Enabled = true;
        _isDirty = true;
        RefreshArchiveStatus();
        InstallModsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void DisableAllMods()
    {
        foreach (var m in Mods) m.Enabled = false;
        _isDirty = true;
        RefreshArchiveStatus();
        InstallModsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void UnInstallMods()
    {
        Exception     = "";
        IsInstalling  = true;
        InstallStatus = Resources.GUIUninstallingText;

        _ = BackgroundUninstall();
    }

    private async Task BackgroundUninstall()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var request = new ArchiveWorkerRequest(
                "uninstall", MistriaLocation, ModsLocation, [], "", GateMode: "auto");
            await new ArchiveWorkerClient().RunAsync(
                request,
                status => Dispatcher.UIThread.Post(() =>
                {
                    if (IsInstalling) InstallStatus = status;
                }),
                CancellationToken.None);
            stopwatch.Stop();
            PerformanceDiagnostics.Log($"Uninstall completed: elapsed={stopwatch.ElapsedMilliseconds} ms");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StopInstallUiDiagnostics();
                IsInstalling  = false;
                InstallStatus = Resources.GUIUninstallCompleteText;
                GameReady = IsLaunchableGameInstallation(MistriaLocation);
                InstallationNeedsRebuild = true;
                _archiveStatusKey = "GUIArchiveNoInstallation";
                _archiveStatusModCount = 0;
                RefreshCachedArchiveStatusText();
                // Nothing is installed any more; the outcome icons are stale
                foreach (var mod in Mods) mod.SetInstallOutcome(ModInstallState.None);
                RefreshSelectedModConflicts();
            });
        }
        catch (Exception e)
        {
            var errorLogPath = WriteDiagnosticErrorLog("uninstall", e);
            Logger.Log($"{Resources.GUIUninstallFatalError}\r\n{e}");
            stopwatch.Stop();
            PerformanceDiagnostics.Log($"Uninstall failed: elapsed={stopwatch.ElapsedMilliseconds} ms, exception={e.GetType().Name}");

            // Keep the page recoverable. The archive transaction has already
            // aborted, so the user can inspect the error and retry.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StopInstallUiDiagnostics();
                IsInstalling  = false;
                InstallStatus = Resources.GUIUninstallFatalError;
                RefreshGameReady();
                Exception     = FormatErrorMessage(Resources.GUIUninstallFatalError, e, errorLogPath);
            });
        }
    }

    private async Task BackgroundInstall()
    {
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            var modsToInstall = Mods.Where(m => m.Enabled).Select(m => m.Mod).ToList();

            PerformanceDiagnostics.Log($"Install worker requested: mods={modsToInstall.Count}");
            var request = new ArchiveWorkerRequest(
                "install",
                MistriaLocation,
                ModsLocation,
                modsToInstall.Select(mod => mod.GetSourcePath()).ToArray(),
                "",
                GateMode: "auto");
            var result = await new ArchiveWorkerClient().RunAsync(
                request,
                status =>
                {
                    PerformanceDiagnostics.Log($"Worker phase: {status}");
                    if (PerformanceDiagnostics.SuppressInstallProgressUi)
                    {
                        PerformanceDiagnostics.Log("Install UI phase callback skipped by AIM_DIAGNOSTICS_NO_PROGRESS_UI");
                        return;
                    }
                    var queuedAt = Stopwatch.GetTimestamp();
                    Dispatcher.UIThread.Post(() =>
                    {
                        var delay = Stopwatch.GetElapsedTime(queuedAt).TotalMilliseconds;
                        PerformanceDiagnostics.Log($"Install UI phase callback: delay={delay:0} ms, thread={Environment.CurrentManagedThreadId}");
                        if (IsInstalling) InstallStatus = status;
                    });
                },
                CancellationToken.None);

            totalStopwatch.Stop();
            PerformanceDiagnostics.Log($"Install completed: elapsed={totalStopwatch.ElapsedMilliseconds} ms, installed={result.Installed.Length}, skipped={result.Skipped.Length}");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StopInstallUiDiagnostics();
                IsInstalling  = false;
                InstallStatus = result.Summary;
                GameReady = IsLaunchableGameInstallation(MistriaLocation);
                InstallationNeedsRebuild = false;
                _archiveStatusKey = "GUIArchiveMatch";
                _archiveStatusModCount = result.Installed.Length;
                RefreshCachedArchiveStatusText();

                // Checkmark for what landed, red X with the reasons for what
                // was skipped; a skipped mod's reasons also landed as
                // validation errors, so refresh the expander bindings too
                var installed = result.Installed.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var skipped   = result.Skipped.ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var mod in Mods)
                {
                    if (skipped.Contains(mod.Mod.GetId()))
                        mod.SetInstallOutcome(ModInstallState.Skipped, Resources.GUIModSkipped);
                    else if (installed.Contains(mod.Mod.GetId()))
                        mod.SetInstallOutcome(ModInstallState.Installed, Resources.GUIModInstalled);
                }
                RefreshSelectedModConflicts();
            });
        }
        catch (Exception e)
        {
            // Write the diagnostic first so its Recent AIM log contains only
            // the progress leading up to the failure, not a second copy of the
            // same full exception.
            var errorLogPath = WriteDiagnosticErrorLog("install", e);
            Logger.Log($"{Resources.GUIInstallFatalError}\r\n{e}");
            var failedModId = (e as ModInstallationException)?.ModId;
            totalStopwatch.Stop();
            PerformanceDiagnostics.Log($"Install failed: elapsed={totalStopwatch.ElapsedMilliseconds} ms, exception={e.GetType().Name}");

            // Keep the page recoverable. The archive transaction has already
            // aborted, so the user can disable the failing mod and retry.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StopInstallUiDiagnostics();
                if (!string.IsNullOrEmpty(failedModId))
                {
                    var failedMod = Mods.FirstOrDefault(m =>
                        string.Equals(m.Mod.GetId(), failedModId, StringComparison.OrdinalIgnoreCase));
                    failedMod?.SetInstallOutcome(
                        ModInstallState.Failed,
                        e.Message + "\r\n" + Texts.GUIErrorReason + GetRootCauseMessage(e));
                }

                IsInstalling  = false;
                InstallStatus = Resources.GUIInstallFatalError;
                RefreshGameReady();
                Exception     = FormatErrorMessage(Resources.GUIInstallFatalError, e, errorLogPath);
            });
        }
    }

    private void StartInstallUiDiagnostics()
    {
        if (!PerformanceDiagnostics.Enabled) return;
        _installUiHeartbeat?.Stop();
        _installUiHeartbeat = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _lastHeartbeatTimestamp = Stopwatch.GetTimestamp();
        _installUiHeartbeat.Tick += (_, _) =>
        {
            var now = Stopwatch.GetTimestamp();
            var gap = Stopwatch.GetElapsedTime(_lastHeartbeatTimestamp).TotalMilliseconds;
            _lastHeartbeatTimestamp = now;
            PerformanceDiagnostics.Log($"UI heartbeat: gap={gap:0} ms, installing={IsInstalling}, thread={Environment.CurrentManagedThreadId}, {PerformanceDiagnostics.ProcessMetrics()}");
        };
        _installUiHeartbeat.Start();
        PerformanceDiagnostics.Log("UI heartbeat started");
    }

    private void StopInstallUiDiagnostics()
    {
        if (_installUiHeartbeat is null) return;
        _installUiHeartbeat.Stop();
        _installUiHeartbeat = null;
        PerformanceDiagnostics.Log("UI heartbeat stopped");
    }

    private static string FormatErrorMessage(string heading, Exception exception, string? errorLogPath)
    {
        var rootCause = GetRootCauseMessage(exception);
        var message = heading + "\n" + exception.Message;
        if (!string.Equals(rootCause, exception.Message, StringComparison.Ordinal))
            message += "\n\n" + Localized("GUIErrorReason") + rootCause;

        if (!string.IsNullOrEmpty(errorLogPath))
        {
            message += $"\n\n{Localized("GUIErrorDetailsSaved")}\n{errorLogPath}";
        }

        return message;
    }

    private static string GetRootCauseMessage(Exception exception)
    {
        while (exception.InnerException is not null)
            exception = exception.InnerException;

        return exception.Message;
    }

    private static string? WriteDiagnosticErrorLog(string operation, Exception exception)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root = string.IsNullOrWhiteSpace(localAppData)
                ? AppContext.BaseDirectory
                : localAppData;
            var directory = Path.Combine(root, "AIM", "logs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"aim-error-{DateTime.Now:yyyyMMdd-HHmmssfff}.txt");
            var contents = $"AIM diagnostic error log\r\n" +
                           $"Timestamp (UTC): {DateTime.UtcNow:O}\r\n" +
                           $"Application: {AppInfo.DisplayVersion}\r\n" +
                           $"Operation: {operation}\r\n\r\n" +
                           "Exception:\r\n" + exception + "\r\n\r\n" +
                           "Recent AIM log:\r\n" + string.Join("\r\n", Logger.GetLogs());
            File.WriteAllText(path, contents, new System.Text.UTF8Encoding(false));
            Logger.Log($"Diagnostic error log written to: {path}");
            return path;
        }
        catch (Exception logException)
        {
            Logger.Log($"Could not write diagnostic error log: {logException.Message}");
            return null;
        }
    }

    private bool CanRemove() =>
        !MistriaLocation.Equals("") && !IsInstalling &&
        new AssetsStore(MistriaLocation).HasMomiInstallation();

    [RelayCommand(CanExecute = nameof(CanLaunchGame))]
    private void LaunchGame()
    {
        var executable = GameExecutableLocator.Find(MistriaLocation);

        if (_settings.LaunchGameDirectly && executable is not null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = MistriaLocation,
                    UseShellExecute = true
                });
                return;
            }
            catch
            {
                // Fall back to Steam if the direct executable cannot be started.
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppInfo.GameLaunchUri,
                UseShellExecute = true
            });
            return;
        }
        catch
        {
            // Fall back to the installed executable when Steam URI handling is
            // unavailable on the current desktop environment.
        }

        if (executable is null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = MistriaLocation,
                UseShellExecute = true
            });
        }
        catch { /* Launching the game must not crash MOMI. */ }
    }

    private bool CanLaunchGame() => GameReady && !IsInstalling;

    private void RefreshGameReady()
    {
        // Launching the game is independent from whether MOMI currently has
        // mods installed. After Uninstall, the pristine game archive should
        // still be launchable, so do not use HasMomiInstallation here.
        GameReady = IsLaunchableGameInstallation(MistriaLocation);
        RefreshArchiveStatus();
    }

    private static bool IsLaunchableGameInstallation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
            return false;

        if (!File.Exists(Path.Combine(location, "Maybe.toml")))
            return false;

        var unpackedAssets = Path.Combine(location, "assets");
        if (Directory.Exists(unpackedAssets))
            return true;

        var archivePath = Path.Combine(location, "assets.zip");
        // The archive contents are validated by the archive worker during the
        // transaction. Do not reopen and enumerate a potentially 600+ MB ZIP
        // on the UI thread merely to refresh the Play button after completion.
        return File.Exists(archivePath);
    }

    private void RefreshArchiveStatus()
    {
        if (string.IsNullOrWhiteSpace(MistriaLocation))
        {
            InstallationNeedsRebuild = false;
            _archiveStatusKey = "GUIArchiveNoGameArchive";
            _archiveStatusModCount = 0;
            RefreshCachedArchiveStatusText();
            return;
        }

        var store = new AssetsStore(MistriaLocation);
        if (!store.HasMomiInstallation())
        {
            foreach (var mod in Mods) mod.SetAlreadyInstalled(false);
            InstallationNeedsRebuild = true;
            _archiveStatusKey = "GUIArchiveNoInstallation";
            _archiveStatusModCount = 0;
            RefreshCachedArchiveStatusText();
            return;
        }

        RecordedInstallState? recorded;
        try { recorded = store.GetRecordedInstallState(); }
        catch
        {
            foreach (var mod in Mods) mod.SetAlreadyInstalled(false);
            InstallationNeedsRebuild = true;
            _archiveStatusKey = "GUIArchiveStateUnavailable";
            _archiveStatusModCount = 0;
            RefreshCachedArchiveStatusText();
            return;
        }

        if (recorded is null)
        {
            foreach (var mod in Mods) mod.SetAlreadyInstalled(false);
            InstallationNeedsRebuild = true;
            _archiveStatusKey = "GUIArchiveVersionsUnavailable";
            _archiveStatusModCount = 0;
            RefreshCachedArchiveStatusText();
            return;
        }

        // A folder and an archive can represent the same logical mod.  The
        // duplicate-copy warning handles that situation in the list, but the
        // archive status is keyed by logical ID and must therefore not throw
        // when multiple physical copies are present.
        var desired = Mods
            .Where(mod => mod.Enabled)
            .GroupBy(mod => mod.Mod.GetId(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Mod.GetVersion(),
                StringComparer.OrdinalIgnoreCase);
        var actual = recorded.Mods
            .GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Version,
                StringComparer.OrdinalIgnoreCase);

        foreach (var mod in Mods)
        {
            var alreadyInstalled = actual.TryGetValue(mod.Mod.GetId(), out var installedVersion) &&
                                    string.Equals(installedVersion, mod.Mod.GetVersion(), StringComparison.OrdinalIgnoreCase);
            mod.SetAlreadyInstalled(alreadyInstalled);
        }

        var matches = desired.Count == actual.Count &&
                      desired.All(pair => actual.TryGetValue(pair.Key, out var version) &&
                                          string.Equals(version, pair.Value, StringComparison.OrdinalIgnoreCase));

        _archiveStatusKey = matches ? "GUIArchiveMatch" : "GUIArchiveDifferent";
        _archiveStatusModCount = actual.Count;
        RefreshCachedArchiveStatusText();
        InstallationNeedsRebuild = !matches;
    }

    private static string Localized(string key) =>
        Resources.ResourceManager.GetString(key, Resources.Culture) ?? key;

    private DuplicateModGroup? FindSelectedDuplicateGroup()
    {
        var groups = DuplicateModDetector.Find(Mods.Select(model => model.Mod));
        return groups.FirstOrDefault(group =>
            group.Copies.Count(copy => Mods.Any(model =>
                ReferenceEquals(model.Mod, copy) && model.Enabled)) > 1);
    }

    private ModFileConflict? FindSelectedHardReplacementConflict()
    {
        var selected = Mods.Where(model => model.Enabled).Select(model => model.Mod).ToList();
        return ModFileConflictDetector.Find(selected)
            .FirstOrDefault(conflict => conflict.Kind == ModFileConflictKind.HardReplacement);
    }

    private bool CanInstall() =>
        !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Any(mod => mod.Enabled) &&
        !IsInstalling && InstallationNeedsRebuild;
}
