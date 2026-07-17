using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class GmlScannerTest
{
    private const string Source = """
        // a comment with function decoy(x) { in it
        function outer(a, b=2) : Parent(a) constructor {
            /* block comment function decoy2() { */
            static describe = function(kind, label="x") {
                var s = "a string with function decoy3() { and // no comment";
                return kind + label;
            }

            on_free = function() {
                cleanup();
            }
        }

        function helper(value) {
            if !is_struct(value) {
                value = wrap(value);
            }
            return value;
        }
        """ + "\n";

    private static List<string> Texts(string source) =>
        GmlScanner.Tokenize(source)
            .Select(t => source[t.Start..t.End])
            .ToList();

    private static List<(string Name, bool Bare)> WriteNames(string source) =>
        GmlScanner.FindGlobalWrites(source)
            .Select(w => (w.Name, w.Bare))
            .ToList();

    [Test]
    public void ShouldStripCommentsAndKeepAStringAsOneToken()
    {
        var tokens = Texts(Source);

        Assert.That(tokens, Does.Not.Contain("decoy"));
        Assert.That(tokens, Does.Not.Contain("decoy2"));
        Assert.That(tokens, Does.Contain("\"a string with function decoy3() { and // no comment\""));
        Assert.That(tokens, Does.Contain("describe"));
    }

    [Test]
    public void ShouldTokenizeWordRunsAndPunctuation()
    {
        var tokens = Texts("hp -= amount;");

        Assert.That(tokens, Is.EqualTo(new[] { "hp", "-", "=", "amount", ";" }));
    }

    [Test]
    public void ShouldFindDeclWithConstructorInheritance()
    {
        var spans = GmlScanner.FindFunctions(Source, "outer");

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].Form, Is.EqualTo(FunctionForm.Decl));
        Assert.That(spans[0].Args, Is.EqualTo(new[] { "a", "b" }));

        var body = Source[spans[0].BodyOpen..spans[0].BodyClose];
        Assert.That(body, Does.Contain("describe"));
        Assert.That(body, Does.Contain("on_free"));
    }

    [Test]
    public void ShouldNotDoubleCountTheStaticFormAsAssign()
    {
        var spans = GmlScanner.FindFunctions(Source, "describe");

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].Form, Is.EqualTo(FunctionForm.Static));
        Assert.That(spans[0].Args, Is.EqualTo(new[] { "kind", "label" }));
        Assert.That(spans[0].Params, Is.EqualTo("kind, label=\"x\""));
    }

    [Test]
    public void ShouldFindTheAssignForm()
    {
        var spans = GmlScanner.FindFunctions(Source, "on_free");

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].Form, Is.EqualTo(FunctionForm.Assign));
        Assert.That(spans[0].Args, Is.Empty);
    }

    [Test]
    public void ShouldFindNothingForAnAbsentFunction()
    {
        Assert.That(GmlScanner.FindFunctions(Source, "missing"), Is.Empty);
    }

    [Test]
    public void ShouldMatchAnAnchorWhitespaceInsensitively()
    {
        var spans = GmlScanner.FindFunctions(Source, "helper");

        var matches = GmlScanner.FindAnchor(Source, spans[0].BodyOpen, spans[0].BodyClose,
            "if !is_struct(value) {\n    value = wrap(value); }");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(Source[matches[0].Start..], Does.StartWith("if !is_struct"));
    }

    [Test]
    public void ShouldNotMatchAnAnchorOutsideTheRegion()
    {
        var spans = GmlScanner.FindFunctions(Source, "describe");

        var matches = GmlScanner.FindAnchor(Source, spans[0].BodyOpen, spans[0].BodyClose,
            "value = wrap(value);");

        Assert.That(matches, Is.Empty);
    }

    [Test]
    public void ShouldReturnTheLineIndent()
    {
        var pos = Source.IndexOf("static describe", StringComparison.Ordinal);

        Assert.That(GmlScanner.LineIndent(Source, pos), Is.EqualTo("    "));
    }

    [Test]
    public void ShouldExcludeNestedDefinitionsFromTopLevel()
    {
        var spans = GmlScanner.TopLevelDefinitions(Source);

        Assert.That(spans.Select(s => (s.Name, s.Form)), Is.EqualTo(new[]
        {
            ("outer", FunctionForm.Decl),
            ("helper", FunctionForm.Decl),
        }));
    }

    [Test]
    public void ShouldDepthTrackBracesInsideEnumsAndStructLiterals()
    {
        var source = "enum Kind {\n    A,\n    B,\n}\n"
                     + "var t = { fn: 1 };\n"
                     + "function real_export() {\n    var x = { a: 1 };\n}\n";

        var spans = GmlScanner.TopLevelDefinitions(source);

        Assert.That(spans.Select(s => s.Name), Is.EqualTo(new[] { "real_export" }));
    }

    [Test]
    public void ShouldReportTopLevelAssignFormsWithTheirForm()
    {
        var source = "top_assign = function() {\n}\nfunction top_decl() {\n}\n";

        var spans = GmlScanner.TopLevelDefinitions(source);

        Assert.That(spans.Select(s => (s.Name, s.Form)), Is.EqualTo(new[]
        {
            ("top_assign", FunctionForm.Assign),
            ("top_decl", FunctionForm.Decl),
        }));
    }

    [Test]
    public void ShouldNeverMatchCommentsOrStringsAsDefinitions()
    {
        var source = "// function ghost_a() {\n"
                     + "var s = \"function ghost_b() {\";\n"
                     + "function real_fn() {\n}\n";

        var spans = GmlScanner.TopLevelDefinitions(source);

        Assert.That(spans.Select(s => s.Name), Is.EqualTo(new[] { "real_fn" }));
    }

    [Test]
    public void ShouldFindBareAndDeepWrites()
    {
        var source = "global.alpha = 1;\n"
                     + "global.beta.field = 2;\n"
                     + "global.gamma[3] = 4;\n";

        Assert.That(WriteNames(source), Is.EqualTo(new[]
        {
            ("alpha", true),
            ("beta", false),
            ("gamma", false),
        }));
    }

    [Test]
    public void ShouldFindAccessorFormAndCompoundAssigns()
    {
        var source = "global[$ \"delta\"] = 1;\n"
                     + "global.count += 1;\n"
                     + "global.lazy ??= {};\n";

        Assert.That(WriteNames(source), Is.EqualTo(new[]
        {
            ("delta", true),
            ("count", true),
            ("lazy", true),
        }));
    }

    [Test]
    public void ShouldNeverMatchReadsOrComparisonsAsWrites()
    {
        var source = "if (global.alpha == 1) { x(); }\n"
                     + "if (global.beta != 2) { y(); }\n"
                     + "var v = global[$ \"gamma\"];\n"
                     + "show(global.delta);\n";

        Assert.That(WriteNames(source), Is.Empty);
    }

    [Test]
    public void ShouldKeepIncrementsAndUnaryMinusAsABlindSpot()
    {
        // `global.a - -b` tokenizes exactly like a postfix decrement, so
        // neither increments nor that expression may register as writes
        var source = "global.ticks++;\n"
                     + "var d = global.a - -b;\n"
                     + "var e = global.c + +f;\n";

        Assert.That(WriteNames(source), Is.Empty);
    }

    [Test]
    public void ShouldNeverMatchCommentsOrStringsAsWrites()
    {
        var source = "// global.ghost = 1\n"
                     + "var s = \"global.ghost2 = 1\";\n"
                     + "global.real = 1;\n";

        Assert.That(WriteNames(source), Is.EqualTo(new[] { ("real", true) }));
    }
}
