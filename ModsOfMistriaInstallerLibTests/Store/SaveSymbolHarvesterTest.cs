using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace ModsOfMistriaInstallerLibTests.Store;

// The reseed harvest reads real save containers, so these tests build real
// containers, the same zlib-over-length-prefixed-records format the game
// writes, produced by a test-only writer.
[TestFixture]
public class SaveSymbolHarvesterTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "npc_roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"

        [[extension]]
        id   = "status_effect"
        file = "gml/StatusEffect.gml"

        [extension.ordinal]
        enum     = "StatusEffectId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"
        """ + "\n";

    // Capitalized members, like the real enums. Saves carry the native
    // lowercase form, and the harvester must bridge that gap.
    private const string NpcEnum = """
        enum NpcId {
            Ari,
            Eiland,
            LEN,
        }
        """;

    private const string StatusEnum = """
        enum StatusEffectId {
            Burn,
            WellRested,
            LEN,
        }
        """;

    private static SeamCatalog LoadCatalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

    // The catalog loader normalises `file` values to their archive-entry
    // form, so the pristine keys carry the assets/ prefix.
    private static MemoryPristineSource Pristine() => new(new Dictionary<string, byte[]>
    {
        ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes(NpcEnum),
        ["assets/gml/StatusEffect.gml"] = Encoding.UTF8.GetBytes(StatusEnum),
    });

    // The container format under test, one zlib stream over a u64le record
    // count, then per record u64le name length, name, u64le body length, body.
    private static byte[] Pack(params (string Name, string Body)[] records)
    {
        using var plain = new MemoryStream();
        void U64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            plain.Write(buffer);
        }

        U64((ulong)records.Length);
        foreach (var (name, body) in records)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            U64((ulong)nameBytes.Length);
            plain.Write(nameBytes);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            U64((ulong)bodyBytes.Length);
            plain.Write(bodyBytes);
        }

        using var packed = new MemoryStream();
        using (var deflate = new ZLibStream(packed, CompressionLevel.Fastest, true))
            deflate.Write(plain.ToArray());
        return packed.ToArray();
    }

    private static string WriteSaves(params byte[][] saves)
    {
        var dir = Directory.CreateTempSubdirectory("momi-harvest-test").FullName;
        for (var i = 0; i < saves.Length; i++)
            File.WriteAllBytes(Path.Combine(dir, $"game-1-{i}.sav"), saves[i]);
        return dir;
    }

    [Test]
    public void ShouldHarvestACustomNpcKeyAndIgnoreVanilla()
    {
        var sav = Pack(("npcs", """{"ari":{},"eiland":{},"modauthor_luna":{}}"""));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["npc_roster"].BaseLen, Is.EqualTo(2));
        Assert.That(harvest["npc_roster"].Symbols, Is.EquivalentTo(new[] { "modauthor_luna" }));
    }

    [Test]
    public void ShouldHarvestAnActiveStatusEffectTypeAndSkipNullSlots()
    {
        var sav = Pack(("player",
            """{"stats":{"status_effects":[null,{"type":"burn"},{"type":"modauthor_zeal"}]}}"""));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.EquivalentTo(new[] { "modauthor_zeal" }));
        Assert.That(harvest["npc_roster"].Symbols, Is.Empty,
            "a save with no npcs record contributes nothing to npc_roster");
    }

    [Test]
    public void ShouldUnionAcrossSaves()
    {
        var first = Pack(("player", """{"stats":{"status_effects":[{"type":"burn"},{"type":"modauthor_zeal"}]}}"""));
        var second = Pack(("player", """{"stats":{"status_effects":[{"type":"modauthor_haste"}]}}"""));
        var dir = WriteSaves(first, second);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols,
            Is.EquivalentTo(new[] { "modauthor_zeal", "modauthor_haste" }));
    }

    [Test]
    public void ShouldSkipAnUnreadableSaveWithoutThrowing()
    {
        var good = Pack(("player", """{"stats":{"status_effects":[{"type":"modauthor_zeal"}]}}"""));
        var garbage = Encoding.UTF8.GetBytes("this is not a zlib stream at all");
        var dir = WriteSaves(good, garbage);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.EquivalentTo(new[] { "modauthor_zeal" }),
            "the readable save still harvests when a sibling is corrupt");
    }

    [Test]
    public void ShouldRejectNamesThatAreNotSymbolShaped()
    {
        // A crafted save must not smuggle arbitrary text into generated GML
        // through a symbol, so anything outside the strict lowercase
        // identifier shape is refused at harvest.
        var sav = Pack(("player",
            """{"stats":{"status_effects":[{"type":"burn"},{"type":"Not Valid"},{"type":"UPPER"},{"type":"9starts_with_digit"},{"type":"has-hyphen"}]}}"""));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.Empty);
    }

    [Test]
    public void ShouldSubtractPristineMembersInTheirNativeNameForm()
    {
        // The live regression this pins. Capitalized members subtracted
        // verbatim match nothing, and every vanilla member harvests as
        // custom. The save type "well_rested" must be explained by member
        // WellRested.
        var sav = Pack(("player", """{"stats":{"status_effects":[{"type":"burn"},{"type":"well_rested"}]}}"""));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.Empty);
    }

    [Test]
    public void ShouldConvertMemberNamesLikeTheEngineDoes()
    {
        Assert.That(ExtensionSymbols.ToNativeName("Eiland"), Is.EqualTo("eiland"));
        Assert.That(ExtensionSymbols.ToNativeName("MrBig"), Is.EqualTo("mr_big"));
        Assert.That(ExtensionSymbols.ToNativeName("already_lower"), Is.EqualTo("already_lower"));
    }

    [Test]
    public void ShouldReturnEmptyHarvestWhenPristineScanFails()
    {
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>());
        var sav = Pack(("player", """{"stats":{"status_effects":[{"type":"modauthor_zeal"}]}}"""));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), pristine);

        Assert.That(harvest, Is.Empty,
            "no pristine enum means no vanilla baseline, so the point is skipped rather than guessed");
    }

    [Test]
    public void ShouldCapSymbolsPerPointAndSayThatItDid()
    {
        // a truncated recovery must never present itself as a complete one
        var slots = string.Join(",", Enumerable.Range(0, 300).Select(i => $"{{\"type\":\"flood_{i:d3}\"}}"));
        var sav = Pack(("player", "{\"stats\":{\"status_effects\":[" + slots + "]}}"));
        var dir = WriteSaves(sav);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Has.Count.EqualTo(SaveSymbolHarvester.MaxSymbolsPerPoint));
        Assert.That(harvest["status_effect"].CapHit, Is.True);
    }

    [Test]
    public void ShouldHarvestNothingFromADriftedRecordShape()
    {
        // format drift, where the wanted record parses as JSON but is not the shape
        // the rule understands. Nothing harvests, and the harvester reports
        // the drift instead of presenting silence as health
        var driftedSlots = Pack(("player", """{"stats":{"status_effects":{"0":{"type":"modauthor_zeal"}}}}"""));
        var driftedStats = Pack(("player", """{"someday":{"the":"stats"}}"""));
        var dir = WriteSaves(driftedSlots, driftedStats);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.Empty);
    }

    [Test]
    public void ShouldExposePristineNamesForTheUnionToSubtract()
    {
        // the installer's archive-marker union applies the same subtraction
        // the save path does, through this surface
        var dir = WriteSaves();

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["npc_roster"].PristineNames, Is.EquivalentTo(new[] { "ari", "eiland" }));
        Assert.That(harvest["status_effect"].PristineNames, Is.EquivalentTo(new[] { "burn", "well_rested" }));
    }

    [Test]
    public void ShouldIgnoreFilesOutsideTheSavePattern()
    {
        var real = Pack(("player", """{"stats":{"status_effects":[{"type":"modauthor_zeal"}]}}"""));
        var decoy = Pack(("player", """{"stats":{"status_effects":[{"type":"modauthor_decoy"}]}}"""));
        var dir = WriteSaves(real);
        File.WriteAllBytes(Path.Combine(dir, "game-1-9.sav.bak"), decoy);
        File.WriteAllBytes(Path.Combine(dir, "notes.sav"), decoy);

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine());

        Assert.That(harvest["status_effect"].Symbols, Is.EquivalentTo(new[] { "modauthor_zeal" }));
    }

    [Test]
    public void ShouldHandANoteForEveryLoggedProblemToTheCaller()
    {
        var dir = WriteSaves(Encoding.UTF8.GetBytes("not a vault"));
        List<string> notes = [];

        SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine(), notes);

        Assert.That(notes, Has.Exactly(1).Contains("unreadable save skipped"));
    }

    [Test]
    public void ShouldTreatAnOccupiedSlotWithoutATypeAsDrift()
    {
        // An occupied slot without a type string is an unrecognized shape,
        // not an empty slot.
        var sav = Pack(("player", """{"stats":{"status_effects":[null,{"kind":"modauthor_zeal"}]}}"""));
        var dir = WriteSaves(sav);
        List<string> notes = [];

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine(), notes);

        Assert.That(harvest["status_effect"].Symbols, Is.Empty);
        Assert.That(notes, Has.Exactly(1).Contains("unexpected shape"));
    }

    [Test]
    public void ShouldTreatAnNpcsRecordWithNoVanillaNameAsDrift()
    {
        // A real npcs record carries the vanilla roster. A record with no
        // vanilla name among its keys is some other shape, and harvesting
        // it would mint stray symbols.
        var sav = Pack(("npcs", """{"entries":{},"roster":{}}"""));
        var dir = WriteSaves(sav);
        List<string> notes = [];

        var harvest = SaveSymbolHarvester.Harvest(dir, LoadCatalog(), Pristine(), notes);

        Assert.That(harvest["npc_roster"].Symbols, Is.Empty);
        Assert.That(notes, Has.Exactly(1).Contains("unexpected shape"));
    }
}
