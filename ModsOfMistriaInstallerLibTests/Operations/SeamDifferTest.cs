using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Operations;

// The triage diff between two fabricated builds. Identical regions pass,
// whitespace and comment drift stay invisible, real changes render a line diff,
// vanished locators report as missing, and a modded side refuses. The diff
// never writes.
[TestFixture]
public class SeamDifferTest
{
    private const string CatalogToml = """
        version = 2

        [[hook]]
        name = "test.filter"
        kind = "filter"
        doc  = "A test filter."

        [[hook]]
        name = "test.event"
        kind = "event"
        doc  = "A test event."

        [[seam]]
        id      = "tfilter"
        file    = "gml/F.gml"
        target  = { fn = "regen", at = "head" }
        op      = "filter"
        hook    = "test.filter"
        var     = "amount"
        ctx     = "{ cap: max_amount }"

        [[seam]]
        id      = "tctx"
        file    = "gml/G.gml"
        context_before = '''
            play_sound(track);
        '''
        op      = "emit"
        hook    = "test.event"
        ctx     = "{ track: track, volume: base_volume }"
        """;

    private const string RegenPristine =
        "function regen(amount, max_amount) {\n"
        + "    hp = min(hp + amount, max_amount);\n"
        + "}\n";

    private const string MusicPristine =
        "function play_music(track) {\n"
        + "    var base_volume = 0.8;\n"
        + "    play_sound(track);\n"
        + "}\n";

    private static readonly SeamCatalog Catalog =
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(CatalogToml), "synthetic");

    private static MemoryPristineSource Pristine(string regen, string music) =>
        new(new Dictionary<string, byte[]>
        {
            ["assets/gml/F.gml"] = Encoding.UTF8.GetBytes(regen),
            ["assets/gml/G.gml"] = Encoding.UTF8.GetBytes(music),
        });

    private static SeamDiffResult Diff(string oldRegen, string oldMusic, string newRegen, string newMusic) =>
        SeamDiffer.Diff(Pristine(oldRegen, oldMusic), Pristine(newRegen, newMusic), Catalog);

    [Test]
    public void ShouldPassIdenticalBuilds()
    {
        var result = Diff(RegenPristine, MusicPristine, RegenPristine, MusicPristine);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Entries, Has.All.Matches<SeamRegionDiff>(
            e => e.Status == SeamRegionStatus.Unchanged));
        Assert.That(SeamDiffer.RenderText(result, "old.zip", "new.zip"), Does.Contain("OK"));
    }

    [Test]
    public void ShouldIgnoreWhitespaceAndCommentDrift()
    {
        var reformatted = RegenPristine
            .Replace("    hp = min", "        hp   =  min // rebalanced someday\n    ");

        var result = Diff(RegenPristine, MusicPristine, reformatted, MusicPristine);

        Assert.That(result.Ok, Is.True);
    }

    [Test]
    public void ShouldRenderALineDiffForAChangedTargetFunction()
    {
        var changed = RegenPristine.Replace("hp = min(hp + amount, max_amount);",
            "hp = min(hp + amount * 2, max_amount);");

        var result = Diff(RegenPristine, MusicPristine, changed, MusicPristine);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        var region = result.Entries.Single(e => e.EntryId == "tfilter");
        Assert.That(region.Status, Is.EqualTo(SeamRegionStatus.Changed));
        Assert.That(region.Region, Is.EqualTo("function 'regen'"));
        Assert.That(region.DiffLines, Has.Some.StartsWith("-     hp = min(hp + amount,"));
        Assert.That(region.DiffLines, Has.Some.StartsWith("+     hp = min(hp + amount * 2,"));
        Assert.That(result.Entries.Single(e => e.EntryId == "tctx").Status,
            Is.EqualTo(SeamRegionStatus.Unchanged));
    }

    [Test]
    public void ShouldCompareTheFunctionEnclosingATextAnchor()
    {
        // the anchored line itself is untouched, and only code above it changed
        var changed = MusicPristine.Replace("var base_volume = 0.8;", "var base_volume = 0.5;");

        var result = Diff(RegenPristine, MusicPristine, RegenPristine, changed);

        var region = result.Entries.Single(e => e.EntryId == "tctx");
        Assert.That(region.Status, Is.EqualTo(SeamRegionStatus.Changed));
        Assert.That(region.Region, Is.EqualTo("function 'play_music' around the anchor"));
    }

    [Test]
    public void ShouldElideUnchangedStretchesFromTheDiff()
    {
        // one changed line deep in a long body prints only its neighborhood
        var filler = string.Concat(Enumerable.Range(0, 30).Select(i => $"    filler_{i}();\n"));
        var longOld = "function regen(amount, max_amount) {\n" + filler
                      + "    hp = min(hp + amount, max_amount);\n}\n";
        var longNew = longOld.Replace("hp + amount", "hp + amount * 2");

        var result = Diff(longOld, MusicPristine, longNew, MusicPristine);

        var region = result.Entries.Single(e => e.EntryId == "tfilter");
        Assert.That(region.RegionStartLine, Is.EqualTo(1));
        Assert.That(region.DiffLines, Has.Some.EqualTo("  ... (28 lines unchanged, resuming at 29)"));
        Assert.That(region.DiffLines, Has.None.Contains("filler_5();"));
        Assert.That(region.DiffLines, Has.Some.Contains("filler_29();"));
        Assert.That(region.DiffLines.Count, Is.LessThan(12));
    }

    [Test]
    public void ShouldReportAVanishedLocatorAsMissing()
    {
        var renamed = RegenPristine.Replace("function regen(", "function regenerate(");

        var result = Diff(RegenPristine, MusicPristine, renamed, MusicPristine);

        var region = result.Entries.Single(e => e.EntryId == "tfilter");
        Assert.That(region.Status, Is.EqualTo(SeamRegionStatus.NewMissing));
        Assert.That(region.Note, Does.Contain("defined 0x"));
        Assert.That(result.ExitCode, Is.EqualTo(1));
    }

    [Test]
    public void ShouldRefuseAModdedBuild()
    {
        // the default marker for a template seam is mmapi_<id>, so a build
        // carrying it is a modded build, not a pristine one
        var modded = RegenPristine.Replace("    hp = min",
            "    amount = mmapi_apply_filters(\"test.filter\", amount, { cap: max_amount }); // mmapi_tfilter\n    hp = min");

        var result = Diff(RegenPristine, MusicPristine, modded, MusicPristine);

        Assert.That(result.ModdedMarkers, Has.Count.EqualTo(1));
        Assert.That(result.ModdedMarkers[0], Does.Contain("the new"));
        Assert.That(SeamDiffer.RenderText(result, "old.zip", "new.zip"), Does.Contain("REFUSED"));
    }

    [Test]
    public void ShouldCarryTheContractInJson()
    {
        var changed = RegenPristine.Replace("hp + amount", "hp + amount * 2");
        var result = Diff(RegenPristine, MusicPristine, changed, MusicPristine);

        var payload = JObject.Parse(SeamDiffer.ToJson(result, "old.zip", "new.zip"));

        Assert.That(payload["ok"]!.Value<bool>(), Is.False);
        Assert.That(payload["changed"]!.Value<int>(), Is.EqualTo(1));
        var entries = (JArray)payload["entries"]!;
        Assert.That(entries, Has.Count.EqualTo(2));
        var tfilter = entries.Single(e => e["id"]!.Value<string>() == "tfilter");
        Assert.That(tfilter["status"]!.Value<string>(), Is.EqualTo("changed"));
        Assert.That(tfilter["region_start_line"]!.Value<int>(), Is.EqualTo(1));
        Assert.That(tfilter["old_text"]!.Value<string>(), Does.Contain("hp + amount,"));
        Assert.That(tfilter["new_text"]!.Value<string>(), Does.Contain("hp + amount * 2"));
        Assert.That((JArray)tfilter["diff"]!, Is.Not.Empty);
    }
}
