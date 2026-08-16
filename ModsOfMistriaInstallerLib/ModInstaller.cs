using System.Diagnostics;
using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Garethp.ModsOfMistriaInstallerLib.Tools;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Microsoft.Win32;

namespace Garethp.ModsOfMistriaInstallerLib;

// Coordinates mod installation and uninstallation.
// Delegates file-type-specific work to Installer subclasses.
public class ModInstaller
{
    private readonly string _fomLocation;
    private readonly string _assetsLocation;
    private readonly string _atlasDirectory;
    // The game's save directory, for the reseed harvest. Null disables the
    // harvest entirely. An explicit dependency rather than resolved here, so
    // tests stay hermetic and entry points opt in deliberately.
    private readonly string? _savesLocation;
    private IFileModifier _fileModifier;

    public ModInstaller(string fomLocation, string modsLocation, string? savesLocation = null)
    {
        _fomLocation    = fomLocation;
        _savesLocation  = savesLocation;
        _assetsLocation = Path.Combine(fomLocation, "assets");
        _atlasDirectory = Path.Combine(_assetsLocation, "atlases");
    }

    public static IEnumerable<IGenerator> GetGenerators()
    {
        return (from app in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from type in app.GetTypes()
            where type.GetInterface(nameof(IGenerator)) is not null && !type.IsAbstract
            let attributes = type.GetCustomAttributes(typeof(InformationGenerator), true)
            where attributes is { Length: > 0 } &&
                  attributes.Any(attribute => (InformationGenerator)attribute is { ManifestVersion: 2 })
            select Activator.CreateInstance(type) as IGenerator).ToList();
    }
    
    
    public static void ValidateMods(List<IMod> mods)
    {
        var desiredGenerators = GetGenerators();
        
        mods.ForEach(mod =>
        {
            foreach (var generator in desiredGenerators)
            {
                mod.GetValidation().Merge(generator.Validate(mod));
            }
        });
    }

    public InstallResult InstallMods(List<IMod> mods, Action<string, string> reportStatus,
        GmlLayerOptions? gmlOptions = null, CompileGateMode gateMode = CompileGateMode.Auto,
        Action<string, string>? reportPhase = null)
    {
        if (!Directory.Exists(_fomLocation))
            throw new DirectoryNotFoundException(Resources.CoreMistriaLocationDoesNotExist);

        // Coarse progress for a status line: the current mod (or "" for a
        // whole-install step) and the phase it is in. reportStatus stays the
        // verbose per-file channel.
        var phase = reportPhase ?? ((_, _) => { });

        var store = new AssetsStore(_fomLocation);
        store.EnsureBackup();

        // Stage the GML layer before the rebuild. The layer stages only when
        // at least one mod ships gml; a mod-content failure excludes that one
        // mod. When the game build itself moved under the catalog, every GML
        // mod is skipped whole and the content-only install proceeds.
        var result = new InstallResult();
        GmlLayerPlan? plan = null;
        var installMods = mods;

        // A mod may ship extension registrations and no gml at all. It still
        // enters the GML layer, because it still owns an install namespace and
        // still has to be excludable by the same machinery as everything else.
        var registrars = mods.Where(ExtensionCollector.HasRegistrations).ToHashSet();
        var gmlMods = mods
            .Select(mod => GmlModCollector.Collect(mod, evenWithoutGml: registrars.Contains(mod)))
            .OfType<GmlModCode>()
            .ToList();

        var ledger = ExtensionLedgerStore.Load(_fomLocation);
        List<ExtensionRegistration> registrations = [];

        // A grown base enum used to fail the install until a manual rebase.
        // The rebase is save-invisible, so it runs here automatically.
        AutoRebaseLedger(store, ledger);

        // The reseed union. A lost ledger is rebuilt from the saves' symbol
        // names before the staging gate reads HasAssignments. Fail-soft, and
        // only when a saves location is wired.
        if (_savesLocation is not null) ReseedLedgerFromSaves(store, ledger);
        else Logger.Log("  reseed: no saves location wired, the save half of the ledger "
                        + "reseed is unavailable this install");

        // A non-empty ledger enters the GML layer even with zero mods, because the
        // tombstone's enum member is what keeps a save's name references
        // resolving, so uninstalling every mod must still render vacancies.
        if (gmlMods.Count > 0 || ledger.HasAssignments)
        {
            var (catalogName, catalogBytes) = PayloadResolver.SeamCatalog();
            var catalog = SeamCatalogLoader.Load(catalogBytes, catalogName);

            // Registration content is validated before staging, and a failure
            // excludes the whole mod, because a mod's own gml may reference the
            // enum member of a registration that was dropped, so a partial
            // exclusion turns a data error into a compile error blamed on the
            // wrong thing.
            foreach (var gmlMod in gmlMods.ToList())
            {
                if (!registrars.Contains(gmlMod.Mod)) continue;

                var collected = ExtensionCollector.Collect(gmlMod.Mod, catalog);
                foreach (var finding in collected.Findings) Logger.Log($"  ! {finding}");

                if (collected.Problems.Count > 0)
                {
                    gmlMods.Remove(gmlMod);
                    foreach (var reason in collected.Problems)
                    {
                        gmlMod.Mod.GetValidation().AddError(gmlMod.Mod, "gml", reason);
                        Logger.Log($"  ! skipped mod '{gmlMod.Id}' v{gmlMod.Version}: {reason}");
                    }

                    result.Skipped.Add(new SkippedMod(gmlMod.Id, gmlMod.Version,
                        collected.Problems.ToList()));
                    continue;
                }

                registrations.AddRange(collected.Registrations);
            }

            var registrationSkips = result.Skipped.Select(s => s.Id).ToHashSet();
            installMods = mods.Where(m => !registrationSkips.Contains(m.GetId())).ToList();

            // The letters advisory. A mod letter whose `npc` names neither a
            // vanilla NPC nor a registered symbol renders through the mailbox
            // fallback icon, so say so at install time. Fail-soft on every
            // side, because an advisory may never block an install.
            try
            {
                using var lettersPristine = new ZipPristineSource(store.BackupPath);
                var natives = ExtensionCollector.NpcNativeNames(catalog, lettersPristine);
                if (natives is not null)
                {
                    var senders = new HashSet<string>(natives, StringComparer.Ordinal);
                    senders.UnionWith(registrations.Select(r => r.Symbol));
                    var localsByMod = registrations
                        .GroupBy(r => r.ModId)
                        .ToDictionary(g => g.Key,
                            g => g.Select(r => r.LocalName).ToHashSet(StringComparer.Ordinal));
                    foreach (var mod in installMods)
                    {
                        var valid = senders;
                        if (localsByMod.TryGetValue(mod.GetId(), out var locals))
                        {
                            valid = new HashSet<string>(senders, StringComparer.Ordinal);
                            valid.UnionWith(locals);
                        }

                        List<LintFinding> letterFindings = [];
                        ExtensionCollector.CheckLetterSenders(mod, valid, letterFindings);
                        foreach (var finding in letterFindings) Logger.Log($"  ! {finding}");
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Log($"  letters advisory skipped: {exception.Message}");
            }

            phase("", "Preparing GML layer");
            try
            {
                plan = StageGmlLayer(store, catalog, gmlMods, gmlOptions, gateMode,
                    registrations, ledger);
            }
            catch (SeamStagingException exception)
            {
                // fail-on-skip keeps its CI meaning: a stale catalog is a hard stop
                if (gmlOptions?.FailOnSkip == true) throw;

                // The full anchor report goes to the log; the mods carry the
                // short reason
                Logger.Log(exception.Message);

                // A non-empty ledger makes this failure fatal, not skippable.
                // Proceeding would commit an archive with no tombstone enum
                // members and no tolerance seams while stamped saves still
                // name them, the same hole the staging gate below closes for
                // the zero-mods case, resurfacing through the exception path.
                if (ledger.HasAssignments)
                    throw new InvalidOperationException(
                        "the GML layer failed to stage while the extension ledger holds "
                        + "assignments. Committing anyway would strip the enum members and "
                        + "save-tolerance fixes that installed saves rely on, so the install "
                        + "stops instead. The staging problems logged above are the cause.",
                        exception);

                foreach (var gmlMod in gmlMods)
                {
                    gmlMod.Mod.GetValidation().AddError(gmlMod.Mod, "gml", Resources.CoreGameGmlChanged);
                    result.Skipped.Add(new SkippedMod(gmlMod.Id, gmlMod.Version, [Resources.CoreGameGmlChanged]));
                }

                var gmlModSet = gmlMods.Select(g => g.Mod).ToHashSet();
                installMods = installMods.Where(m => !gmlModSet.Contains(m)).ToList();
            }
        }

        if (plan is not null)
        {
            // One mod, one fate. An excluded mod's content is excluded too
            foreach (var excluded in plan.Excluded)
            {
                var mod = excluded.Mod.Mod;
                foreach (var reason in excluded.Reasons)
                    mod.GetValidation().AddError(mod, "gml", reason);
                result.Skipped.Add(new SkippedMod(excluded.Mod.Id, excluded.Mod.Version, excluded.Reasons));
            }

            var excludedMods = plan.Excluded.Select(e => e.Mod.Mod).ToHashSet();
            installMods = installMods.Where(m => !excludedMods.Contains(m)).ToList();
        }

        _fileModifier = store.BeginRebuild();
        _fileModifier.Write("manifest.toml", "");

        if (plan is not null)
        {
            foreach (var (rel, bytes) in plan.Added) _fileModifier.Write(rel, bytes);
            foreach (var (rel, staged) in plan.Seamed) _fileModifier.Write(rel, staged.Encode());
        }

        var totalTime = Stopwatch.StartNew();

        // Shared state across all installers for this install session
        IDManager.Reset();
        var fileNameUIDMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var atlasUtils        = new AtlasUtilities(_atlasDirectory, _fileModifier);

        IDManager.CollectUsedIds(atlasUtils.GetAtlases(), _fileModifier);

        // Location pre-pass: merges all mod locations and patches TMX destination_ids
        // before the per-mod loop so that positional LocationIds are globally consistent.
        phase("", "Merging locations");
        new LocationInstaller(_fomLocation, _fileModifier).Install(installMods, reportStatus);

        foreach (var mod in installMods)
        {
            var modTimer = Stopwatch.StartNew();
            reportStatus($"Installing {mod.GetName()} {mod.GetVersion()} by {mod.GetAuthor()}", "");

            var modRegistrations = registrations
                .Where(r => r.ModId == mod.GetId())
                .ToList();
            RunInstallers(mod, fileNameUIDMapping, atlasUtils, reportStatus, phase, modRegistrations);

            modTimer.Stop();
            reportStatus($"Finished {mod.GetName()}", modTimer.Elapsed.ToString());
        }

        phase("", "Saving atlases");
        atlasUtils.Flush();

        phase("", "Writing game archive");
        store.Commit();

        // Ordinals reach disk only now, in the same transaction as a
        // successful commit, so a failed install never burns one. The entries
        // are the final round's, since a mod dropped by the exclusion loop never had
        // its assignment applied, and so never leaves a tombstone behind.
        if (plan is not null && plan.NewAssignments.Count > 0)
        {
            foreach (var entry in plan.NewAssignments) ledger.Assign(entry.PointId, entry.Assignment);
            foreach (var entry in plan.NewAssignments)
                Logger.Log($"  extension '{entry.PointId}': assigned ordinal "
                           + $"{entry.Assignment.Ordinal} to {entry.Assignment.Symbol}");
        }

        // A returning mod reclaims its attribution from a reseed's
        // "recovered" placeholder. Diagnostic only (the symbol and ordinal
        // are the contract), but the ledger is what a person reads to trace
        // a symbol, so it should name the real owner again.
        var survivingIds = installMods.Select(m => m.GetId()).ToHashSet(StringComparer.Ordinal);
        foreach (var registration in registrations.Where(r => survivingIds.Contains(r.ModId)))
            ledger.Reattribute(registration.PointId, registration.Symbol, registration.ModId);

        // Dirty also covers assignments the reseed union recovered from
        // saves, which persist through the same commit-then-save transaction.
        if (ledger.Dirty) ledger.Save();

        // After the archive commits, so the Mods tab never describes an
        // archive that failed to land. The Mods tab lists exactly what runs.
        GameManifestWriter.Write(installMods);

        totalTime.Stop();
        reportStatus(Resources.CoreInstallCompleted, totalTime.Elapsed.ToString());

        result.Installed.AddRange(installMods);
        return result;
    }

    private static GmlLayerPlan StageGmlLayer(AssetsStore store, SeamCatalog catalog,
        List<GmlModCode> gmlMods, GmlLayerOptions? gmlOptions, CompileGateMode gateMode,
        IReadOnlyList<ExtensionRegistration> registrations, IExtensionLedger ledger)
    {
        ZipPristineSource pristine;
        try
        {
            pristine = new ZipPristineSource(store.BackupPath);
        }
        catch (Exception exception)
        {
            // A raw zip exception here would otherwise escape the staging
            // catch (which handles SeamStagingException only) with a message
            // that never names the backup as the failing piece.
            throw new InvalidOperationException(
                $"the pristine backup at {store.BackupPath} could not be opened "
                + $"({exception.Message}) - the GML layer cannot stage without it. Verify the "
                + "game files through Steam and reinstall to refresh the backup.", exception);
        }

        using (pristine)
        {
            return GmlLayer.Stage(catalog, pristine, gmlMods, GmlCompileGate.Resolve(gateMode),
                gmlOptions, registrations, ledger);
        }
    }

    // Normalizes ledger ordinals against the current pristine base enums by
    // running the rebaser in memory, persisted through the same
    // commit-then-save transaction via Dirty. Fail-soft. A scan problem here
    // leaves the ledger untouched and falls through to staging, which fails
    // closed with its own message.
    private static void AutoRebaseLedger(AssetsStore store, ExtensionLedgerStore ledger)
    {
        if (!ledger.HasAssignments) return;

        try
        {
            var (catalogName, catalogBytes) = PayloadResolver.SeamCatalog();
            var catalog = SeamCatalogLoader.Load(catalogBytes, catalogName);

            // Names live forever, so a point this MOMI does not declare keeps
            // its symbols untouched. Said out loud because a silently carried
            // orphan otherwise looks identical to a point that was processed.
            var declared = catalog.Extensions.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var pointId in ledger.PointIds.Where(id => !declared.Contains(id)))
                Logger.Log($"  extension ledger holds symbols for point '{pointId}', which this "
                           + "MOMI does not declare - kept as is, another MOMI version owns it");

            using var pristine = new ZipPristineSource(store.BackupPath);
            var result = ExtensionRebaser.Run(catalog, pristine, ledger);
            if (!result.Ok)
            {
                foreach (var problem in result.Problems) Logger.Log($"  ! ordinal scan: {problem}");
                return;
            }

            foreach (var point in result.Points.Where(p => p.Changed))
            foreach (var (symbol, oldOrdinal, newOrdinal) in point.Moves)
            {
                if (oldOrdinal == newOrdinal) continue;
                Logger.Log($"  extension '{point.PointId}': the game's base enum moved, reassigned "
                           + $"'{symbol}' from ordinal {oldOrdinal} to {newOrdinal} "
                           + "(saves reference names, so this is invisible to them)");
            }
        }
        catch (Exception exception)
        {
            Logger.Log($"  automatic ordinal rebase skipped: {exception.Message}");
        }
    }

    // Harvested symbols enter as vacancy assignments attributed to
    // "recovered", ordinals appended densely above the ledger's maximum.
    // Re-deriving after a failed install is idempotent, not a burned ordinal.
    private void ReseedLedgerFromSaves(AssetsStore store, ExtensionLedgerStore ledger)
    {
        try
        {
            var (catalogName, catalogBytes) = PayloadResolver.SeamCatalog();
            var catalog = SeamCatalogLoader.Load(catalogBytes, catalogName);

            using var pristine = new ZipPristineSource(store.BackupPath);
            var harvest = SaveSymbolHarvester.Harvest(_savesLocation!, catalog, pristine);

            // The outgoing archive's markers are the second harvest source.
            // Its symbols join a point's set only when the pristine
            // scan for that point succeeded (the entry exists at all), and
            // they pass the same pristine-name subtraction and cap the save
            // path applies, so a stale marker can never re-mint a name the
            // current game defines natively. A point with no save rule still
            // gets archive coverage through the same entries.
            var fromArchive = ArchiveMarkerHarvester.Harvest(store.LivePath, catalog);
            foreach (var (pointId, symbols) in fromArchive)
            {
                if (!harvest.TryGetValue(pointId, out var found)) continue;
                foreach (var symbol in symbols.OrderBy(s => s, StringComparer.Ordinal))
                {
                    if (found.PristineNames.Contains(symbol))
                    {
                        Logger.Log($"  reseed: archive marker for '{pointId}' names '{symbol}', "
                                   + "which the current game defines natively - marker ignored");
                        continue;
                    }

                    if (found.Symbols.Count >= SaveSymbolHarvester.MaxSymbolsPerPoint)
                    {
                        Logger.Log($"  reseed: '{pointId}' union hit the "
                                   + $"{SaveSymbolHarvester.MaxSymbolsPerPoint}-symbol cap, "
                                   + "remaining archive markers dropped");
                        break;
                    }

                    found.Symbols.Add(symbol);
                }
            }

            foreach (var (pointId, found) in harvest)
            {
                if (found.Symbols.Count == 0) continue;

                var assigned = ledger.Assignments(pointId);
                var known = assigned.Select(a => a.Symbol).ToHashSet(StringComparer.Ordinal);
                var fresh = found.Symbols
                    .Where(symbol => !known.Contains(symbol))
                    .OrderBy(symbol => symbol, StringComparer.Ordinal)
                    .ToList();
                if (fresh.Count == 0) continue;

                // Tripwire, not a gate (the harvest never blocks an install).
                // A recovery this large has one known benign cause (a genuine
                // multi-mod loss) and one known dangerous one, a pristine
                // backup that no longer matches the installed game, which is
                // exactly how the live near-miss recovered a whole vanilla
                // roster before the stub-collision check stopped it.
                if (fresh.Count >= 10)
                    Logger.Log($"  ! reseed: recovering {fresh.Count} symbols for '{pointId}' in "
                               + "one install is unusually large - if this game was recently "
                               + "updated, verify the pristine backup matches it before trusting "
                               + "the recovery");

                var next = Math.Max(
                    assigned.Count > 0 ? assigned.Max(a => a.Ordinal) + 1 : found.BaseLen,
                    found.BaseLen);
                foreach (var symbol in fresh)
                {
                    ledger.Assign(pointId, new ExtensionAssignment(symbol, next++, "recovered"));
                    Logger.Log($"  extension '{pointId}': recovered tombstone for '{symbol}'");
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log($"  reseed from saves skipped: {exception.Message}");
        }
    }

    public void Uninstall()
    {
        UninstallAurie();

        // Informational only, never a gate. The ledger keeps its symbols
        // through an uninstall (names live forever), but the vanilla archive
        // stops rendering them, so a save that names modded content needs a
        // reinstall before it can load again. This is the one moment MOMI
        // knows both halves of that, so it says so in the log.
        try
        {
            if (ExtensionLedgerStore.Load(_fomLocation).HasAssignments)
                Logger.Log("uninstall: the extension ledger keeps its symbols, but the restored "
                           + "vanilla archive no longer renders them - saves that name modded "
                           + "content need a reinstall before they can load");
        }
        catch (Exception)
        {
            // a corrupt ledger must not block an uninstall
        }

        if (new AssetsStore(_fomLocation).Uninstall())
        {
            // The Mods tab matches the store again
            GameManifestWriter.Write([]);
        }
    }

    /**
     * We're seeing a lot of people returning from back when Aurie used the registry to patch into Fields of Mistria
     * and encountering a "Missing executable" error. This is very difficult to resolve for users, so let's try to
     * resolve it automatically for them in MOMI
     */
    private void UninstallAurie()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var mistriaSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options")?.OpenSubKey("FieldsOfMistria.exe");

        if (mistriaSubKey is null) return;

        var proc = new Process();
        proc.StartInfo.FileName = "reg";
        proc.StartInfo.ArgumentList.Add("delete");
        proc.StartInfo.ArgumentList.Add(mistriaSubKey.Name);
        proc.StartInfo.ArgumentList.Add("/f");
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.Verb = "runas";
        proc.Start();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void RunInstallers(
        IMod mod,
        Dictionary<string, string> fileNameUIDMapping,
        AtlasUtilities atlasUtils,
        Action<string, string> reportStatus,
        Action<string, string> reportPhase,
        IReadOnlyList<ExtensionRegistration> registrations)
    {
        var generatedInformation = new GeneratedInformation();
        var modName = mod.GetName();

        // 0. Expand momi/ compact definitions into virtual overlay files
        reportPhase(modName, "Preparing");
        var generated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        generatedInformation.Merge(new OutfitGenerator().Generate(mod));
        
        foreach (var kvp in new FurnitureGenerator().Generate(mod))
            generated.TryAdd(kvp.Key, kvp.Value);

        var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        new CompactFurnitureGenerator().Generate(mod, generated, redirects);

        // Rewrite npc_roster local names (fiddle/npcs/luna.toml, schedule keys,
        // art file names) to the derived symbol, so authors write one short
        // name and MOMI de-conflicts at install.
        var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ExtensionLocalNames.Expand(mod, registrations, generated, redirects, hidden);

        IMod effectiveMod = generated.Count > 0 || redirects.Count > 0 || hidden.Count > 0
            ? new GeneratedOverlayMod(mod, generated, redirects, hidden)
            : mod;
        
        foreach (var generator in GetGenerators())
        {
            generatedInformation.Merge(generator.Generate(mod));
        }
        
        generatedInformation.Merge(new TOMLCollector().Collect(effectiveMod));
        
        // 1. Pack images into atlases first so IDs are ready for TOML
        reportPhase(modName, "Installing Images");
        new ImageInstaller(fileNameUIDMapping, atlasUtils, _fileModifier)
            .Install(effectiveMod, generatedInformation, reportStatus);

        // 2. Install TOML files (uses IDs populated above)
        reportPhase(modName, "Installing TOML");
        new TOMLInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, generatedInformation, reportStatus);

        // 3. Install JSON files
        reportPhase(modName, "Installing JSON");
        new JSONInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, generatedInformation, reportStatus);

        // 4. Install XML files
        reportPhase(modName, "Installing XML");
        new XMLInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, generatedInformation, reportStatus);

        // 5. Install MIST files (overwrite)
        reportPhase(modName, "Installing Mist");
        new MISTInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, generatedInformation, reportStatus);

        // 6. Generate data-layer content from momi/ definitions (fiddle, outlines, asset_parts)
        reportPhase(modName, "Installing Outfits");
        new OutfitInstaller(fileNameUIDMapping, _fileModifier)
            .Install(mod, generatedInformation, reportStatus);
        
        reportPhase(modName, "Installing Furniture");
        new FurnitureInstaller(fileNameUIDMapping, _fileModifier)
            .Install(mod, generatedInformation, reportStatus);
        
        atlasUtils.SemiFlush();
    }
}
