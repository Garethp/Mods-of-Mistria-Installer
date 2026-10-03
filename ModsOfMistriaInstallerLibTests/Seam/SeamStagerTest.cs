using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class SeamStagerTest
{
    private const string Base = """
        version = 2

        [[hook]]
        name = "test.event"
        kind = "event"
        doc  = "A test event."
        """ + "\n";

    private const string FilterHook = "\n" + """
        [[hook]]
        name = "test.filter"
        kind = "filter"
        doc  = "A test filter."
        """ + "\n";

    private const string GoodSeam = "\n" + """
        [[seam]]
        id = "good"
        file = "gml/A.gml"
        anchor = '''
        function a() {
        }'''
        replace = '''
        function a() {
            try { mmapi_emit("test.event", undefined); } catch (__t_good) {} // t_good
        }'''
        marker = "t_good"
        provides = ["test.event"]
        """ + "\n";

    private const string TargetSeam = "\n" + """
        [[seam]]
        id      = "ttarget"
        file    = "gml/D.gml"
        target  = { fn = "damage", at = "after", anchor = "hp -= amount;" }
        op      = "emit"
        hook    = "test.event"
        ctx     = "self"
        """ + "\n";

    private const string TargetPristine =
        "function damage(amount) {\n"
        + "        hp    -=   amount;   // drift: extra spaces and a comment\n"
        + "    flash();\n"
        + "}\n";

    private const string GoodPristine = "function a() {\n}\n";

    private const string WrapSeam = "\n" + """
        [[seam]]
        id      = "twrap"
        file    = "gml/E.gml"
        target  = { fn = "describe" }
        op      = "wrap"
        hook    = "test.filter"
        ctx     = "{ thing: self }"
        blank_before = true
        """ + "\n";

    private const string WrapPristine =
        "function Thing() constructor {\n"
        + "    static describe = function(kind, label=\"x\") {\n"
        + "        return kind + label;\n"
        + "    }\n"
        + "}\n";

    private static SeamCatalog Load(string text) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text), "seams.toml");

    private static MemoryPristineSource Pristine(IReadOnlyDictionary<string, string> files) =>
        new(files.ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)));

    private static string StageOne(string catalogText, string fileText, string rel)
    {
        var catalog = Load(catalogText);
        var pristine = Pristine(new Dictionary<string, string>
        {
            [rel] = fileText,
            ["assets/gml/A.gml"] = GoodPristine,
        });

        return SeamStager.Simulate(catalog, pristine)[rel].Text;
    }

    [Test]
    public void ShouldSurviveWhitespaceDriftInATargetSeam()
    {
        var staged = StageOne(Base + GoodSeam + TargetSeam, TargetPristine, "assets/gml/D.gml");

        var lines = staged.Split('\n');
        Assert.That(lines[1], Does.Contain("hp    -=   amount;"));
        Assert.That(lines[2], Does.Contain("mmapi_emit(\"test.event\", self)"));
        Assert.That(lines[3], Does.Contain("flash();"));
    }

    [Test]
    public void ShouldFailClosedOnAMissingTargetFunction()
    {
        var catalog = Load(Base + GoodSeam + TargetSeam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/D.gml"] = "function other() {\n}\n",
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("'damage' defined 0x"));
    }

    [Test]
    public void ShouldFailClosedOnAMissingTargetAnchor()
    {
        var catalog = Load(Base + GoodSeam + TargetSeam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/D.gml"] = "function damage(amount) {\n    flash();\n}\n",
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("target anchor matched 0x"));
    }

    [Test]
    public void ShouldFailClosedWhenTheTargetAnchorSharesALine()
    {
        // a reformat squeezed the anchor into a one-line block. A line-wise
        // insertion would land the payload outside it, so this must fail
        var catalog = Load(Base + GoodSeam + TargetSeam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/D.gml"] =
                "function damage(amount) {\n    if (armored) { hp -= amount; }\n    flash();\n}\n",
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("shares a line"));
    }

    [Test]
    public void ShouldRenameAndAppendTheWrapper()
    {
        var staged = StageOne(Base + FilterHook + GoodSeam + WrapSeam, WrapPristine, "assets/gml/E.gml");

        Assert.That(staged, Does.Contain(
            "static __mmapi_orig_describe = function(kind, label=\"x\") {\n"
            + "        return kind + label;\n    }"));
        Assert.That(staged, Does.Contain("static describe = function(kind, label=\"x\") {"));
        Assert.That(staged, Does.Contain("self.__mmapi_orig_describe(kind, label)"));
        Assert.That(staged, Does.Contain(
            "mmapi_apply_filters(\"test.filter\", __mmapi_wrap_result, { thing: self })"));
    }

    private const string FilterSeam = "\n" + """
        [[seam]]
        id      = "tfilter"
        file    = "gml/F.gml"
        target  = { fn = "regen", at = "head" }
        op      = "filter"
        hook    = "test.filter"
        var     = "amount"
        ctx     = "{ actor: self, cap: max_amount }"
        """ + "\n";

    private const string FilterPristine =
        "function regen(amount, max_amount) {\n"
        + "    hp = min(hp + amount, max_amount);\n"
        + "}\n";

    [Test]
    public void ShouldStageAFilterWhoseReadsAllResolve()
    {
        var staged = StageOne(Base + FilterHook + GoodSeam + FilterSeam, FilterPristine,
            "assets/gml/F.gml");

        Assert.That(staged, Does.Contain(
            "amount = mmapi_apply_filters(\"test.filter\", amount, { actor: self, cap: max_amount })"));
    }

    [Test]
    public void ShouldFailClosedWhenAPayloadReadIsGone()
    {
        // an engine update renamed the parameters, so the payload's reads no
        // longer resolve anywhere in the function
        var catalog = Load(Base + FilterHook + GoodSeam + FilterSeam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/F.gml"] = "function regen(value, value_cap) {\n"
                                   + "    hp = min(hp + value, value_cap);\n"
                                   + "}\n",
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("the payload reads 'amount', 'max_amount'"));
        Assert.That(exception.Message, Does.Contain("never mentions them"));
        Assert.That(exception.Problems[0].Kind, Is.EqualTo(SeamProblemKind.Reads));
    }

    [Test]
    public void ShouldExemptNonScopeNamesInAPayload()
    {
        // an uppercase enum root, a call name, struct keys and keyword
        // literals are not scope reads, so a body mentioning none of them
        // still stages
        var seam = FilterSeam.Replace(
            "\"{ actor: self, cap: max_amount }\"",
            "\"{ kind: ItemKind.Weapon, count: total_count(), flag: true }\"");
        var staged = StageOne(Base + FilterHook + GoodSeam + seam, FilterPristine,
            "assets/gml/F.gml");

        Assert.That(staged, Does.Contain("ItemKind.Weapon"));
    }

    private const string ContextSeam = "\n" + """
        [[seam]]
        id      = "tctx"
        file    = "gml/G.gml"
        context_before = '''
            play_sound(track);
        '''
        op      = "emit"
        hook    = "test.event"
        ctx     = "{ track: track, volume: base_volume }"
        """ + "\n";

    private const string ContextPristine =
        "function play_music(track) {\n"
        + "    var base_volume = 0.8;\n"
        + "    play_sound(track);\n"
        + "}\n";

    [Test]
    public void ShouldStageATextSeamWhoseReadsResolveInTheEnclosingFunction()
    {
        // base_volume is declared above the anchored line, outside the
        // anchor's own text, so only the enclosing span can prove it
        var staged = StageOne(Base + GoodSeam + ContextSeam, ContextPristine, "assets/gml/G.gml");

        Assert.That(staged, Does.Contain("{ track: track, volume: base_volume }"));
    }

    [Test]
    public void ShouldFailClosedWhenATextSeamReadIsGoneFromTheEnclosingFunction()
    {
        var catalog = Load(Base + GoodSeam + ContextSeam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/G.gml"] = "function play_music(track) {\n"
                                   + "    var volume_scale = 0.8;\n"
                                   + "    play_sound(track);\n"
                                   + "}\n",
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("the payload reads 'base_volume'"));
        Assert.That(exception.Message, Does.Contain("'play_music' in"));
        Assert.That(exception.Message, Does.Contain("never mentions it"));
    }

    [Test]
    public void ShouldFallBackToTheWholeFileForATopLevelAnchor()
    {
        // the anchor sits outside any function, so the file is the region.
        // The asset-prefixed name never needs proving at all
        var seam = ContextSeam
            .Replace("    play_sound(track);", "init_thing();")
            .Replace("\"{ track: track, volume: base_volume }\"",
                "\"{ mode: boot_mode, icon: spr_boot_icon }\"");
        var staged = StageOne(Base + GoodSeam + seam, "boot_mode = 0;\ninit_thing();\n",
            "assets/gml/G.gml");

        Assert.That(staged, Does.Contain("{ mode: boot_mode, icon: spr_boot_icon }"));
    }

    [Test]
    public void ShouldHoldAssetReadsAgainstTheInventoryWhenOneExists()
    {
        // the fixture seam's ctx references a sprite. A source with an asset
        // inventory proves or refuses it, and a source without one stands down
        var seam = ContextSeam.Replace(
            "\"{ track: track, volume: base_volume }\"",
            "\"{ track: track, icon: spr_track_icon }\"");
        var catalog = Load(Base + GoodSeam + seam);
        var files = new Dictionary<string, string>
        {
            ["assets/gml/G.gml"] = ContextPristine,
            ["assets/gml/A.gml"] = GoodPristine,
        };

        var carrying = new MemoryPristineSource(
            files.ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)),
            new HashSet<string> { "spr_track_icon" });
        Assert.That(SeamStager.Simulate(catalog, carrying)["assets/gml/G.gml"].Text,
            Does.Contain("spr_track_icon"));

        var missing = new MemoryPristineSource(
            files.ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)),
            new HashSet<string>());
        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, missing));
        Assert.That(exception!.Message, Does.Contain("references asset 'spr_track_icon'"));
        Assert.That(exception.Problems[0].Kind, Is.EqualTo(SeamProblemKind.Asset));

        var unknowing = new MemoryPristineSource(
            files.ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)));
        Assert.That(SeamStager.Simulate(catalog, unknowing)["assets/gml/G.gml"].Text,
            Does.Contain("spr_track_icon"));
    }

    [Test]
    public void ShouldHoldWrapPayloadReadsAgainstTheWrappedFunction()
    {
        var seam = WrapSeam.Replace("{ thing: self }", "{ thing: self, size: label_size }");
        var catalog = Load(Base + FilterHook + GoodSeam + seam);
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/E.gml"] = WrapPristine,
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("the payload reads 'label_size'"));
        Assert.That(exception.Message, Does.Contain("never mentions it"));
    }

    [Test]
    public void ShouldRefuseASelfReferencingWrapBody()
    {
        var catalog = Load(Base + FilterHook + GoodSeam + WrapSeam);
        var recursive = WrapPristine.Replace("return kind + label;", "return describe(kind, label);");
        var pristine = Pristine(new Dictionary<string, string>
        {
            ["assets/gml/E.gml"] = recursive,
            ["assets/gml/A.gml"] = GoodPristine,
        });

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.Simulate(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("self-referencing"));
    }

    // --- call-rewrite staging ---------------------------------------------------

    private const string PristineNode =
        "function set_key(key) {\n"
        + "    // a comment naming local_get(key) is not a call\n"
        + "    var decoy = \"a string naming local_get(key) is not a call\";\n"
        + "    var info = local_get_info(key);\n"
        + "    self.set_text(local_get(key));\n"
        + "    var wide = local_get   (key);\n"
        + "    var gap = local_get /* mid */ (key);\n"
        + "    return local_get(format(\"misc_local/{Season}\", season)) + local_get(key);\n"
        + "}\n";

    private const int NodeDirectSites = 5;

    private const string PristineGame = "function step_begin() {\n}\n";
    private const string PristinePlain = "function helper() {\n    return 1;\n}\n";

    private const string WrapperGml =
        "function mmapi_local_get(key) {\n"
        + "    return local_get(key);\n"
        + "}\n";

    private const string RewriteBase = """
        version = 2

        [[hook]]
        name = "local.get"
        kind = "filter"
        doc  = "A test filter over resolved text."

        [[hook]]
        name = "local.missing"
        kind = "filter"
        doc  = "A test filter over misses."
        """ + "\n";

    private const string Rewrite = "\n" + """
        [[call_rewrite]]
        id       = "local_get_dispatch"
        callee   = "local_get"
        to       = "mmapi_local_get"
        args     = 1
        provides = ["local.get", "local.missing"]
        """ + "\n";

    private const string GameSeam = "\n" + """
        [[hook]]
        name = "test.event"
        kind = "event"
        doc  = "A test event."

        [[seam]]
        id = "game_step"
        file = "gml/objects/Game.gml"
        anchor = '''
        function step_begin() {
        }'''
        replace = '''
        function step_begin() {
            try { mmapi_emit("test.event", undefined); } catch (__t_gs) {} // t_game_step
            var greet = local_get("mod/greeting");
        }'''
        marker = "t_game_step"
        provides = ["test.event"]
        """ + "\n";

    private const string RewriteCatalog = RewriteBase + GameSeam + Rewrite;
    private const string RewriteOnlyCatalog = RewriteBase + Rewrite;

    private static readonly Dictionary<string, string> RewriteFiles = new()
    {
        ["assets/gml/objects/Game.gml"] = PristineGame,
        ["assets/gml/scripts/UI/Node.gml"] = PristineNode,
        ["assets/gml/scripts/Plain.gml"] = PristinePlain,
    };

    private static Dictionary<string, StagedFile> Stage(string catalogText = RewriteCatalog,
        Dictionary<string, string>? files = null)
    {
        var catalog = Load(catalogText);
        var texts = files ?? RewriteFiles;
        var pristine = Pristine(texts);
        var listing = texts.Keys
            .Order(StringComparer.Ordinal)
            .ToList();

        var staged = SeamStager.Simulate(catalog, pristine);
        SeamStager.StageCallRewrites(catalog, staged, pristine, listing);
        return staged;
    }

    [Test]
    public void ShouldRewriteDirectCalls()
    {
        var text = Stage()["assets/gml/scripts/UI/Node.gml"].Text;

        Assert.That(GmlScanner.FindCalls(text, "local_get"), Is.Empty);
        var sites = GmlScanner.FindCalls(text, "mmapi_local_get");
        Assert.That(sites, Has.Count.EqualTo(NodeDirectSites));
        Assert.That(sites, Has.All.Matches<CallSite>(s => s.Args == 1 && s.Kind == CallKind.Call));

        Assert.That(text, Does.Contain("mmapi_local_get   (key)"));
        Assert.That(text, Does.Contain("mmapi_local_get /* mid */ (key)"));
        Assert.That(text, Does.Contain(
            "return mmapi_local_get(format(\"misc_local/{Season}\", season)) + mmapi_local_get(key);"));
    }

    [Test]
    public void ShouldKeepCommentsStringsAndLongerIdentifiers()
    {
        var text = Stage()["assets/gml/scripts/UI/Node.gml"].Text;

        Assert.That(text, Does.Contain("// a comment naming local_get(key) is not a call"));
        Assert.That(text, Does.Contain("\"a string naming local_get(key) is not a call\""));
        Assert.That(text, Does.Contain("local_get_info(key)"));
    }

    [Test]
    public void ShouldRewriteSeamInjectedTextAfterSeams()
    {
        var game = Stage()["assets/gml/objects/Game.gml"];

        Assert.That(game.Text, Does.Contain("t_game_step"));
        Assert.That(game.Text, Does.Contain("var greet = mmapi_local_get(\"mod/greeting\");"));
        Assert.That(game.EntryIds, Is.EqualTo(new[] { "game_step", "local_get_dispatch" }));
    }

    [Test]
    public void ShouldNotStageFilesWithoutCallSites()
    {
        Assert.That(Stage(), Does.Not.ContainKey("assets/gml/scripts/Plain.gml"));
    }

    [Test]
    public void ShouldNotStageAMentionWithoutACall()
    {
        // the substring IS present, so the file is tokenized, and the
        // tokenizer has the final say - a comment, a string and a longer
        // identifier are not calls, so nothing is rewritten
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/Mentions.gml"] =
                "function mentions(key) {\n"
                + "    // local_get(key) named in a comment\n"
                + "    var s = \"local_get(key)\";\n"
                + "    return local_get_info(key);\n"
                + "}\n",
        };

        Assert.That(Stage(files: files), Does.Not.ContainKey("assets/gml/scripts/Mentions.gml"));
    }

    [Test]
    public void ShouldKeepTheNativeCallInMmapiFiles()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/mmapi/mmapi_local.gml"] = WrapperGml,
        };

        var staged = Stage(files: files);

        Assert.That(staged, Does.Not.ContainKey("assets/gml/scripts/mmapi/mmapi_local.gml"));
    }

    [Test]
    public void ShouldFailClosedOnAnArityMismatch()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/UI/Node.gml"] = "function f() {\n    return local_get(a, b);\n}\n",
        };

        var exception = Assert.Throws<SeamStagingException>(() => Stage(files: files));

        Assert.That(exception!.Message, Does.Contain("Node.gml:2 passes 2 argument(s)"));
        Assert.That(exception.Message, Does.Contain("expected 1"));
    }

    [Test]
    public void ShouldReportMemberAccessAsAResidual()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/UI/Node.gml"] = "function f() {\n    return thing.local_get(key);\n}\n",
        };

        var exception = Assert.Throws<SeamStagingException>(() => Stage(files: files));

        Assert.That(exception!.Message, Does.Contain("member access"));
    }

    [Test]
    public void ShouldReportADefinitionAsAResidual()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/UI/Node.gml"] = "function local_get(key) {\n    return key;\n}\n",
        };

        var exception = Assert.Throws<SeamStagingException>(() => Stage(files: files));

        Assert.That(exception!.Message, Does.Contain("defines 'local_get'"));
    }

    [Test]
    public void ShouldFailClosedOnZeroRewrittenSites()
    {
        var files = new Dictionary<string, string>
        {
            ["assets/gml/scripts/Plain.gml"] = PristinePlain,
        };

        var exception = Assert.Throws<SeamStagingException>(
            () => Stage(RewriteOnlyCatalog, files));

        Assert.That(exception!.Message, Does.Contain("no direct call to 'local_get'"));
    }

    [Test]
    public void ShouldBatchRewriteErrors()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/UI/Node.gml"] = "function f() {\n    return local_get(a, b);\n}\n",
            ["assets/gml/scripts/UI/Peer.gml"] = "function g() {\n    return thing.local_get(key);\n}\n",
        };

        var exception = Assert.Throws<SeamStagingException>(() => Stage(files: files));

        Assert.That(exception!.Message, Does.Contain("Node.gml:2"));
        Assert.That(exception.Message, Does.Contain("Peer.gml:2"));
        Assert.That(exception.Message, Does.Contain("problem(s)"));
    }

    [Test]
    public void ShouldBatchSeamAndRewriteProblemsInOneStage()
    {
        // a broken anchor must not hide rewrite drift in the same build
        var catalog = Load(RewriteCatalog);
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/objects/Game.gml"] = "function step_begin() {\n    injected();\n}\n",
            ["assets/gml/scripts/UI/Node.gml"] = "function f() {\n    return local_get(a, b);\n}\n",
        };
        var pristine = Pristine(files);

        var exception = Assert.Throws<SeamStagingException>(() => SeamStager.StageAll(catalog, pristine));

        Assert.That(exception!.Message, Does.Contain("game_step"));
        Assert.That(exception.Message, Does.Contain("passes 2 argument(s)"));
    }

    [Test]
    public void ShouldKeepCrlfLineEndings()
    {
        var files = new Dictionary<string, string>(RewriteFiles)
        {
            ["assets/gml/scripts/UI/Node.gml"] = PristineNode.Replace("\n", "\r\n"),
        };

        var node = Stage(files: files)["assets/gml/scripts/UI/Node.gml"];

        Assert.That(node.Eol, Is.EqualTo("\r\n"));
        var encoded = Encoding.UTF8.GetString(node.Encode());
        Assert.That(encoded, Does.Contain("mmapi_local_get"));
        Assert.That(encoded.Replace("\r\n", ""), Does.Not.Contain('\n'));
    }
}
