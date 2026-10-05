using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

// ScanEnum is additive. The function scan is untouched and still refuses to
// match inside enum bodies (GmlScannerTest covers that). These cover the enum
// shapes the ordinal domain depends on.
[TestFixture]
public class GmlScannerEnumTest
{
    // the shape of the engine's roster enums, with positional members and a LEN sentinel
    private const string Roster = """
        enum NpcId {
            Adeline,
            Balor,
            Caldarus,
            LEN
        }
        """ + "\n";

    [Test]
    public void ShouldReadMembersInOrderWithPositionalValues()
    {
        var scan = GmlScanner.ScanEnum(Roster, "NpcId").Single();

        Assert.That(scan.Members.Select(m => (m.Name, m.Value)), Is.EqualTo(new[]
        {
            ("Adeline", 0L),
            ("Balor", 1L),
            ("Caldarus", 2L),
            ("LEN", 3L),
        }));
        Assert.That(scan.Members, Has.All.Matches<GmlEnumMember>(m => !m.IsExplicit));
    }

    [Test]
    public void ShouldReadExplicitValuesAndResumeNumberingAfterThem()
    {
        // the form the ledger emits, an explicit value after which GML resumes
        // auto-numbering, which is why the sentinel needs no rewrite
        var source = "enum NpcId {\n    Adeline,\n    felix = 34,\n    LEN\n}\n";

        var scan = GmlScanner.ScanEnum(source, "NpcId").Single();

        Assert.That(scan.Members.Select(m => (m.Name, m.Value)), Is.EqualTo(new[]
        {
            ("Adeline", 0L),
            ("felix", 34L),
            ("LEN", 35L),
        }));
        Assert.That(scan.Members[1].IsExplicit, Is.True);
        Assert.That(scan.Members[1].ValueText, Is.EqualTo("34"));
    }

    [Test]
    public void ShouldReadHexAndNegativeExplicitValues()
    {
        var source = "enum Flags {\n    A = 0x10,\n    B = -2,\n    C = $ff,\n}\n";

        var scan = GmlScanner.ScanEnum(source, "Flags").Single();

        Assert.That(scan.Members.Select(m => (m.Name, m.Value)), Is.EqualTo(new[]
        {
            ("A", 16L),
            ("B", -2L),
            ("C", 255L),
        }));
    }

    [Test]
    public void ShouldRecordAnUnparseableValueWithoutGuessingAtIt()
    {
        // the counter carries on, but ValueText is populated so the expander
        // rejects the member rather than computing on an assumption
        var source = "enum Kind {\n    A = OTHER + 1,\n    B,\n}\n";

        var scan = GmlScanner.ScanEnum(source, "Kind").Single();

        Assert.That(scan.Members[0].ValueText, Is.EqualTo("OTHER + 1"));
        Assert.That(scan.Members[0].IsExplicit, Is.True);
        Assert.That(scan.Members[1].Name, Is.EqualTo("B"));
    }

    [Test]
    public void ShouldTolerateATrailingComma()
    {
        var scan = GmlScanner.ScanEnum("enum Kind {\n    A,\n    B,\n}\n", "Kind").Single();

        Assert.That(scan.Members.Select(m => m.Name), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void ShouldIgnoreAnEnumDeclaredInsideAFunction()
    {
        // a nested enum is not the engine's roster enum, and extending it would
        // be meaningless
        var source = "function f() {\n    enum Inner {\n        A,\n        LEN\n    }\n}\n";

        Assert.That(GmlScanner.ScanEnum(source, "Inner"), Is.Empty);
    }

    [Test]
    public void ShouldIgnoreStructLiteralBracesBetweenEnums()
    {
        var source = "var t = { a: 1, b: { c: 2 } };\n" + Roster;

        var scan = GmlScanner.ScanEnum(source, "NpcId").Single();

        Assert.That(scan.Members, Has.Count.EqualTo(4));
    }

    [Test]
    public void ShouldIgnoreCommentsAndStringsAroundTheDeclaration()
    {
        var source = "// enum NpcId { fake }\n"
                     + "var s = \"enum NpcId { alsofake }\";\n"
                     + "/* enum NpcId { blockfake } */\n"
                     + Roster;

        var scan = GmlScanner.ScanEnum(source, "NpcId").Single();

        Assert.That(scan.Members.Select(m => m.Name), Does.Contain("Adeline"));
        Assert.That(scan.Members, Has.Count.EqualTo(4));
    }

    [Test]
    public void ShouldReportEveryDeclarationSoTheCallerCanRequireExactlyOne()
    {
        var source = Roster + "\n" + Roster;

        Assert.That(GmlScanner.ScanEnum(source, "NpcId"), Has.Count.EqualTo(2));
    }

    [Test]
    public void ShouldReturnNothingForAnEnumDeclaredNowhere()
    {
        // the mechanical enforcement of the identity-domain boundary. A native
        // enum such as MonsterId has no GML declaration to scan, so a point
        // over it cannot be authored
        Assert.That(GmlScanner.ScanEnum(Roster, "MonsterId"), Is.Empty);
    }

    [Test]
    public void ShouldLocateTheSentinelLineForSplicing()
    {
        var scan = GmlScanner.ScanEnum(Roster, "NpcId").Single();

        var sentinelLineStart = GmlScanner.LineStart(Roster, scan.Members[^1].Start);

        Assert.That(Roster[sentinelLineStart..], Does.StartWith("    LEN"));
    }

    [Test]
    public void ShouldDetectAFileEndingInsideABlockComment()
    {
        Assert.That(GmlScanner.EndsInsideBlockComment("#macro a b\n/* trailing\n"), Is.True);
        Assert.That(GmlScanner.EndsInsideBlockComment("#macro a b\n/* closed */\n"), Is.False);
        Assert.That(GmlScanner.EndsInsideBlockComment("var s = \"/*\";\n"), Is.False);
        Assert.That(GmlScanner.EndsInsideBlockComment("// /* in a line comment\n"), Is.False);
    }
}
