using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Garethp.ModsOfMistriaInstallerLib.Tools;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib;

// Coordinates mod installation and uninstallation.
// Delegates file-type-specific work to Installer subclasses.
public class ModInstaller
{
    private readonly string _fomLocation;
    private readonly string _assetsLocation;
    private readonly string _atlasDirectory;
    private IFileModifier _fileModifier;

    public ModInstaller(string fomLocation, string modsLocation)
    {
        _fomLocation    = fomLocation;
        _assetsLocation = Path.Combine(fomLocation, "assets");
        _atlasDirectory = Path.Combine(_assetsLocation, "atlases");
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
        var gmlMods = mods.Select(GmlModCollector.Collect).OfType<GmlModCode>().ToList();
        if (gmlMods.Count > 0)
        {
            phase("", "Preparing GML layer");
            try
            {
                plan = StageGmlLayer(store, gmlMods, gmlOptions, gateMode);
            }
            catch (SeamStagingException exception)
            {
                // fail-on-skip keeps its CI meaning: a stale catalog is a hard stop
                if (gmlOptions?.FailOnSkip == true) throw;

                // The full anchor report goes to the log; the mods carry the
                // short reason
                Logger.Log(exception.Message);
                foreach (var gmlMod in gmlMods)
                {
                    gmlMod.Mod.GetValidation().AddError(gmlMod.Mod, "gml", Resources.CoreGameGmlChanged);
                    result.Skipped.Add(new SkippedMod(gmlMod.Id, gmlMod.Version, [Resources.CoreGameGmlChanged]));
                }

                var gmlModSet = gmlMods.Select(g => g.Mod).ToHashSet();
                installMods = mods.Where(m => !gmlModSet.Contains(m)).ToList();
            }
        }

        if (plan is not null)
        {
            // D12: one mod, one fate - an excluded mod's content is excluded too
            foreach (var excluded in plan.Excluded)
            {
                var mod = excluded.Mod.Mod;
                foreach (var reason in excluded.Reasons)
                    mod.GetValidation().AddError(mod, "gml", reason);
                result.Skipped.Add(new SkippedMod(excluded.Mod.Id, excluded.Mod.Version, excluded.Reasons));
            }

            var excludedMods = plan.Excluded.Select(e => e.Mod.Mod).ToHashSet();
            installMods = mods.Where(m => !excludedMods.Contains(m)).ToList();
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

            RunInstallers(mod, fileNameUIDMapping, atlasUtils, reportStatus, phase);

            modTimer.Stop();
            reportStatus($"Finished {mod.GetName()}", modTimer.Elapsed.ToString());
        }

        phase("", "Saving atlases");
        atlasUtils.Flush();

        phase("", "Writing game archive");
        store.Commit();

        // After the archive commits, so the Mods tab never describes an
        // archive that failed to land. The Mods tab lists exactly what runs (D12).
        GameManifestWriter.Write(installMods);

        totalTime.Stop();
        reportStatus(Resources.CoreInstallCompleted, totalTime.Elapsed.ToString());

        result.Installed.AddRange(installMods);
        return result;
    }

    private GmlLayerPlan StageGmlLayer(AssetsStore store, List<GmlModCode> gmlMods,
        GmlLayerOptions? gmlOptions, CompileGateMode gateMode)
    {
        var (catalogName, catalogBytes) = PayloadResolver.SeamCatalog();
        var catalog = SeamCatalogLoader.Load(catalogBytes, catalogName);

        using var pristine = new ZipPristineSource(store.BackupPath);
        return GmlLayer.Stage(catalog, pristine, gmlMods, GmlCompileGate.Resolve(gateMode), gmlOptions);
    }

    public void Uninstall()
    {
        if (new AssetsStore(_fomLocation).Uninstall())
        {
            // The Mods tab matches the store again
            GameManifestWriter.Write([]);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void RunInstallers(
        IMod mod,
        Dictionary<string, string> fileNameUIDMapping,
        AtlasUtilities atlasUtils,
        Action<string, string> reportStatus,
        Action<string, string> reportPhase)
    {
        var modName = mod.GetName();

        // 0. Expand momi/ compact definitions into virtual overlay files
        reportPhase(modName, "Preparing");
        var generated = new OutfitGenerator().Generate(mod);
        foreach (var kvp in new FurnitureGenerator().Generate(mod))
            generated.TryAdd(kvp.Key, kvp.Value);

        var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        new CompactFurnitureGenerator().Generate(mod, generated, redirects);

        IMod effectiveMod = generated.Count > 0 || redirects.Count > 0
            ? new GeneratedOverlayMod(mod, generated, redirects)
            : mod;

        // 1. Pack images into atlases first so IDs are ready for TOML
        reportPhase(modName, "Installing Images");
        new ImageInstaller(fileNameUIDMapping, atlasUtils, _fileModifier)
            .Install(effectiveMod, reportStatus);

        // 2. Install TOML files (uses IDs populated above)
        reportPhase(modName, "Installing TOML");
        new TOMLInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, reportStatus);

        // 3. Install JSON files
        reportPhase(modName, "Installing JSON");
        new JSONInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, reportStatus);

        // 4. Install XML files
        reportPhase(modName, "Installing XML");
        new XMLInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, reportStatus);

        // 5. Install MIST files (overwrite)
        reportPhase(modName, "Installing Mist");
        new MISTInstaller(fileNameUIDMapping, _fileModifier)
            .Install(effectiveMod, reportStatus);

        // 6. Generate data-layer content from momi/ definitions (fiddle, outlines, asset_parts)
        reportPhase(modName, "Installing Outfits");
        new OutfitInstaller(fileNameUIDMapping, _fileModifier)
            .Install(mod, reportStatus);

        reportPhase(modName, "Installing Furniture");
        new FurnitureInstaller(fileNameUIDMapping, _fileModifier)
            .Install(mod, reportStatus);

        atlasUtils.SemiFlush();
    }
}
