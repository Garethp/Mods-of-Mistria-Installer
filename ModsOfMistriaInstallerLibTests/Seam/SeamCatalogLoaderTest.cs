using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class SeamCatalogLoaderTest
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

    private const string SeamB = "\n" + """
        [[seam]]
        id = "second"
        file = "gml/B.gml"
        anchor = '''
        function b() {
        }'''
        replace = '''
        function b() {
            REPLACE_BODY // t_second
        }'''
        marker = "t_second"
        provides = [PROVIDES]
        """ + "\n";

    private const string OverrideHook = "\n" + """
        [[hook]]
        name = "test.take"
        kind = "override"
        contention = "claim-scoped"
        doc  = "A test override."
        """ + "\n";

    private const string OverrideSeam = "\n" + """
        [[seam]]
        id = "taker"
        file = "gml/F.gml"
        anchor = '''
        function f() {
        }'''
        replace = '''
        function f() {
            var __r = mmapi_run_override("test.take", undefined); // t_taker
            if (__r != undefined) { return __r; }
        }'''
        marker = "t_taker"
        provides = ["test.take"]
        """ + "\n";

    private const string TemplateSeam = "\n" + """
        [[seam]]
        id      = "tguard"
        file    = "gml/C.gml"
        context_before = '''
        function c(item) {
        '''
        context_after = '''
            do_thing(item);'''
        op      = "guard"
        hook    = "test.guard"
        ctx     = "item"
        on_veto = "return false;"

        [[hook]]
        name = "test.guard"
        kind = "guard"
        doc  = "A test guard."
        """ + "\n";

    private const string Rewrite = "\n" + """
        [[call_rewrite]]
        id       = "local_get_dispatch"
        callee   = "local_get"
        to       = "mmapi_local_get"
        args     = 1
        provides = ["local.get", "local.missing"]
        """ + "\n";

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

    private static string SeamBWith(string body, string provides) =>
        SeamB.Replace("REPLACE_BODY", body).Replace("PROVIDES", provides);

    private static SeamCatalog Load(string text) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text), "seams.toml");

    private static string LoadError(string text)
    {
        var exception = Assert.Throws<SeamCatalogException>(() => Load(text));
        return exception!.Message;
    }

    [Test]
    public void ShouldRejectAV1Catalog()
    {
        var message = LoadError("version = 1\n");

        Assert.That(message, Does.Contain("unsupported version 1"));
    }

    [Test]
    public void ShouldEnforceTheHookNameCharset()
    {
        // hook names are rendered into mmapi_hook_catalog.gml as GML string
        // literals, so a quote or a newline in one emits a file that does not
        // parse - and the framework's whole hook table goes with it
        foreach (var tomlValue in new[]
                 {
                     "'test.ev\"il'",
                     "\"test.ev\\nil\"",
                     "'test ev'",
                     "'Test.Event'",
                     "'test..ev'",
                 })
        {
            var message = LoadError(Base.Replace("name = \"test.event\"", $"name = {tomlValue}"));

            Assert.That(message, Does.Contain("dotted lowercase hook name"), tomlValue);
        }
    }

    [Test]
    public void ShouldEnforceTheHookAliasCharset()
    {
        var text = (Base + GoodSeam).Replace("doc  = \"A test event.\"",
            "doc  = \"A test event.\"\naliases = ['ev\"il']");

        Assert.That(LoadError(text), Does.Contain("dotted lowercase"));
    }

    [Test]
    public void ShouldAllowAStatementMarkerButNeverALineBreak()
    {
        // a marker is an identity token searched verbatim, NOT an identifier:
        // the shipped catalog uses a whole statement as one. A line break is
        // the one thing it may not carry (it trails a payload as a comment)
        var ok = GoodSeam.Replace("marker = \"t_good\"", "marker = \"mmapi_thing();\"")
            .Replace("catch (__t_good) {} // t_good", "catch (__t_good) {} // mmapi_thing();");
        Assert.That(Load(Base + ok).Seams[0].Marker, Is.EqualTo("mmapi_thing();"));

        var spans = GoodSeam.Replace("marker = \"t_good\"", "marker = \"\"\"t_\\ngood\"\"\"");
        Assert.That(LoadError(Base + spans), Does.Contain("spans lines"));
    }

    [Test]
    public void ShouldRequireCatchVarToBeAnIdentifier()
    {
        var seam = "\n" + """
            [[seam]]
            id = "tmpl"
            file = "gml/C.gml"
            op = "emit"
            hook = "test.event"
            catch_var = "not an ident"
            context_before = '''
            function c() {
            '''
            context_after = '''}'''
            """ + "\n";

        Assert.That(LoadError(Base + GoodSeam + seam), Does.Contain("catch_var"));
    }

    [Test]
    public void ShouldRejectAnEscapingFilePath()
    {
        foreach (var (bad, want) in new[]
                 {
                     ("../../evil.gml", "'..'"),
                     ("gml/../../evil.gml", "'..'"),
                     ("C:/evil.gml", "colon"),
                     ("gml/A.gml:stream", "colon"),
                 })
        {
            var message = LoadError(Base + GoodSeam.Replace("file = \"gml/A.gml\"", $"file = \"{bad}\""));

            Assert.That(message, Does.Contain(want), bad);
        }
    }

    [Test]
    public void ShouldNormaliseALeadingSlashFilePathIntoTheStore()
    {
        var catalog = Load(Base + GoodSeam.Replace("file = \"gml/A.gml\"", "file = \"/gml/A.gml\""));

        Assert.That(catalog.Seams[0].File, Is.EqualTo("assets/gml/A.gml"));
    }

    [Test]
    public void ShouldBatchAnEscapingFilePathWithOtherProblems()
    {
        var bad = (Base + GoodSeam).Replace("file = \"gml/A.gml\"", "file = \"../evil.gml\"")
            .Replace("provides = [\"test.event\"]", "provides = [\"test.event\", \"test.ghost\"]");

        var message = LoadError(bad);

        Assert.That(message, Does.Contain("'..'"));
        Assert.That(message, Does.Contain("test.ghost"));
    }

    [Test]
    public void ShouldLoadAGoodCatalog()
    {
        var catalog = Load(Base + GoodSeam);

        Assert.That(catalog.Version, Is.EqualTo(2));
        Assert.That(catalog.Seams, Has.Count.EqualTo(1));
        Assert.That(catalog.Hook("test.event")!.Kind, Is.EqualTo(HookKind.Event));
    }

    [Test]
    public void ShouldRejectAnUnprovidedSeamHook()
    {
        var message = LoadError(Base + FilterHook + GoodSeam);

        Assert.That(message, Does.Contain("test.filter"));
        Assert.That(message, Does.Contain("no seam provides it"));
    }

    [Test]
    public void ShouldRejectUndeclaredProvides()
    {
        var bad = GoodSeam.Replace("provides = [\"test.event\"]",
            "provides = [\"test.event\", \"test.ghost\"]");

        var message = LoadError(Base + bad);

        Assert.That(message, Does.Contain("test.ghost"));
        Assert.That(message, Does.Contain("[[hook]] stanza"));
    }

    [Test]
    public void ShouldLintADispatchKindMismatch()
    {
        var seam = SeamBWith("try { mmapi_emit(\"test.filter\", undefined); } catch (__t_s) {}",
            "\"test.filter\"");

        var message = LoadError(Base + FilterHook + GoodSeam + seam);

        Assert.That(message, Does.Contain("declared kind `filter`"));
    }

    [Test]
    public void ShouldLintADispatchOutsideProvides()
    {
        var seam = SeamBWith("value = mmapi_apply_filters(\"test.filter\", value, undefined);\n"
                             + "    try { mmapi_emit(\"test.event\", undefined); } catch (__t_s) {}",
            "\"test.filter\"");

        var message = LoadError(Base + FilterHook + GoodSeam + seam);

        Assert.That(message, Does.Contain("second"));
        Assert.That(message, Does.Contain("does not list it in `provides`"));
    }

    [Test]
    public void ShouldLintADiscardedFilterResult()
    {
        var seam = SeamBWith("try { mmapi_apply_filters(\"test.filter\", 1, undefined); } catch (__t_s) {}",
            "\"test.filter\"");

        var message = LoadError(Base + FilterHook + GoodSeam + seam);
        Assert.That(message, Does.Contain("discards the mmapi_apply_filters result"));

        var ok = (Base + FilterHook + GoodSeam + seam).Replace("kind = \"filter\"",
            "kind = \"filter\"\nin_place = true");
        var catalog = Load(ok);
        Assert.That(catalog.Hook("test.filter")!.InPlace, Is.True);
    }

    [Test]
    public void ShouldRejectASeamProvidedRuntimeHook()
    {
        var bad = (Base + GoodSeam).Replace("kind = \"event\"",
            "kind = \"event\"\nprovider = \"runtime\"");

        Assert.That(LoadError(bad), Does.Contain("provider=runtime"));
    }

    [Test]
    public void ShouldAcceptARuntimeHookWithNoSeam()
    {
        var text = Base + GoodSeam + "\n" + """
            [[hook]]
            name = "test.derived"
            kind = "event"
            provider = "runtime"
            doc  = "Emitted by the framework itself."
            """ + "\n";

        var catalog = Load(text);

        Assert.That(catalog.Hooks, Does.Contain("test.derived"));
        Assert.That(HookCatalogRenderer.Render(catalog), Does.Contain("\"test.derived\", \"event\","));
    }

    [Test]
    public void ShouldEnforceMarkerDiscipline()
    {
        var missing = GoodSeam.Replace("marker = \"t_good\"", "marker = \"absent_token\"");
        Assert.That(LoadError(Base + missing), Does.Contain("does not appear in `replace`"));

        var inAnchor = GoodSeam.Replace("function a() {\n}'''\nreplace",
            "function a() { // t_good\n}'''\nreplace");
        Assert.That(LoadError(Base + inAnchor), Does.Contain("appears in the pristine `anchor`"));
    }

    [Test]
    public void ShouldBatchErrors()
    {
        var bad = GoodSeam.Replace("provides = [\"test.event\"]", "provides = [\"test.ghost\"]")
            .Replace("marker = \"t_good\"", "marker = \"absent_token\"");

        var message = LoadError(Base + bad);

        Assert.That(message, Does.Contain("test.ghost"));
        Assert.That(message, Does.Contain("absent_token"));
        Assert.That(message, Does.Contain("problem(s)"));
    }

    [Test]
    public void ShouldOrderApplicationByDependsOn()
    {
        var second = SeamBWith("try { mmapi_emit(\"test.event\", undefined); } catch (__t_s) {}",
            "\"test.event\"");
        var text = Base + GoodSeam.Replace("provides = [\"test.event\"]",
            "provides = [\"test.event\"]\ndepends_on = [\"second\"]") + second;

        var catalog = Load(text);

        var ids = catalog.Entries.Select(e => e.Id).ToList();
        Assert.That(ids.IndexOf("second"), Is.LessThan(ids.IndexOf("good")));
    }

    [Test]
    public void ShouldRejectUnknownDependsOnAndCycles()
    {
        var unknown = GoodSeam.Replace("provides = [\"test.event\"]",
            "provides = [\"test.event\"]\ndepends_on = [\"ghost\"]");
        Assert.That(LoadError(Base + unknown), Does.Contain("unknown entry"));

        var cycle = GoodSeam.Replace("provides = [\"test.event\"]",
            "provides = [\"test.event\"]\ndepends_on = [\"good\"]");
        Assert.That(LoadError(Base + cycle), Does.Contain("cycle"));
    }

    [Test]
    public void ShouldLoadOverrideContention()
    {
        var catalog = Load(Base + GoodSeam + OverrideHook + OverrideSeam);

        Assert.That(catalog.Hook("test.take")!.Contention, Is.EqualTo(HookContention.ClaimScoped));
        Assert.That(catalog.Hook("test.event")!.Contention, Is.Null);

        var rendered = HookCatalogRenderer.Render(catalog);
        Assert.That(rendered, Does.Contain("\"test.take\", \"claim-scoped\","));
        Assert.That(rendered, Does.Contain("global.__mmapi_hook_contention"));
        Assert.That(rendered, Does.Not.Contain("\"test.event\", \"claim-scoped\""));
    }

    [Test]
    public void ShouldRequireContentionOnAnOverrideHook()
    {
        var bad = OverrideHook.Replace("contention = \"claim-scoped\"\n", "");

        var message = LoadError(Base + GoodSeam + bad + OverrideSeam);

        Assert.That(message, Does.Contain("needs `contention`"));
    }

    [Test]
    public void ShouldRejectABadContentionValue()
    {
        var bad = OverrideHook.Replace("\"claim-scoped\"", "\"shared\"");

        var message = LoadError(Base + GoodSeam + bad + OverrideSeam);

        Assert.That(message, Does.Contain("contention 'shared' is not"));
    }

    [Test]
    public void ShouldRejectContentionOnANonOverrideHook()
    {
        var bad = Base.Replace("kind = \"event\"", "kind = \"event\"\ncontention = \"exclusive\"");

        var message = LoadError(bad + GoodSeam);

        Assert.That(message, Does.Contain("only override hooks carry it"));
    }

    [Test]
    public void ShouldRejectAliasCollisions()
    {
        var text = (Base + FilterHook + GoodSeam).Replace(
            "name = \"test.event\"\nkind = \"event\"",
            "name = \"test.event\"\nkind = \"event\"\naliases = [\"test.filter\"]");

        Assert.That(LoadError(text), Does.Contain("collides with a hook name"));
    }

    [Test]
    public void ShouldParseAliases()
    {
        var text = (Base + GoodSeam).Replace(
            "name = \"test.event\"\nkind = \"event\"",
            "name = \"test.event\"\nkind = \"event\"\naliases = [\"test.old_event\"]");

        var catalog = Load(text);

        Assert.That(catalog.Hook("test.event")!.Aliases, Is.EqualTo(new[] { "test.old_event" }));
        Assert.That(HookCatalogRenderer.Render(catalog), Does.Contain("\"test.old_event\", \"test.event\","));
    }

    [Test]
    public void ShouldGenerateTheDispatchForATemplateSeam()
    {
        var catalog = Load(Base + GoodSeam + TemplateSeam);

        var entry = catalog.Entries.First(e => e.Id == "tguard");
        Assert.That(entry.Op, Is.EqualTo(DispatchOp.Guard));
        Assert.That(entry.Hooks, Is.EqualTo(new[] { "test.guard" }));
        Assert.That(entry.Anchor, Is.EqualTo("function c(item) {\n    do_thing(item);"));
        Assert.That(entry.Replace, Does.Contain(
            "try { if (mmapi_check_guards(\"test.guard\", item) == false) "
            + "{ return false; } } catch (__mmapi_tguard) {} // mmapi_tguard"));
        Assert.That(entry.Replace, Does.StartWith("function c(item) {\n"));
        Assert.That(entry.Replace, Does.EndWith("\n    do_thing(item);"));
    }

    [Test]
    public void ShouldRejectProvidesOnATemplateSeam()
    {
        var bad = TemplateSeam.Replace("on_veto = \"return false;\"",
            "on_veto = \"return false;\"\nprovides = [\"test.guard\"]");

        Assert.That(LoadError(Base + GoodSeam + bad), Does.Contain("derives it from `hook`"));
    }

    [Test]
    public void ShouldLintATemplateSeamKindMismatch()
    {
        // declaring the hook as an event makes the generated guard dispatch illegal
        var bad = (Base + GoodSeam + TemplateSeam).Replace(
            "name = \"test.guard\"\nkind = \"guard\"", "name = \"test.guard\"\nkind = \"event\"");

        Assert.That(LoadError(bad), Does.Contain("declared kind `event`"));
    }

    [Test]
    public void ShouldRejectAnUnknownTemplateOp()
    {
        var bad = (Base + GoodSeam + TemplateSeam).Replace("op      = \"guard\"",
            "op      = \"detour\"");

        Assert.That(LoadError(bad), Does.Contain("op 'detour'"));
    }

    [Test]
    public void ShouldLoadAGoodRewriteCatalog()
    {
        var catalog = Load(RewriteBase + Rewrite);

        Assert.That(catalog.CallRewrites, Has.Count.EqualTo(1));
        var rewrite = catalog.CallRewrites[0];
        Assert.That(rewrite.Id, Is.EqualTo("local_get_dispatch"));
        Assert.That(rewrite.Callee, Is.EqualTo("local_get"));
        Assert.That(rewrite.To, Is.EqualTo("mmapi_local_get"));
        Assert.That(rewrite.Args, Is.EqualTo(1));
        Assert.That(rewrite.Hooks, Is.EqualTo(new[] { "local.get", "local.missing" }));

        Assert.That(catalog.Hooks, Does.Contain("local.get"));
        Assert.That(catalog.Hooks, Does.Contain("local.missing"));
    }

    [Test]
    public void ShouldBatchMissingRewriteFields()
    {
        var message = LoadError(RewriteBase + "\n[[call_rewrite]]\nid = \"x\"\n");

        Assert.That(message, Does.Contain("missing `callee`"));
        Assert.That(message, Does.Contain("missing `to`"));
        Assert.That(message, Does.Contain("`args` must be a non-negative integer"));
        Assert.That(message, Does.Contain("provides no hooks"));
    }

    [Test]
    public void ShouldRejectAMissingRewriteId()
    {
        var message = LoadError(RewriteBase + "\n[[call_rewrite]]\ncallee = \"a\"\n");

        Assert.That(message, Does.Contain("[[call_rewrite]] #1 is missing `id`"));
    }

    [Test]
    public void ShouldRequireAPlainIdentifierCallee()
    {
        var bad = Rewrite.Replace("callee   = \"local_get\"", "callee   = \"local.get\"");

        Assert.That(LoadError(RewriteBase + bad), Does.Contain("not a plain identifier"));
    }

    [Test]
    public void ShouldRejectARewriteToItself()
    {
        var bad = Rewrite.Replace("to       = \"mmapi_local_get\"", "to       = \"local_get\"");

        Assert.That(LoadError(RewriteBase + bad), Does.Contain("rewrites 'local_get' to itself"));
    }

    [Test]
    public void ShouldRequireANonNegativeIntegerArity()
    {
        foreach (var badArgs in new[] { "args     = -1", "args     = \"1\"", "args     = true" })
        {
            var bad = Rewrite.Replace("args     = 1", badArgs);

            Assert.That(LoadError(RewriteBase + bad),
                Does.Contain("`args` must be a non-negative integer"), badArgs);
        }
    }

    [Test]
    public void ShouldRejectUndeclaredRewriteProvides()
    {
        var bad = Rewrite.Replace("provides = [\"local.get\", \"local.missing\"]",
            "provides = [\"local.get\", \"local.missing\", \"local.ghost\"]");

        var message = LoadError(RewriteBase + bad);

        Assert.That(message, Does.Contain("local.ghost"));
        Assert.That(message, Does.Contain("[[hook]] stanza"));
    }

    [Test]
    public void ShouldRejectADuplicateRewriteId()
    {
        var second = Rewrite.Replace("callee   = \"local_get\"", "callee   = \"other_fn\"")
            .Replace("to       = \"mmapi_local_get\"", "to       = \"mmapi_other_fn\"");

        var message = LoadError(RewriteBase + Rewrite + second);

        Assert.That(message, Does.Contain("duplicate id/name 'local_get_dispatch'"));
    }

    [Test]
    public void ShouldRejectADuplicateCallee()
    {
        var second = Rewrite.Replace("id       = \"local_get_dispatch\"",
            "id       = \"second_dispatch\"");

        var message = LoadError(RewriteBase + Rewrite + second);

        Assert.That(message, Does.Contain("both rewrite 'local_get'"));
    }

    [Test]
    public void ShouldRejectChainedRewrites()
    {
        var second = Rewrite.Replace("id       = \"local_get_dispatch\"", "id       = \"chained\"")
            .Replace("callee   = \"local_get\"", "callee   = \"mmapi_local_get\"")
            .Replace("to       = \"mmapi_local_get\"", "to       = \"deeper\"");

        var message = LoadError(RewriteBase + Rewrite + second);

        Assert.That(message, Does.Contain("must not chain"));
    }
}
