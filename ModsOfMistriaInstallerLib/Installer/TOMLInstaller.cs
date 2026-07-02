using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs all TOML files from a mod:
//   • spr_*.meta.toml   → copies to assets/, injects the ID from FileNameUIDMapping
//   • poly_*.meta.toml  → copies to assets/, generates a new ID, links required_assets
//   • other *.toml      → merges with the existing game file using MOMI patch semantics
//   • other *.meta.toml → copies/merges as plain TOML
//
// ImageInstaller must run first so FileNameUIDMapping is populated.
public class TOMLInstaller(
    string fomLocation,
    InstallManifest manifest,
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier modifier)
    : Installer(fomLocation, manifest, fileNameUidMapping)
{
    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        var collector = new TOMLCollector();
        collector.Collect(mod);

        foreach (var group in collector.Groups)
        {
            if (group.HasAnimation)
                InstallAnimationMeta(mod, group, reportStatus);

            if (group.HasShape)
                InstallShapeMeta(mod, group, reportStatus);
        }

        foreach (var relPath in collector.OtherTomlFiles)
            InstallGenericToml(mod, relPath, reportStatus);
    }

    // Animation .meta.toml

    private void InstallAnimationMeta(IMod mod, AnimationGroup group, Action<string, string> reportStatus)
    {
        var relPath = group.AnimationMetaRelPath!;
        var dest    = DestinationPath(relPath);

        var toml = Toml.ParseToml(mod.ReadFile(relPath));
        var meta  = EnsureTable(toml, "meta_properties");

        // If the mod's file already has an id, honour it and register it for
        // related files (poly_) to reference.  Otherwise use the ID assigned by
        // ImageInstaller, or generate one now (animation with no PNG strip).
        if (meta.TryGetValue("id", out var existingIdObj) &&
            existingIdObj is string existingId &&
            !string.IsNullOrEmpty(existingId))
        {
            if (!FileNameUIDMapping.ContainsKey(group.BaseName))
                FileNameUIDMapping[group.BaseName] = existingId;
        }
        else
        {
            if (!FileNameUIDMapping.TryGetValue(group.BaseName, out var id))
            {
                id = IDManager.GenerateUniqueId();
                FileNameUIDMapping[group.BaseName] = id;
            }
            meta["id"] = id;
        }

        Dirty(dest);
        MergeOrWriteToml(toml, dest);
        reportStatus($"Installed animation meta: {relPath}", "");
    }

    // Poly .meta.toml

    private void InstallShapeMeta(IMod mod, AnimationGroup group, Action<string, string> reportStatus)
    {
        var relPath = group.ShapeMetaRelPath!;
        var dest    = DestinationPath(relPath);

        var toml = Toml.ParseToml(mod.ReadFile(relPath));
        var meta  = EnsureTable(toml, "meta_properties");

        // Preserve the mod's own shape id if provided; generate one otherwise.
        if (!meta.TryGetValue("id", out var existingIdObj) ||
            existingIdObj is not string existingId ||
            string.IsNullOrEmpty(existingId))
        {
            meta["id"] = IDManager.GenerateUniqueId();
        }

        // Link to the paired animation only when the mod hasn't already set it.
        if (!meta.TryGetValue("required_assets", out _) &&
            FileNameUIDMapping.TryGetValue(group.BaseName, out var animId))
        {
            meta["required_assets"] = new TomlArray { animId };
        }

        Dirty(dest);
        MergeOrWriteToml(toml, dest);
        reportStatus($"Installed shape meta: {relPath}", "");
    }

    // Generic TOML / ungrouped .meta.toml

    private void InstallGenericToml(IMod mod, string relPath, Action<string, string> reportStatus)
    {
        var dest       = DestinationPath(relPath);
        var sourceToml = Toml.ParseToml(mod.ReadFile(relPath));

        Dirty(dest);
        MergeOrWriteToml(sourceToml, dest);
        reportStatus($"Installed TOML: {relPath}", "");
    }

    // Helpers

    // Merges sourceToml into the existing destination file (if it exists),
    // or writes it directly if the destination is new.
    private void MergeOrWriteToml(TomlTable sourceToml, string destPath)
    {
        if (!modifier.Exists(destPath))
        {
            modifier.Write(destPath, TomlSerializer.Serialize(sourceToml));
        } 

        var destToml = TomlSerializer.Deserialize<TomlTable>(modifier.Read(destPath))!;
        MOMIOperations.MergeTomlTables(destToml, sourceToml);
        
        modifier.Write(destPath, TomlSerializer.Serialize(destToml));
    }

    private static TomlTable EnsureTable(TomlTable parent, string key)
    {
        if (!parent.TryGetValue(key, out var obj) || obj is not TomlTable table)
        {
            table = new TomlTable();
            parent[key] = table;
        }
        return table;
    }
}
