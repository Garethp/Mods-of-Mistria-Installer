using System.Diagnostics;
using System.IO.Compression;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib;

// Coordinates mod installation and uninstallation.
// Delegates file-type-specific work to Installer subclasses.
public class ModInstaller
{
    private readonly string _fomLocation;
    private readonly string _assetsLocation;
    private readonly string _atlasDirectory;

    public ModInstaller(string fomLocation, string modsLocation)
    {
        _fomLocation    = fomLocation;
        _assetsLocation = Path.Combine(fomLocation, "assets");
        _atlasDirectory = Path.Combine(_assetsLocation, "atlases");
    }

    public void InstallMods(List<IMod> mods, Action<string, string> reportStatus)
    {
        if (!Directory.Exists(_fomLocation))
            throw new DirectoryNotFoundException(Resources.CoreMistriaLocationDoesNotExist);

        if (IsFreshInstall())
        {
            var zipPath = Path.Combine(_fomLocation, "assets.zip");
            if (!File.Exists(zipPath)) return;

            reportStatus("Fresh install: extracting assets.zip. This may take a while.", "");
            ZipFile.ExtractToDirectory(zipPath, _fomLocation);
            File.Move(zipPath, Path.Combine(_fomLocation, "assets_backup.zip"));
            reportStatus("Assets extracted.", "");
            return;
        }

        var totalTime = Stopwatch.StartNew();

        Uninstall();

        // Shared state across all installers for this install session
        IDManager.Reset();
        var manifest          = InstallManifest.LoadOrCreate(_fomLocation);
        var fileNameUIDMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var atlasUtils        = new AtlasUtilities(_atlasDirectory, manifest);

        IDManager.CollectUsedIds(atlasUtils.GetAtlases());

        foreach (var mod in mods)
        {
            var modTimer = Stopwatch.StartNew();
            reportStatus($"Installing {mod.GetName()} {mod.GetVersion()} by {mod.GetAuthor()}", "");

            RunInstallers(mod, manifest, fileNameUIDMapping, atlasUtils, reportStatus);

            modTimer.Stop();
            reportStatus($"Finished {mod.GetName()}", modTimer.Elapsed.ToString());
        }

        manifest.Save();
        totalTime.Stop();
        reportStatus(Resources.CoreInstallCompleted, totalTime.Elapsed.ToString());
    }

    public void Uninstall()
    {
        var manifest = InstallManifest.LoadOrCreate(_fomLocation);

        // If a manifest exists, use it for a clean targeted restore
        manifest.Restore();

        // Also sweep assets/ and remove any files absent from assets_backup.zip
        // to handle installs that predate the manifest
        RemoveModdedFiles();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void RunInstallers(
        IMod mod,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping,
        AtlasUtilities atlasUtils,
        Action<string, string> reportStatus)
    {
        // 1. Pack images into atlases first so IDs are ready for TOML
        new ImageInstaller(_fomLocation, manifest, fileNameUIDMapping, atlasUtils)
            .Install(mod, reportStatus);

        // 2. Install TOML files (uses IDs populated above)
        new TOMLInstaller(_fomLocation, manifest, fileNameUIDMapping)
            .Install(mod, reportStatus);

        // 3. JSON (stub — no-op for now)
        new JSONInstaller(_fomLocation, manifest, fileNameUIDMapping)
            .Install(mod, reportStatus);
    }

    private bool IsFreshInstall() =>
        !Directory.Exists(_assetsLocation);

    // Deletes any file in assets/ that is not present in assets_backup.zip.
    // This catches files added by older installs that had no manifest.
    private void RemoveModdedFiles()
    {
        var backupZipPath = Path.Combine(_fomLocation, "assets_backup.zip");
        if (!File.Exists(backupZipPath) || !Directory.Exists(_assetsLocation))
            return;

        using var zip = ZipFile.OpenRead(backupZipPath);

        var zipFiles = new HashSet<string>(
            zip.Entries
               .Where(e => !string.IsNullOrEmpty(e.Name))
               .Select(e =>
               {
                   var path = e.FullName.Replace('/', Path.DirectorySeparatorChar);
                   const string prefix = "assets" + "\\";
                   return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                       ? path[prefix.Length..]
                       : path;
               }),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(_assetsLocation, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_assetsLocation, file);
            if (!zipFiles.Contains(relative))
                File.Delete(file);
        }
    }
}
