using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Store;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using UpdateChecker = Garethp.ModsOfMistriaInstallerLib.UpdateChecker;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public partial class ModlistPageViewModel : PageViewBase
{
    private bool _updating;
    private readonly Settings _settings;
    private ProfileManager? _profileManager;

    // True when in-GUI state differs from what is saved in the current profile
    private bool _isDirty;
    // Suppresses dirty-marking during programmatic enabled-state changes (profile apply)
    private bool _suppressDirty;
    // Prevents re-entrant cascades when auto-enabling/disabling dependents
    private bool _cascading;

    public ModlistPageViewModel(Settings settings)
    {
        _settings = settings;
        _settings.PropertyChanged += (_, _) => { Task.Run(UpdateModlist); };
        Task.Run(UpdateModlist);
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
                "Save Profile",
                $"Save changes to profile \"{CurrentProfile}\" before switching?",
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
        var name = $"Profile {Profiles.Count + 1}";
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
            "Delete Profile",
            $"Delete profile \"{CurrentProfile}\"? This cannot be undone.",
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
        var loadOrder = Mods.Select(m => m.Mod.GetId()).ToList();
        _profileManager.SaveCurrentProfile(enabled, loadOrder);
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
            var enabledSet = enabledIds.Count == 0 && loadOrder.Count == 0
                ? allMods.Select(m => m.GetId()).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : enabledIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sorted = ProfileManager.SortByLoadOrder(allMods, loadOrder);

            var newModels = sorted.Select((mod, idx) =>
            {
                var model = Mods.FirstOrDefault(m => m.Mod.GetId() == mod.GetId())
                            ?? new ModModel(mod);
                model.Enabled  = enabledSet.Contains(mod.GetId());
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
        }
    }

    // ── Load order ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void MoveModUp(ModModel mod)
    {
        var index = Mods.IndexOf(mod);
        if (index <= 0) return;
        Mods.Move(index, index - 1);
        RefreshPositions();
        _isDirty = true;
    }

    [RelayCommand]
    private void MoveModDown(ModModel mod)
    {
        var index = Mods.IndexOf(mod);
        if (index < 0 || index >= Mods.Count - 1) return;
        Mods.Move(index, index + 1);
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

    private void UpdateModlist() => UpdateModlist(false);

    private void UpdateModlist(bool force)
    {
        // UpdateModlist is requested from background tasks because manifest
        // discovery can involve many archives. ObservableCollection mutations
        // must nevertheless happen on Avalonia's UI thread; otherwise
        // ItemsRepeater virtualization can display one mod's row for another.
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => UpdateModlist(force));
            return;
        }

        if (_updating) return;
        if (MistriaLocation == _settings.MistriaLocation && ModsLocation == _settings.ModsLocation && !force) return;
        _updating = true;

        MistriaLocation = _settings.MistriaLocation;
        ModsLocation    = _settings.ModsLocation;

        Mods.Clear();

        if (Directory.Exists(ModsLocation))
        {
            // (Re-)create profile manager when the mods folder changes
            try { _profileManager = new ProfileManager(ModsLocation); }
            catch { _profileManager = null; }

            var rawMods = MistriaLocator.GetMods(MistriaLocation, ModsLocation);
            ModInstaller.ValidateMods(rawMods);
            
            // Apply dependency resolution (auto-enable deps)
            if (_profileManager is not null)
            {
                var (enabledIds, loadOrder) = _profileManager.GetCurrentProfile();

                List<string> resolvedEnabled;
                if (enabledIds.Count == 0 && loadOrder.Count == 0)
                {
                    // Fresh profile: enable everything
                    resolvedEnabled = rawMods.Select(m => m.GetId()).ToList();
                }
                else
                {
                    resolvedEnabled = ProfileManager.ResolveEnabledWithDeps(rawMods, enabledIds);
                }

                var enabledSet  = resolvedEnabled.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var sortedMods  = ProfileManager.SortByLoadOrder(rawMods, loadOrder);

                for (var i = 0; i < sortedMods.Count; i++)
                {
                    var model = new ModModel(sortedMods[i])
                    {
                        Enabled  = enabledSet.Contains(sortedMods[i].GetId()),
                        Position = i + 1
                    };
                    // Track enabled changes; cascade enable/disable to dependencies
                    model.PropertyChanged += async (sender, e) =>
                    {
                        if (e.PropertyName != nameof(ModModel.Enabled) || _suppressDirty) return;
                        _isDirty = true;
                        RefreshArchiveStatus();
                        if (_cascading) return;
                        _cascading = true;
                        List<ModRequirement> missing;
                        try
                        {
                            var changed = (ModModel)sender!;
                            if (changed.Enabled)
                                missing = EnableDependenciesOf(changed);
                            else
                            {
                                DisableDependentsOf(changed);
                                missing = [];
                            }
                        }
                        finally { _cascading = false; }
                        InstallModsCommand.NotifyCanExecuteChanged();
                        UnInstallModsCommand.NotifyCanExecuteChanged();

                        if (missing.Count > 0)
                        {
                            var lines = string.Join("\n\n", missing.Select(r =>
                            {
                                var line = $"• \"{r.Name}\" by {r.Author}";
                                if (!string.IsNullOrEmpty(r.DownloadUrl))
                                    line += $"\n  {r.DownloadUrl}";
                                return line;
                            }));

                            var urls = missing
                                .Where(r => !string.IsNullOrEmpty(r.DownloadUrl))
                                .Select(r => r.DownloadUrl!)
                                .ToList();

                            if (urls.Count > 0)
                            {
                                var ask = await MessageBoxManager.GetMessageBoxStandard(
                                    "Missing Requirements",
                                    $"The following required mods could not be found:\n\n{lines}\n\nOpen download page(s) in browser?",
                                    ButtonEnum.YesNo).ShowAsync();

                                if (ask == ButtonResult.Yes)
                                {
                                    var urlList = string.Join("\n", urls.Select(u => $"• {u}"));
                                    var confirm = await MessageBoxManager.GetMessageBoxStandard(
                                        "Open External Links",
                                        $"You are about to open the following URL(s) in your browser:\n\n{urlList}",
                                        ButtonEnum.YesNo).ShowAsync();

                                    if (confirm == ButtonResult.Yes)
                                        foreach (var url in urls)
                                            System.Diagnostics.Process.Start(
                                                new System.Diagnostics.ProcessStartInfo
                                                {
                                                    FileName        = url,
                                                    UseShellExecute = true
                                                });
                                }
                            }
                            else
                            {
                                await MessageBoxManager.GetMessageBoxStandard(
                                    "Missing Requirements",
                                    $"The following required mods could not be found and must be installed manually:\n\n{lines}",
                                    ButtonEnum.Ok).ShowAsync();
                            }
                        }
                    };
                    Mods.Add(model);
                }
            }
            else
            {
                // No profile manager (e.g. can't write to folder): fallback behaviour
                var allDisabled = rawMods.All(m => !m.IsInstalled());
                if (allDisabled) rawMods.ForEach(m => m.SetInstalled(true));

                for (var i = 0; i < rawMods.Count; i++)
                {
                    var model = new ModModel(rawMods[i]) { Position = i + 1 };
                    Mods.Add(model);
                }
            }

            RefreshProfileList();
        }

        _isDirty = false;

        // Kick off background update checks; results trickle in on the UI thread
        var modSnapshot = Mods.ToList();
        _ = Task.Run(() => CheckModUpdatesAsync(modSnapshot));

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InstallStatus = "";
            RefreshGameReady();
            InstallModsCommand.NotifyCanExecuteChanged();
            UnInstallModsCommand.NotifyCanExecuteChanged();

            if (MistriaLocation.Equals(""))
                InstallStatus = Resources.GUICouldNotFindMistria;
            else if (ModsLocation.Equals(""))
                InstallStatus = Resources.GUICouldNotFindMods;
            else if (Mods.Count == 0)
                InstallStatus = NoModsToInstallText;
        });

        _updating = false;
    }

    // Checks all mods for updates in parallel; each result updates the matching
    // ModModel on the UI thread so the update badge appears as responses arrive.
    private static async Task CheckModUpdatesAsync(List<ModModel> models)
    {
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
    [ObservableProperty] private bool _isInstalling;

    [NotifyCanExecuteChangedFor(nameof(InstallModsCommand))]
    [ObservableProperty] private bool _installationNeedsRebuild;

    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [ObservableProperty] private bool _gameReady;

    public ObservableCollection<ModModel> Mods { get; } = [];

    // ── Commands ──────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void InstallMods()
    {
        Exception = "";

        // Auto-save profile state before installing so load order is persisted
        SaveCurrentProfileState();

        // The icons describe the install that is about to run, not the last one
        foreach (var mod in Mods) mod.SetInstallOutcome(ModInstallState.None);

        InstallStatus = InstallInProgressText;
        IsInstalling  = true;
        Task.Run(BackgroundInstall);
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
            SuggestedFileName = $"momi-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            DefaultExtension  = "txt",
            FileTypeChoices   = [FilePickerFileTypes.TextPlain]
        });

        if (files is not null)
            await File.WriteAllTextAsync(files.Path.AbsolutePath, string.Join("\r\n", logs));
    }

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

        Task.Run(async () =>
        {
            try
            {
                new ModInstaller(MistriaLocation, ModsLocation).Uninstall();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsInstalling  = false;
                    InstallStatus = Resources.GUIUninstallCompleteText;
                    RefreshGameReady();
                    // Nothing is installed any more; the outcome icons are stale
                    foreach (var mod in Mods) mod.SetInstallOutcome(ModInstallState.None);
                });
            }
            catch (Exception e)
            {
                var errorLogPath = WriteDiagnosticErrorLog("uninstall", e);
                Logger.Log($"{Resources.GUIUninstallFatalError}\r\n{e}");

                // Keep the page recoverable. The archive transaction has already
                // aborted, so the user can inspect the error and retry.
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsInstalling  = false;
                    InstallStatus = Resources.GUIUninstallFatalError;
                    RefreshGameReady();
                    Exception     = FormatErrorMessage(Resources.GUIUninstallFatalError, e, errorLogPath);
                });
            }
        });
    }

    private async void BackgroundInstall()
    {
        try
        {
            var installer     = new ModInstaller(MistriaLocation, ModsLocation);
            var modsToInstall = Mods.Where(m => m.Enabled).Select(m => m.Mod).ToList();

            // Per-file messages go to the log only; the status line follows
            // the coarse mod + phase channel so it doesn't redraw per file
            var result = installer.InstallMods(modsToInstall,
                (message, _) => Logger.Log(message),
                reportPhase: (mod, phaseText) =>
                    InstallStatus = mod.Length == 0 ? phaseText : $"{mod} - {phaseText}");

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsInstalling  = false;
                InstallStatus = result.Summary();
                RefreshGameReady();

                // Checkmark for what landed, red X with the reasons for what
                // was skipped; a skipped mod's reasons also landed as
                // validation errors, so refresh the expander bindings too
                var installed = result.Installed.ToHashSet();
                var skipped   = result.Skipped.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
                foreach (var mod in Mods)
                {
                    if (skipped.TryGetValue(mod.Mod.GetId(), out var skip))
                        mod.SetInstallOutcome(ModInstallState.Skipped, string.Join("\r\n", skip.Reasons));
                    else if (installed.Contains(mod.Mod))
                        mod.SetInstallOutcome(ModInstallState.Installed, Resources.GUIModInstalled);
                    mod.RefreshValidation();
                }
            });
        }
        catch (Exception e)
        {
            // Write the diagnostic first so its Recent MOMI log contains only
            // the progress leading up to the failure, not a second copy of the
            // same full exception.
            var errorLogPath = WriteDiagnosticErrorLog("install", e);
            Logger.Log($"{Resources.GUIInstallFatalError}\r\n{e}");
            var failedModId = (e as ModInstallationException)?.ModId;

            // Keep the page recoverable. The archive transaction has already
            // aborted, so the user can disable the failing mod and retry.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(failedModId))
                {
                    var failedMod = Mods.FirstOrDefault(m =>
                        string.Equals(m.Mod.GetId(), failedModId, StringComparison.OrdinalIgnoreCase));
                    failedMod?.SetInstallOutcome(
                        ModInstallState.Failed,
                        e.Message + "\r\nReason: " + GetRootCauseMessage(e));
                }

                IsInstalling  = false;
                InstallStatus = Resources.GUIInstallFatalError;
                RefreshGameReady();
                Exception     = FormatErrorMessage(Resources.GUIInstallFatalError, e, errorLogPath);
            });
        }
    }

    private static string FormatErrorMessage(string heading, Exception exception, string? errorLogPath)
    {
        var rootCause = GetRootCauseMessage(exception);
        var message = heading + "\n" + exception.Message;
        if (!string.Equals(rootCause, exception.Message, StringComparison.Ordinal))
            message += "\n\nReason: " + rootCause;

        if (!string.IsNullOrEmpty(errorLogPath))
        {
            message += $"\n\nError details saved to:\n{errorLogPath}";
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
            var directory = Path.Combine(root, "MOMI", "logs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"momi-error-{DateTime.Now:yyyyMMdd-HHmmssfff}.txt");
            var contents = $"MOMI diagnostic error log\r\n" +
                           $"Timestamp (UTC): {DateTime.UtcNow:O}\r\n" +
                           $"Application: {AppInfo.DisplayVersion}\r\n" +
                           $"Operation: {operation}\r\n\r\n" +
                           "Exception:\r\n" + exception + "\r\n\r\n" +
                           "Recent MOMI log:\r\n" + string.Join("\r\n", Logger.GetLogs());
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

        var executable = Path.Combine(MistriaLocation, "FieldsOfMistria.exe");
        if (!File.Exists(executable)) return;

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
        if (!File.Exists(archivePath))
            return false;

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(archivePath);
            return archive.Entries.Any(entry =>
                entry.FullName.Replace('\\', '/')
                    .StartsWith("assets/", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void RefreshArchiveStatus()
    {
        if (string.IsNullOrWhiteSpace(MistriaLocation))
        {
            InstallationNeedsRebuild = false;
            ArchiveStatus = "No game archive detected.";
            return;
        }

        var store = new AssetsStore(MistriaLocation);
        if (!store.HasMomiInstallation())
        {
            InstallationNeedsRebuild = true;
            ArchiveStatus = "No MOMI installation detected. Install will create one.";
            return;
        }

        RecordedInstallState? recorded;
        try { recorded = store.GetRecordedInstallState(); }
        catch
        {
            InstallationNeedsRebuild = true;
            ArchiveStatus = "MOMI installation detected, but its recorded state is unavailable.";
            return;
        }

        if (recorded is null)
        {
            InstallationNeedsRebuild = true;
            ArchiveStatus = "MOMI installation detected, but installed mod versions are unavailable.";
            return;
        }

        var desired = Mods
            .Where(mod => mod.Enabled)
            .ToDictionary(mod => mod.Mod.GetId(), mod => mod.Mod.GetVersion(),
                StringComparer.OrdinalIgnoreCase);
        var actual = recorded.Mods
            .ToDictionary(mod => mod.Id, mod => mod.Version,
                StringComparer.OrdinalIgnoreCase);

        var matches = desired.Count == actual.Count &&
                      desired.All(pair => actual.TryGetValue(pair.Key, out var version) &&
                                          string.Equals(version, pair.Value, StringComparison.OrdinalIgnoreCase));

        ArchiveStatus = matches
            ? $"MOMI installation: {actual.Count} mod(s) installed."
            : "The installed mod set or version differs from this profile. Recommended: Uninstall, then Install. Install can also rebuild from the pristine backup.";
        InstallationNeedsRebuild = !matches;
    }

    private bool CanInstall() =>
        !MistriaLocation.Equals("") && !ModsLocation.Equals("") && Mods.Any(mod => mod.Enabled) &&
        !IsInstalling && InstallationNeedsRebuild;
}
