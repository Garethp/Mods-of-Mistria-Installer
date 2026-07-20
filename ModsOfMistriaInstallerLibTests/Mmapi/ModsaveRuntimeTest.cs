using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Mmapi;

// mmapi's per-save sidecars and config, tested by running them. The bug these
// guard against loses player data silently: a crash partway through a save
// leaves a truncated file, which reads back as undefined, which is exactly what
// a fresh save hands load_fn - so the mod's state resets and nothing says why.
//
// The load-bearing decision is the ordering: the .bak is written from the old
// contents before each primary write, so a failed backup leaves the primary's
// old good state and a failed primary leaves the previous state in .bak.
//
// Testable off-engine because _files.gml is a fake in-memory filesystem and
// mmapi's file surface is late-bound engine names. That matters most for the
// corrupt-file case: no real filesystem produces one on demand.
[TestFixture]
public class ModsaveRuntimeTest
{
    private static void AssertBodyPasses(string name)
    {
        // The stub filesystem goes first, ahead of the prelude: it defines the
        // engine names the framework late-binds, and the body calls
        // test_fs_reset at its top.
        var result = GmlProbe.RunWithFramework(
        [
            GmlProbe.Fixture(Path.Combine("modsave_runtime", "_files.gml")),
            GmlProbe.Fixture("gml_prelude.gml"),
            GmlProbe.Fixture(Path.Combine("modsave_runtime", name)),
        ]);

        GmlProbe.AssertAllPass(result, name);
    }

    [Test]
    public void ShouldKeepALastGoodModsaveBackup()
    {
        // The .bak carries the previous contents, a corrupt primary recovers
        // from it on load and says so, a corrupt primary never clobbers the
        // good .bak, an absent primary still loads as a fresh save, and a
        // failing backup never costs the real save.
        AssertBodyPasses("modsave_backup.gml");
    }

    [Test]
    public void ShouldKeepALastGoodConfigBackup()
    {
        // mmapi_config_save/load get the same treatment, on the same shared
        // helpers.
        AssertBodyPasses("config_backup.gml");
    }
}
