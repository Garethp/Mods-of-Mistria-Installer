using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Mmapi;

// The harness itself, against synthetic probes with known verdicts.
//
// The suites above are only as good as the runner's willingness to report a
// failure, and this suite is the reason theirs is credible: the original ran
// green for weeks while executing nothing. Green must mean checked. These are
// the negative controls that make the PASSes evidence rather than decor.
[TestFixture]
public class ProbeHarnessTest
{
    private string _tempDir = "";

    [SetUp]
    public void RequireProbeAndTempDir()
    {
        // These assert that the runner reports failure, so they catch
        // AssertionException. Without that guard a missing probe would surface
        // as the wrong exception type and fail rather than skip.
        GmlProbe.RequireProbe();

        _tempDir = Path.Combine(Path.GetTempPath(), "momi_probe_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void RemoveTempDir()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    // A throwaway probe. The extension is load-bearing: it selects the compat
    // dialect.
    private string Synthetic(string body)
    {
        var probe = Path.Combine(_tempDir, "synthetic.gml");
        File.WriteAllText(probe, body);
        return probe;
    }

    private void AssertAllPass(string probe) =>
        GmlProbe.AssertAllPass(GmlProbe.RunGml(probe), Path.GetFileName(probe));

    [Test]
    public void ShouldCatchAProbeReportingFail()
    {
        // A FAIL line fails the suite. This is the whole point: if a quirk
        // comes back, a test goes red rather than a mod misbehaving.
        var probe = Synthetic(
            "show_debug_message(\"PASS one\");\n"
            + "show_debug_message(\"FAIL two -- the quirk reproduced\");\n");

        var caught = Assert.Throws<AssertionException>(() => AssertAllPass(probe));

        Assert.That(caught!.Message, Does.Contain("reported FAIL"));
    }

    [Test]
    public void ShouldCatchAProbeAssertingNothing()
    {
        // A probe that prints no PASS is a vacuous pass, not a pass.
        var probe = Synthetic("var x = 1;\n");

        var caught = Assert.Throws<AssertionException>(() => AssertAllPass(probe));

        Assert.That(caught!.Message, Does.Contain("asserted nothing"));
    }

    [Test]
    public void ShouldCatchAProbeThatThrows()
    {
        // An uncaught throw is a non-zero exit, so a probe that dies halfway
        // cannot bank the PASSes it printed first.
        var probe = Synthetic(
            "show_debug_message(\"PASS one\");\n"
            + "function boom() { throw \"kaboom\"; }\n"
            + "boom();\n");

        var caught = Assert.Throws<AssertionException>(() => AssertAllPass(probe));

        Assert.That(caught!.Message, Does.Contain("did not run"));
    }

    [Test]
    public void ShouldCatchAProbeThatDoesNotCompile()
    {
        // A malformed probe is a failure, not a skip.
        var probe = Synthetic("function nope( {\n");

        var caught = Assert.Throws<AssertionException>(() => AssertAllPass(probe));

        Assert.That(caught!.Message, Does.Contain("did not run"));
    }
}
