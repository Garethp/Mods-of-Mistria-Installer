using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Garethp.ModsOfMistriaInstallerLib.Tools;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

// The whole install flow against a disposable copy of the real engine: the
// shipped catalog, the carried framework, the real compile gate and the real
// store, end to end. Skipped where no pristine archive exists (CI).
[TestFixture]
public class GmlLayerLocalTest
{
    private static string PristineZipPath()
    {
        var path = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");
        return path!;
    }

    [Test]
    public void ShouldInstallAndUninstallAgainstTheRealEngine()
    {
        var pristineZip = PristineZipPath();
        var fom = Path.Combine(Path.GetTempPath(), "momi_e2e_" + Path.GetRandomFileName());
        Directory.CreateDirectory(fom);
        try
        {
            // A disposable copy: the pristine archive is the vanilla assets.zip
            File.Copy(pristineZip, Path.Combine(fom, "assets.zip"));

            var modDir = Path.Combine(fom, "TestMod");
            Directory.CreateDirectory(Path.Combine(modDir, "gml", "core"));
            File.WriteAllText(Path.Combine(modDir, "manifest.json"),
                """{"name": "Mod", "version": "1.0", "author": "Example", "minInstallerVersion": "0.12"}""");
            File.WriteAllText(Path.Combine(modDir, "gml", "core", "State.gml"),
                "function example_mod_boot() {\n    mmapi_on(\"save.game_loaded\", example_mod_boot);\n}\n");

            var store = new AssetsStore(fom);
            store.EnsureBackup();

            var gmlMod = GmlModCollector.Collect(FolderMod.FromManifest(modDir));
            Assert.That(gmlMod, Is.Not.Null);

            var (catalogName, catalogBytes) = PayloadResolver.SeamCatalog();
            var catalog = SeamCatalogLoader.Load(catalogBytes, catalogName);

            var gate = GmlCompileGate.Resolve(CompileGateMode.Mandatory);
            GmlLayerPlan plan;
            using (var pristine = new ZipPristineSource(store.BackupPath))
            {
                plan = GmlLayer.Stage(catalog, pristine, [gmlMod!], gate);
            }

            Assert.That(plan.Excluded, Is.Empty);
            Assert.That(plan.Findings.Where(f => f.File.Length > 0), Is.Empty);

            var modifier = store.BeginRebuild();
            modifier.Write("manifest.toml", "");
            foreach (var (rel, bytes) in plan.Added) modifier.Write(rel, bytes);
            foreach (var (rel, staged) in plan.Seamed) modifier.Write(rel, staged.Encode());
            store.Commit();

            using (var live = ZipFile.OpenRead(store.LivePath))
            {
                foreach (var (name, bytes) in PayloadResolver.MmapiSources())
                {
                    var entry = live.GetEntry(SeamStager.MmapiTreePrefix + name);
                    Assert.That(entry, Is.Not.Null, $"{name} missing from the live archive");
                    using var stream = entry!.Open();
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    Assert.That(buffer.ToArray(), Is.EqualTo(bytes), $"{name} differs in the live archive");
                }

                Assert.That(live.GetEntry(SeamStager.HookCatalogRel), Is.Not.Null);
                Assert.That(live.GetEntry("assets/gml/scripts/example_mod/core/State.gml"), Is.Not.Null);

                foreach (var rel in plan.Seamed.Keys)
                    Assert.That(live.GetEntry(rel), Is.Not.Null, $"seamed {rel} missing");
            }

            // Uninstall restores the vanilla archive
            Assert.That(store.Uninstall(), Is.True);
            Assert.That(File.ReadAllBytes(store.LivePath), Is.EqualTo(File.ReadAllBytes(store.BackupPath)));
        }
        finally
        {
            Directory.Delete(fom, true);
        }
    }
}
