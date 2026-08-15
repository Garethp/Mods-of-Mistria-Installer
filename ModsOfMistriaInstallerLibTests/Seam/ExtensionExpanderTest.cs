using System.Security.Cryptography;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Seam;

// Golden tests over synthetic pristine, with exact expected text, so a change in
// what the expander emits has to be looked at rather than absorbed.
[TestFixture]
public class ExtensionExpanderTest
{
    private const string NpcIdRel = "assets/gml/NpcId.gml";
    private const string ManifestRel = "assets/gml/object_manifest.gml";

    // the vanilla shape in miniature, a roster enum with a LEN sentinel and the
    // two mapping switches, one with a fatal default, one with a soft one
    private const string NpcIdPristine =
        "enum NpcId {\n"
        + "    Adeline,\n"
        + "    Balor,\n"
        + "    LEN\n"
        + "}\n"
        + "\n"
        + "function npc_id_to_gm_obj_id(npc_id) {\n"
        + "    switch (npc_id) {\n"
        + "        default: impossible(\"Unexpected NpcId: {}\", npc_id);\n"
        + "    }\n"
        + "}\n"
        + "\n"
        + "function gm_obj_id_to_npc_id(obj) {\n"
        + "    switch (obj) {\n"
        + "        default: return undefined;\n"
        + "    }\n"
        + "}\n";

    private const string ManifestPristine =
        "#macro obj_adeline object(\"obj_adeline\")\n"
        + "#macro obj_balor object(\"obj_balor\")\n";

    private const string Point = """
        version = 2

        [[extension]]
        id   = "roster"
        file = "gml/NpcId.gml"
        doc  = "One registration per custom NPC."

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.fields]]
        name = "object"
        type = "identifier"
        doc  = "GML object name."

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [[extension.sites]]
        id       = "id_to_obj"
        kind     = "anchor"
        anchor   = '        default: impossible("Unexpected NpcId: {}", npc_id);'
        place    = "before"
        template = "case NpcId.{{symbol}}: return {{object}};"
        indent   = 8

        [[extension.sites]]
        id       = "obj_to_id"
        kind     = "anchor"
        anchor   = "        default: return undefined;"
        place    = "before"
        template = "case {{object}}: return NpcId.{{symbol}};"
        indent   = 8

        [[extension.sites]]
        id       = "object_macro"
        kind     = "append"
        file     = "gml/object_manifest.gml"
        template = '#macro {{object}} object("{{object}}")'

        [extension.vacancy]
        enum_member  = "{{symbol}} = {{ordinal}},"
        id_to_obj    = "case NpcId.{{symbol}}: return obj_mmapi_npc_vacant;"
        obj_to_id    = ""
        object_macro = ""
        """ + "\n";

    private static SeamCatalog Load(string text) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text), "seams.toml");

    private static MemoryPristineSource Pristine(params (string Rel, string Text)[] files) =>
        new(files.ToDictionary(f => f.Rel, f => Encoding.UTF8.GetBytes(f.Text)));

    private static MemoryPristineSource DefaultPristine() =>
        Pristine((NpcIdRel, NpcIdPristine), (ManifestRel, ManifestPristine));

    private static ExtensionRegistration Reg(string symbol, string obj, string modId = "mod.x") =>
        new("roster", symbol, symbol, modId, new Dictionary<string, string> { ["object"] = obj });

    // Stage the point over pristine with no seams in play.
    private static Dictionary<string, StagedFile> Expand(
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger? ledger = null,
        string? catalogText = null,
        IPristineSource? pristine = null)
    {
        var catalog = Load(catalogText ?? Point);
        var source = pristine ?? DefaultPristine();
        var staged = SeamStager.Simulate(catalog, source);
        ExtensionExpander.Expand(catalog, regs, ledger ?? new MemoryExtensionLedger(), staged, source);
        return staged;
    }

    private static SeamStagingException ExpandFails(
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger? ledger = null,
        string? catalogText = null,
        IPristineSource? pristine = null) =>
        Assert.Throws<SeamStagingException>(() => Expand(regs, ledger, catalogText, pristine))!;

    [Test]
    public void ShouldRenderOneRegistrantAtEverySite()
    {
        var staged = Expand([Reg("modx_luna", "modx_luna_obj")]);

        Assert.That(staged[NpcIdRel].Text, Is.EqualTo(
            "enum NpcId {\n"
            + "    Adeline,\n"
            + "    Balor,\n"
            + "    modx_luna = 2, // mmapi_ext:roster:enum_member:modx_luna\n"
            + "    LEN\n"
            + "}\n"
            + "\n"
            + "function npc_id_to_gm_obj_id(npc_id) {\n"
            + "    switch (npc_id) {\n"
            + "        case NpcId.modx_luna: return modx_luna_obj; "
            + "// mmapi_ext:roster:id_to_obj:modx_luna\n"
            + "        default: impossible(\"Unexpected NpcId: {}\", npc_id);\n"
            + "    }\n"
            + "}\n"
            + "\n"
            + "function gm_obj_id_to_npc_id(obj) {\n"
            + "    switch (obj) {\n"
            + "        case modx_luna_obj: return NpcId.modx_luna; "
            + "// mmapi_ext:roster:obj_to_id:modx_luna\n"
            + "        default: return undefined;\n"
            + "    }\n"
            + "}\n"));
    }

    [Test]
    public void ShouldAppendToASecondFile()
    {
        var staged = Expand([Reg("modx_luna", "modx_luna_obj")]);

        Assert.That(staged[ManifestRel].Text, Is.EqualTo(
            ManifestPristine
            + "\n"
            + "#macro modx_luna_obj object(\"modx_luna_obj\") "
            + "// mmapi_ext:roster:object_macro:modx_luna\n"));
    }

    [Test]
    public void ShouldGiveTwoSitesInOneFileDistinctMarkers()
    {
        // the spec bug this component exists for. Without {site} in the marker,
        // the second site to splice trips the already-present check against its
        // own sibling
        var staged = Expand([Reg("modx_luna", "modx_luna_obj")]);

        Assert.That(staged[NpcIdRel].Text, Does.Contain("mmapi_ext:roster:id_to_obj:modx_luna"));
        Assert.That(staged[NpcIdRel].Text, Does.Contain("mmapi_ext:roster:obj_to_id:modx_luna"));
    }

    [Test]
    public void ShouldOrderMultipleRegistrantsByOrdinal()
    {
        // registrants arrive in whatever order the collector found them. New
        // symbols are assigned by sorted symbol, and lines render by ordinal
        var staged = Expand([
            Reg("modz_wren", "modz_wren_obj", "mod.z"),
            Reg("moda_luna", "moda_luna_obj", "mod.a"),
        ]);

        var members = staged[NpcIdRel].Text.Split('\n')
            .Where(l => l.Contains("mmapi_ext:roster:enum_member:"))
            .ToList();
        Assert.That(members, Is.EqualTo(new[]
        {
            "    moda_luna = 2, // mmapi_ext:roster:enum_member:moda_luna",
            "    modz_wren = 3, // mmapi_ext:roster:enum_member:modz_wren",
        }));
    }

    [Test]
    public void ShouldRenderTheSameBytesWhateverOrderRegistrantsArriveIn()
    {
        var forwards = Expand([Reg("moda_luna", "a_obj", "mod.a"), Reg("modz_wren", "z_obj", "mod.z")]);
        var backwards = Expand([Reg("modz_wren", "z_obj", "mod.z"), Reg("moda_luna", "a_obj", "mod.a")]);

        Assert.That(backwards[NpcIdRel].Text, Is.EqualTo(forwards[NpcIdRel].Text));
    }

    [Test]
    public void ShouldRenderAVacancyForALedgerEntryWhoseModIsGone()
    {
        // the tombstone. The symbol keeps its ordinal and its enum member, so a
        // save naming it still resolves, but it maps to the vacant stub object
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));

        var staged = Expand([], ledger);

        Assert.That(staged[NpcIdRel].Text, Does.Contain(
            "    modx_luna = 2, // mmapi_ext:roster:enum_member:modx_luna:vacant"));
        Assert.That(staged[NpcIdRel].Text, Does.Contain(
            "        case NpcId.modx_luna: return obj_mmapi_npc_vacant; "
            + "// mmapi_ext:roster:id_to_obj:modx_luna:vacant"));
    }

    [Test]
    public void ShouldRenderNoLineForAnEmptyVacancyTemplate()
    {
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));

        var staged = Expand([], ledger);

        // obj_to_id and object_macro both declare an empty vacancy
        Assert.That(staged[NpcIdRel].Text, Does.Not.Contain("mmapi_ext:roster:obj_to_id"));
        Assert.That(staged.ContainsKey(ManifestRel), Is.False);
    }

    [Test]
    public void ShouldReclaimALedgerOrdinalWhenTheModComesBack()
    {
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));

        var staged = Expand([Reg("modx_luna", "modx_luna_obj")], ledger);

        Assert.That(staged[NpcIdRel].Text, Does.Contain(
            "    modx_luna = 2, // mmapi_ext:roster:enum_member:modx_luna\n"));
        Assert.That(staged[NpcIdRel].Text, Does.Not.Contain(":vacant"));
    }

    [Test]
    public void ShouldRecordTheAppliedIdOnEveryFileItTouches()
    {
        var staged = Expand([Reg("modx_luna", "modx_luna_obj")]);

        Assert.That(staged[NpcIdRel].EntryIds, Does.Contain("ext:roster"));
        Assert.That(staged[ManifestRel].EntryIds, Does.Contain("ext:roster"));
    }

    [Test]
    public void ShouldExpandOverSeamedTextRatherThanPristine()
    {
        // seams apply against pristine first. Extension sites anchor against
        // the staged result
        var withSeam = Point + "\n" + """
            [[engine_fix]]
            name    = "note"
            file    = "gml/NpcId.gml"
            anchor  = "function gm_obj_id_to_npc_id(obj) {"
            replace = '''
            // t_note
            function gm_obj_id_to_npc_id(obj) {'''
            marker  = "t_note"
            """ + "\n";

        var staged = Expand([Reg("modx_luna", "modx_luna_obj")], catalogText: withSeam);

        Assert.That(staged[NpcIdRel].Text, Does.Contain("// t_note"));
        Assert.That(staged[NpcIdRel].Text, Does.Contain(
            "        case modx_luna_obj: return NpcId.modx_luna; // mmapi_ext:roster:obj_to_id:modx_luna"));
        Assert.That(staged[NpcIdRel].EntryIds, Is.EqualTo(new[] { "note", "ext:roster" }));
    }

    [Test]
    public void ShouldAppendExactlyOneNewlineToAFileThatLacksATrailingOne()
    {
        var pristine = Pristine((NpcIdRel, NpcIdPristine),
            (ManifestRel, "#macro obj_adeline object(\"obj_adeline\")"));

        var staged = Expand([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(staged[ManifestRel].Text, Is.EqualTo(
            "#macro obj_adeline object(\"obj_adeline\")\n"
            + "\n"
            + "#macro modx_luna_obj object(\"modx_luna_obj\") "
            + "// mmapi_ext:roster:object_macro:modx_luna\n"));
    }

    [Test]
    public void ShouldCollapseATailOfBlankLinesBeforeAppending()
    {
        var pristine = Pristine((NpcIdRel, NpcIdPristine), (ManifestRel, ManifestPristine + "\n\n\n"));

        var staged = Expand([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(staged[ManifestRel].Text, Is.EqualTo(
            ManifestPristine
            + "\n"
            + "#macro modx_luna_obj object(\"modx_luna_obj\") "
            + "// mmapi_ext:roster:object_macro:modx_luna\n"));
    }

    [Test]
    public void ShouldUseTheSiteCommentLeaderForAppendMarkers()
    {
        // An append site may target a TOML file, where a // marker is a
        // syntax error, so the marker leader follows the site's comment field
        // single-token replace, because the literal may carry \r\n, so no multi-line match
        var catalogText = Point.Replace("\"append\"", "\"append\"\ncomment  = \"#\"");

        var staged = Expand([Reg("modx_luna", "modx_luna_obj")], catalogText: catalogText);

        Assert.That(staged[ManifestRel].Text,
            Does.Contain("# mmapi_ext:roster:object_macro:modx_luna"));
        Assert.That(staged[ManifestRel].Text,
            Does.Not.Contain("// mmapi_ext:roster:object_macro"));
    }

    [Test]
    public void ShouldRefuseToAppendIntoAnUnterminatedBlockComment()
    {
        // the silent-corruption case. The appended macro would be swallowed by
        // the comment, and the mod would install clean and do nothing
        var pristine = Pristine((NpcIdRel, NpcIdPristine),
            (ManifestRel, ManifestPristine + "/* work in progress\n"));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("ends inside an unterminated block comment"));
    }

    [Test]
    public void ShouldFailClosedOnAMissedAnchor()
    {
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("        default: return undefined;", "        default: return -1;")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("anchor matched 0x"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Anchor));
        Assert.That(exception.Problems.Single().EntryId, Is.EqualTo("ext:roster:obj_to_id"));
    }

    [Test]
    public void ShouldFailClosedOnAnAmbiguousAnchor()
    {
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine + "\nfunction another(obj) {\n    switch (obj) {\n"
                                     + "        default: return undefined;\n    }\n}\n"),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("anchor matched 2x"));
    }

    [Test]
    public void ShouldFailClosedWhenTheAnchorSharesALineWithOtherCode()
    {
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace(
                "        default: return undefined;",
                "        case obj_x: return NpcId.Adeline;         default: return undefined;")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("shares a line with other code"));
    }

    [Test]
    public void ShouldFailClosedOnAMarkerAlreadyInTheFile()
    {
        var pristine = Pristine(
            (NpcIdRel, "// mmapi_ext:roster:enum_member:modx_luna\n" + NpcIdPristine),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("already appears in"));
        Assert.That(exception.Problems[0].Kind, Is.EqualTo(SeamProblemKind.Marker));
    }

    [Test]
    public void ShouldFailClosedOnAnOrdinalGap()
    {
        // A hole kills the game at launch, during reflection, before any
        // data is consulted ("34 did not match any NpcId"). This is the
        // check that converts that into something actionable.
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 5, "mod.x")));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], ledger);

        Assert.That(exception.Message, Does.Contain("ordinal 2 is unassigned but 'modx_luna' holds 5"));
        Assert.That(exception.Message, Does.Contain("would have a hole"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Extension));
    }

    [Test]
    public void ShouldFailClosedWhenTheGameGrewTheEnumIntoAnAssignedOrdinal()
    {
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 1, "mod.x")));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], ledger);

        Assert.That(exception.Message, Does.Contain("collides with assigned ordinal(s) 1 (modx_luna)"));
        Assert.That(exception.Message, Does.Contain("the install repairs this automatically"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Extension));
    }

    [Test]
    public void ShouldFailClosedWhenTheOrdinalEnumIsGone()
    {
        // the identity-domain boundary, enforced mechanically. A native enum
        // has no GML declaration to scan, so the point cannot stage
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("enum NpcId {", "enum SomethingElse {")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("enum 'NpcId' declared 0x"));
        // a structural locator that stopped matching is the same failure class
        // as a seam target miss, meaning the game changed shape
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Target));
    }

    [Test]
    public void ShouldFailClosedWhenTheSentinelIsNotLast()
    {
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("    Balor,\n    LEN\n", "    LEN,\n    Balor\n")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("ends with 'Balor', not the declared sentinel 'LEN'"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Target));
    }

    [Test]
    public void ShouldFailClosedWhenABaseMemberCarriesAnExplicitValue()
    {
        // the ordinal maths assumes ordinal = index for base members. If the
        // game ever breaks that, say so rather than compute on it
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("    Balor,", "    Balor = 7,")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("explicit value"));
        Assert.That(exception.Message, Does.Contain("'Balor = 7'"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Target));
    }

    [Test]
    public void ShouldFailClosedOnAMissingSiteFile()
    {
        var exception = ExpandFails([Reg("modx_luna", "modx_luna_obj")],
            pristine: Pristine((NpcIdRel, NpcIdPristine)));

        Assert.That(exception.Message, Does.Contain("site file not found in pristine source"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.MissingFile));
    }

    [Test]
    public void ShouldFailClosedWhenTwoModsComposeTheSameSymbol()
    {
        // author "a" + name "b_c" and author "a_b" + name "c" compose the
        // same prefix ancestry. The collector cannot see across mods, so this
        // must be a staging problem naming both mods, never an uncaught crash
        var exception = ExpandFails([
            Reg("a_b_c_luna", "obj_one", "a.b_c"),
            Reg("a_b_c_luna", "obj_two", "a_b.c"),
        ]);

        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Extension));
        Assert.That(exception.Message, Does.Contain("'a.b_c'"));
        Assert.That(exception.Message, Does.Contain("'a_b.c'"));
        Assert.That(exception.Message, Does.Contain("a_b_c_luna"));
    }

    [Test]
    public void ShouldRejectALiveSymbolMatchingABaseMembersNativeName()
    {
        // saves key by the native name form, so a symbol spelled like a base
        // member's native name would share that member's save identity, and
        // the harvest would classify the key as vanilla forever
        var exception = ExpandFails([Reg("adeline", "obj_x")]);

        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Extension));
        Assert.That(exception.Message, Does.Contain("native name"));
        Assert.That(exception.Message, Does.Contain("'Adeline'"));
    }

    [Test]
    public void ShouldRejectALedgeredSymbolMatchingABaseMembersNativeName()
    {
        // the same guard holds for a hand-edited or hostile ledger
        var ledger = new MemoryExtensionLedger();
        ledger.Assign("roster", new ExtensionAssignment("balor", 2, "mod.gone"));

        var exception = ExpandFails([], ledger);

        Assert.That(exception.Message, Does.Contain("native name"));
        Assert.That(exception.Message, Does.Contain("'Balor'"));
    }

    // ---- the non-degradation gate ---------------------------------------

    private static string Hash(StagedFile file) =>
        Convert.ToHexString(SHA256.HashData(file.Encode()));

    private static Dictionary<string, string> Hashes(IReadOnlyDictionary<string, StagedFile> staged) =>
        staged.ToDictionary(f => f.Key, f => Hash(f.Value));

    [Test]
    public void ShouldStageIdenticallyWithADeclaredPointAndNoRegistrants()
    {
        // The gate (byte-identity form 2). A point that exists, with zero
        // registrants and zero ledger vacancies, changes nothing. This
        // exercises load -> scan -> assign -> render-nothing -> splice-nothing.
        var seamed = Point + "\n" + """
            [[engine_fix]]
            name    = "note"
            file    = "gml/NpcId.gml"
            anchor  = "function gm_obj_id_to_npc_id(obj) {"
            replace = '''
            // t_note
            function gm_obj_id_to_npc_id(obj) {'''
            marker  = "t_note"
            """ + "\n";

        var catalog = Load(seamed);
        var pristine = DefaultPristine();

        var seamOnly = SeamStager.Simulate(catalog, pristine);
        var expanded = SeamStager.Simulate(catalog, pristine);
        var added = ExtensionExpander.Expand(
            catalog, [], new MemoryExtensionLedger(), expanded, pristine).Added;

        Assert.That(catalog.Extensions, Has.Count.EqualTo(1), "the point must actually be declared");
        Assert.That(Hashes(expanded), Is.EqualTo(Hashes(seamOnly)));
        // and no file was added to the stage merely by being looked at
        Assert.That(expanded.Keys.Order(StringComparer.Ordinal),
            Is.EqualTo(seamOnly.Keys.Order(StringComparer.Ordinal)));
        // both surfaces, because a mechanism that left staged identical while emitting
        // a stray vacancy stub or registry would pass a staged-only check
        Assert.That(added, Is.Empty);
    }

    [Test]
    public void ShouldStageTheShippedCatalogIdenticallyThroughTheExpander()
    {
        // Byte-identity form 1. Weak while the catalog shipped no points.
        // Now that npc_roster ships, this exercises the real entry's full
        // zero-registrant path (load, enum scan, anchor resolution) against
        // its own synthesised anchors, and still demands byte-identity.
        var (name, bytes) = PayloadResolver.SeamCatalog();
        var catalog = SeamCatalogLoader.Load(bytes, name);
        var pristine = new MemoryPristineSource(PristineSynthesis.FromCatalog(catalog)
            .ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)));

        var seamOnly = SeamStager.Simulate(catalog, pristine);
        var expanded = SeamStager.Simulate(catalog, pristine);
        var added = ExtensionExpander.Expand(
            catalog, [], new MemoryExtensionLedger(), expanded, pristine).Added;

        Assert.That(Hashes(expanded), Is.EqualTo(Hashes(seamOnly)));
        Assert.That(added, Is.Empty);
    }

    [Test]
    public void ShouldStillFailClosedOnRotWithZeroRegistrants()
    {
        // the other half of the gate. The stage above is identical because
        // there was nothing to render, not because the expander skipped the
        // point. A rotted anchor fails the install even when no mod asked for
        // it, the same stance seams take.
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("        default: return undefined;", "        default: return -1;")),
            (ManifestRel, ManifestPristine));

        var exception = ExpandFails([], pristine: pristine);

        Assert.That(exception.Message, Does.Contain("anchor matched 0x"));
    }

    [Test]
    public void ShouldValidateZeroRegistrantWithoutMutatingTheStage()
    {
        var catalog = Load(Point);
        var pristine = DefaultPristine();
        var staged = SeamStager.Simulate(catalog, pristine);
        var before = Hashes(staged);

        var problems = ExtensionExpander.Validate(catalog, staged, pristine, out var anchored);

        Assert.That(problems, Is.Empty);
        Assert.That(anchored["roster"], Is.EqualTo(4));
        Assert.That(Hashes(staged), Is.EqualTo(before));
    }

    [Test]
    public void ShouldReportAnchorRotFromValidateWithoutRegistrants()
    {
        // what keeps extension anchors from rotting silently between builds. A
        // game update that rewrites the file fails the check with no mod
        // installed at all
        var catalog = Load(Point);
        var pristine = Pristine(
            (NpcIdRel, NpcIdPristine.Replace("        default: return undefined;", "        default: return -1;")),
            (ManifestRel, ManifestPristine));
        var staged = SeamStager.Simulate(catalog, pristine);

        var problems = ExtensionExpander.Validate(catalog, staged, pristine, out _);

        Assert.That(problems.Single().Kind, Is.EqualTo(SeamProblemKind.Anchor));
        Assert.That(problems.Single().EntryId, Is.EqualTo("ext:roster:obj_to_id"));
    }
}
