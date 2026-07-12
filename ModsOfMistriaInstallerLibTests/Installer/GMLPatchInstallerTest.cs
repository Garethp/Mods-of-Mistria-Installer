using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Installer;

public class GMLPatchInstallerTest
{
    private const string Target = "gml/scripts/example.gml";
    private const string AnchorFile = "patches/anchor.gml";
    private const string ContentFile = "patches/content.gml";

    [TestCase("insert_before")]
    [TestCase("insert_after")]
    [TestCase("replace_exact")]
    public void AppliesSupportedOperations(string operation)
    {
        var modifier = Modifier((Target, "alpha\nANCHOR\nomega"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch("example.test.operation", operation)]);
        var statuses = new List<string>();

        Installer(modifier).Install(mod, (message, _) => statuses.Add(message));

        var result = modifier.GetFile(Destination(Target));
        var begin = result.IndexOf("// MOMI_GML_PATCH_BEGIN example.test.operation", StringComparison.Ordinal);
        var content = result.IndexOf("CONTENT", StringComparison.Ordinal);
        var end = result.IndexOf("// MOMI_GML_PATCH_END example.test.operation", StringComparison.Ordinal);
        var anchor = result.IndexOf("ANCHOR", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.GreaterThanOrEqualTo(0));
            Assert.That(content, Is.GreaterThan(begin));
            Assert.That(end, Is.GreaterThan(content));
            Assert.That(statuses, Has.Count.EqualTo(1));
        });

        if (operation == "insert_before")
            Assert.That(anchor, Is.GreaterThan(end));
        else if (operation == "insert_after")
            Assert.That(anchor, Is.LessThan(begin));
        else
            Assert.That(anchor, Is.EqualTo(-1));
    }

    [TestCase("\r\n", "\n")]
    [TestCase("\n", "\r\n")]
    public void MatchesAcrossNewLineStylesAndPreservesTargetStyle(
        string targetNewLine,
        string patchNewLine)
    {
        var original = string.Join(targetNewLine, ["alpha", "ANCHOR", "omega", ""]);
        var content = string.Join(patchNewLine, ["first();", "second();"]);
        var modifier = Modifier((Target, original));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, content)],
            [Patch("example.test.newlines", "insert_after")]);

        Installer(modifier).Install(mod, (_, _) => { });

        var result = modifier.GetFile(Destination(Target));
        if (targetNewLine == "\r\n")
        {
            var withoutCrLf = result.Replace("\r\n", "", StringComparison.Ordinal);
            Assert.Multiple(() =>
            {
                Assert.That(withoutCrLf, Does.Not.Contain("\r"));
                Assert.That(withoutCrLf, Does.Not.Contain("\n"));
            });
        }
        else
        {
            Assert.That(result, Does.Not.Contain("\r"));
        }
    }

    [TestCase("no matching text")]
    [TestCase("ANCHOR\nANCHOR")]
    public void RejectsUnexpectedMatchCountWithoutWriting(string original)
    {
        var modifier = Modifier((Target, original));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch("example.test.match-count", "insert_after")]);

        Assert.Throws<InvalidOperationException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));
        Assert.That(modifier.TotalWrites, Is.Zero);
    }

    [Test]
    public void AppliesTheDeclaredNumberOfMatches()
    {
        var modifier = Modifier((Target, "ANCHOR\nmiddle\nANCHOR"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch("example.test.two-matches", "insert_after", expectedMatches: 2)]);

        Installer(modifier).Install(mod, (_, _) => { });

        Assert.That(
            CountOccurrences(
                modifier.GetFile(Destination(Target)),
                "// MOMI_GML_PATCH_BEGIN example.test.two-matches"),
            Is.EqualTo(2));
    }

    [TestCase("data/scripts/example.gml")]
    [TestCase("assets/gml/scripts/example.gml")]
    [TestCase("gml/scripts/example.txt")]
    [TestCase("gml/../data/example.gml")]
    [TestCase("gml\\..\\data\\example.gml")]
    [TestCase("C:\\game\\gml\\example.gml")]
    [TestCase("/gml/scripts/example.gml")]
    public void RejectsUnsafeOrOutOfScopeTargets(string target)
    {
        var modifier = Modifier((Target, "ANCHOR"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch("example.test.path", "insert_after", target)]);

        Assert.Throws<InvalidDataException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));
        Assert.That(modifier.TotalWrites, Is.Zero);
    }

    [TestCase("")]
    [TestCase("contains space")]
    [TestCase("contains/slash")]
    [TestCase("contains\nnewline")]
    [TestCase("nonascii.é")]
    public void RejectsUnsafePatchIds(string id)
    {
        var modifier = Modifier((Target, "ANCHOR"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch(id, "insert_after")]);

        Assert.Throws<InvalidDataException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));
        Assert.That(modifier.TotalWrites, Is.Zero);
    }

    [TestCase(AnchorFile)]
    [TestCase(ContentFile)]
    public void RejectsMissingPatchSourceFiles(string pathToOmit)
    {
        var sourceFiles = new List<(string Path, string Content)>
        {
            (AnchorFile, "ANCHOR"),
            (ContentFile, "CONTENT")
        };
        sourceFiles.RemoveAll(file => file.Path == pathToOmit);
        var modifier = Modifier((Target, "ANCHOR"));
        var mod = Mod(
            sourceFiles,
            [Patch("example.test.missing-source", "insert_after")]);

        Assert.Throws<FileNotFoundException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));
        Assert.That(modifier.TotalWrites, Is.Zero);
    }

    [Test]
    public void RejectsDuplicatePatchIdsIgnoringCase()
    {
        var modifier = Modifier((Target, "ANCHOR"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [
                Patch("example.test.duplicate", "insert_after"),
                Patch("EXAMPLE.TEST.DUPLICATE", "insert_before")
            ]);

        Assert.Throws<InvalidDataException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));
        Assert.That(modifier.TotalWrites, Is.Zero);
    }

    [Test]
    public void RejectsApplyingTheSamePatchTwice()
    {
        var modifier = Modifier((Target, "ANCHOR"));
        var mod = Mod(
            [(AnchorFile, "ANCHOR"), (ContentFile, "CONTENT")],
            [Patch("example.test.reapply", "insert_after")]);
        var installer = Installer(modifier);

        installer.Install(mod, (_, _) => { });

        Assert.Throws<InvalidOperationException>(() =>
            installer.Install(mod, (_, _) => { }));
        Assert.That(modifier.WritesFor(Destination(Target)), Is.EqualTo(1));
    }

    [Test]
    public void AppliesSequentialPatchesAndWritesTargetOnce()
    {
        const string secondAnchor = "patches/second-anchor.gml";
        const string secondContent = "patches/second-content.gml";
        var modifier = Modifier((Target, "ONE\nmiddle\nTWO"));
        var mod = Mod(
            [
                (AnchorFile, "ONE"),
                (ContentFile, "first();"),
                (secondAnchor, "TWO"),
                (secondContent, "second();")
            ],
            [
                Patch("example.test.first", "insert_after"),
                new GmlPatchDefinition(
                    "example.test.second",
                    Target,
                    "insert_before",
                    secondAnchor,
                    secondContent)
            ]);

        Installer(modifier).Install(mod, (_, _) => { });

        var result = modifier.GetFile(Destination(Target));
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("first();"));
            Assert.That(result, Does.Contain("second();"));
            Assert.That(modifier.WritesFor(Destination(Target)), Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidationFailureDoesNotPartiallyWriteAnotherTarget()
    {
        const string otherTarget = "gml/scripts/other.gml";
        const string otherAnchor = "patches/other-anchor.gml";
        const string otherContent = "patches/other-content.gml";
        var modifier = Modifier((Target, "FIRST"), (otherTarget, "SECOND"));
        var mod = Mod(
            [
                (AnchorFile, "FIRST"),
                (ContentFile, "first();"),
                (otherAnchor, "NOT PRESENT"),
                (otherContent, "second();")
            ],
            [
                Patch("example.test.atomic-first", "insert_after"),
                new GmlPatchDefinition(
                    "example.test.atomic-second",
                    otherTarget,
                    "insert_after",
                    otherAnchor,
                    otherContent)
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            Installer(modifier).Install(mod, (_, _) => { }));

        Assert.Multiple(() =>
        {
            Assert.That(modifier.TotalWrites, Is.Zero);
            Assert.That(modifier.GetFile(Destination(Target)), Is.EqualTo("FIRST"));
            Assert.That(modifier.GetFile(Destination(otherTarget)), Is.EqualTo("SECOND"));
        });
    }

    private static GMLPatchInstaller Installer(IFileModifier modifier) =>
        new(new Dictionary<string, string>(), modifier);

    private static GmlPatchDefinition Patch(
        string id,
        string operation,
        string target = Target,
        int expectedMatches = 1) =>
        new(id, target, operation, AnchorFile, ContentFile, expectedMatches);

    private static PatchMockMod Mod(
        IEnumerable<(string Path, string Content)> files,
        List<GmlPatchDefinition> patches) =>
        new(files.ToDictionary(file => file.Path, file => file.Content), patches);

    private static RecordingFileModifier Modifier(params (string Target, string Content)[] files) =>
        new(files.ToDictionary(file => Destination(file.Target), file => file.Content));

    private static string Destination(string target) =>
        Path.Combine("assets", target.Replace('/', Path.DirectorySeparatorChar));

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private sealed class PatchMockMod(
        Dictionary<string, string> files,
        List<GmlPatchDefinition> patches)
        : MockMod(files), IMod
    {
        public List<GmlPatchDefinition> GetGmlPatches() => patches;
    }

    private sealed class RecordingFileModifier(Dictionary<string, string> files) : IFileModifier
    {
        private readonly MockFileModifier _inner = new(files);
        private readonly Dictionary<string, int> _writes = new(StringComparer.OrdinalIgnoreCase);

        public int TotalWrites => _writes.Values.Sum();
        public int WritesFor(string path) => _writes.GetValueOrDefault(path);
        public string GetFile(string path) => _inner.GetFile(path);
        public bool Exists(string file) => _inner.Exists(file);
        public string Read(string file) => _inner.Read(file);
        public Stream GetReadStream(string file) => _inner.GetReadStream(file);
        public string[] FindFiles(string path, string pattern) => _inner.FindFiles(path, pattern);

        public void Write(string file, string contents)
        {
            _writes[file] = WritesFor(file) + 1;
            _inner.Write(file, contents);
        }

        public Stream GetWriteStream(string file) => _inner.GetWriteStream(file);
        public bool ConditionalRestoreBackup(string file, Func<bool> condition) =>
            _inner.ConditionalRestoreBackup(file, condition);
    }
}
