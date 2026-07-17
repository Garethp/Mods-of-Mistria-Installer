using System.Diagnostics;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Tools;

[TestFixture]
public class GmlCompileGateTest
{
    private string _tempDir = "";

    [SetUp]
    public void CreateTempDir()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "momi_compile_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    // The binary-backed tests resolve through this; the pin-discipline guard
    // below deliberately does not, so it runs offline wherever the repo is.
    private static GmlCompileGate RequireGate()
    {
        var checker = GmlCompileGate.ResolveChecker();
        if (checker is null)
            Assert.Ignore("needs the momi-gml-check binary (cargo build --release "
                          + "--manifest-path tools/checker/Cargo.toml), or set MOMI_GML_CHECKER");
        return new GmlCompileGate(checker);
    }

    [TearDown]
    public void RemoveTempDir()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Test]
    public void ShouldCompileTheFrameworkAndGeneratedCatalog()
    {
        var gate = RequireGate();
        var (name, bytes) = PayloadResolver.SeamCatalog();
        var catalog = SeamCatalogLoader.Load(bytes, name);
        var payloadDir = Path.Combine(AppContext.BaseDirectory, "Payload", "mmapi");
        var targets = Directory.GetFiles(payloadDir, "*.gml")
            .Order(StringComparer.Ordinal)
            .Select(src => Write(Path.GetFileName(src), File.ReadAllText(src)))
            .ToList();
        targets.Add(Write("mmapi_hook_catalog.gml", HookCatalogRenderer.Render(catalog)));

        Assert.DoesNotThrow(() => gate.RunFiles(targets));
    }

    [Test]
    public void ShouldRejectCrossChunkDuplicateExportsInUnitMode()
    {
        // the per-mod gate's boot-fidelity mode: two chunks exporting the same
        // top-level function compile fine independently but fail as one unit
        var gate = RequireGate();
        var a = Write("a.gml", "function dup_fn() {\n}\n");
        var b = Write("b.gml", "function dup_fn() {\n}\n");

        Assert.DoesNotThrow(() => gate.RunFiles([a, b]));

        var caught = Assert.Throws<InvalidOperationException>(() => gate.RunUnit([a, b]));
        Assert.That(caught!.Message.ToLowerInvariant(), Does.Contain("duplicate"));
    }

    [Test]
    public void ShouldReportACompileFailureWithTheCompilerDetail()
    {
        var gate = RequireGate();
        var bad = Write("bad.gml", "function nope( {\n");

        var caught = Assert.Throws<InvalidOperationException>(() => gate.RunFiles([bad]));

        Assert.That(caught!.Message, Does.Contain("compile pass FAILED"));
        Assert.That(caught.Message, Does.Contain("bad.gml"));
    }

    [Test]
    public void ShouldIgnoreBlankListfileLines()
    {
        var gate = RequireGate();
        var ok = Write("ok.gml", "function fine() {\n    return 1;\n}\n");

        Assert.DoesNotThrow(() => gate.RunFiles([ok, ok]));
    }

    [Test]
    public void ShouldThrowWhenAStagedFileVanished()
    {
        var gate = RequireGate();
        var missing = Path.Combine(_tempDir, "gone.gml");

        var caught = Assert.Throws<InvalidOperationException>(() => gate.RunFiles([missing]));

        Assert.That(caught!.Message, Does.Contain("vanished"));
    }

    [Test]
    public void ShouldReportTheFabricatorPinFromVersion()
    {
        RequireGate();
        var checker = GmlCompileGate.ResolveChecker()!;
        var startInfo = new ProcessStartInfo(checker)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("--version");

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.EqualTo(0));
        Assert.That(stdout, Does.StartWith("momi-gml-check "));
        Assert.That(stdout, Does.Contain(CrateFabricatorPin()));
    }

    [Test]
    public void ShouldNeverResolveAGateWhenOff()
    {
        Assert.That(GmlCompileGate.Resolve(CompileGateMode.Off), Is.Null);
    }

    [Test]
    public void ShouldResolveAnAutoGateWhenABackendExists()
    {
        RequireGate();

        var gate = GmlCompileGate.Resolve(CompileGateMode.Auto);

        Assert.That(gate, Is.Not.Null);
        Assert.That(gate!.Available, Is.True);
    }

    [Test]
    public void ShouldErrorWhenMandatoryAndNoBackendResolves()
    {
        // The inverse gate of the tests above: this one is only meaningful
        // where no backend exists, which is a plain clone. CI always has one.
        if (GmlCompileGate.ResolveChecker() is not null)
            Assert.Ignore("a backend resolves here; this pins the no-backend path");

        var caught = Assert.Throws<InvalidOperationException>(
            () => GmlCompileGate.Resolve(CompileGateMode.Mandatory));

        Assert.That(caught!.Message, Does.Contain("mandatory"));
    }

    [Test]
    public void ShouldFailLoudlyOnAMissingCheckerOverride()
    {
        // set-but-missing never silently demotes to no gate, in any mode
        var missing = Path.Combine(_tempDir, "not-a-checker.exe");
        Environment.SetEnvironmentVariable("MOMI_GML_CHECKER", missing);

        try
        {
            Assert.Throws<FileNotFoundException>(() => GmlCompileGate.Resolve(CompileGateMode.Auto));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOMI_GML_CHECKER", null);
        }
    }

    // GATE-004's drift guard: the pin lives in five places (four Cargo.toml
    // dependency revs and rev.rs), and a partial re-pin would ship a binary
    // that misreports the VM it was built against. Offline and always on.
    [Test]
    public void ShouldAgreeOnTheFabricatorPinAcrossTheCrate()
    {
        var manifest = File.ReadAllText(CratePath("Cargo.toml"));
        var revs = Regex.Matches(manifest, @"fabricator\.git"", rev = ""([0-9a-f]{40})""")
            .Select(m => m.Groups[1].Value)
            .ToList();

        Assert.That(revs, Has.Count.EqualTo(4), "expected four pinned fabricator dependencies");
        Assert.That(revs.Distinct().Count(), Is.EqualTo(1),
            "the four Cargo.toml fabricator revs disagree - re-pin all five places at once");
        Assert.That(CrateFabricatorPin(), Is.EqualTo(revs[0]),
            "src/rev.rs disagrees with Cargo.toml - the binaries would misreport their VM");
    }

    private static string CrateFabricatorPin()
    {
        var revSource = File.ReadAllText(CratePath(Path.Combine("src", "rev.rs")));
        var match = Regex.Match(revSource, @"FABRICATOR_REV: &str = ""([0-9a-f]{40})""");
        Assert.That(match.Success, Is.True, "no FABRICATOR_REV found in src/rev.rs");
        return match.Groups[1].Value;
    }

    private static string CratePath(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ModsOfMistriaInstaller.sln")))
            dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("ModsOfMistriaInstaller.sln not found above the tests");
        return Path.Combine(dir.FullName, "tools", "checker", relative);
    }
}
