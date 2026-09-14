using System.Text;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Seam;

// The real seam catalog, proven against a pristine stand-in synthesised from
// its own anchors. This is what keeps a hand-edited catalog honest without a
// game checkout: anchors that stop matching, marker collisions, ordering
// violations and lint failures all surface here.
[TestFixture]
public class ShippedCatalogTest
{
    private static readonly string PayloadDir = Path.Combine(AppContext.BaseDirectory, "Payload");

    private static readonly string[] MmapiPrefixes = ["mmapi_", "__mmapi_"];

    private SeamCatalog _catalog = null!;
    private Dictionary<string, string> _pristine = null!;

    [OneTimeSetUp]
    public void LoadShippedCatalog()
    {
        var (name, bytes) = PayloadResolver.SeamCatalog();
        _catalog = SeamCatalogLoader.Load(bytes, name);
        _pristine = PristineSynthesis.FromCatalog(_catalog);
    }

    [Test]
    public void ShouldStageAgainstItsOwnAnchors()
    {
        var pristine = new MemoryPristineSource(
            _pristine.ToDictionary(f => f.Key, f => Encoding.UTF8.GetBytes(f.Value)));

        var staged = SeamStager.Simulate(_catalog, pristine);

        Assert.That(staged.Keys.Order(StringComparer.Ordinal), Is.EqualTo(_catalog.Files));
        var applied = staged.Values
            .SelectMany(f => f.EntryIds)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.That(applied, Is.EqualTo(_catalog.Entries
            .Select(e => e.Id)
            .Order(StringComparer.Ordinal)
            .ToList()));
    }

    [Test]
    public void ShouldDeclareAndCountEveryHook()
    {
        // The shipped catalog must self-declare its integrity counts; the loader
        // enforces declared == parsed (and orphan hooks, in both directions), so
        // loading at all proves consistency. The echo here documents the contract.
        Assert.That(_catalog.DeclaredCounts, Is.Not.Null);
        Assert.That(_catalog.DeclaredCounts!.Hooks, Is.EqualTo(_catalog.HookDeclarations.Count));
        Assert.That(_catalog.DeclaredCounts.Seams, Is.EqualTo(_catalog.Seams.Count));
        Assert.That(_catalog.DeclaredCounts.EngineFixes, Is.EqualTo(_catalog.EngineFixes.Count));
        Assert.That(_catalog.DeclaredCounts.CallRewrites, Is.EqualTo(_catalog.CallRewrites.Count));

        var runtime = _catalog.HookDeclarations
            .Where(d => d.Provider == HookProvider.Runtime)
            .Select(d => d.Name)
            .ToList();
        Assert.That(runtime, Does.Contain("game.room_changed"));
        Assert.That(runtime, Does.Contain("game.day_changed"));

        // The rename kept the old name resolving: game.day_changed carries the
        // catalog's first alias.
        var dayChanged = _catalog.HookDeclarations.Single(d => d.Name == "game.day_changed");
        Assert.That(dayChanged.Aliases, Does.Contain("game.day_started"));
    }

    [Test]
    public void ShouldGroupRecordsByTypeUnderSectionBanners()
    {
        // File order is presentational and the parsed model erases it, so this
        // check reads the raw text. The convention holds records grouped by
        // type under one banner per section, ordered hook declarations, then
        // seams, then call rewrites, then engine fixes, with new records
        // appended to their type's section.
        var (_, bytes) = PayloadResolver.SeamCatalog();
        var lines = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Split('\n');

        string[] banners =
        [
            "# --- hook declarations ",
            "# --- seams ",
            "# --- call rewrites ",
            "# --- engine fixes ",
        ];
        string[] headers = ["[[hook]]", "[[seam]]", "[[call_rewrite]]", "[[engine_fix]]"];

        var section = -1;
        var perSection = new int[banners.Length];
        foreach (var line in lines)
        {
            var banner = Array.FindIndex(banners, b =>
                line.StartsWith(b, StringComparison.Ordinal)
                && line.EndsWith("---", StringComparison.Ordinal));
            if (banner >= 0)
            {
                Assert.That(banner, Is.EqualTo(section + 1), $"banner out of order: {line}");
                section = banner;
                continue;
            }

            var header = Array.IndexOf(headers, line);
            if (header < 0) continue;
            Assert.That(header, Is.EqualTo(section), $"{line} outside its section");
            perSection[header]++;
        }

        Assert.That(section, Is.EqualTo(banners.Length - 1), "not every banner is present");
        var counts = _catalog.DeclaredCounts!;
        Assert.That(perSection, Is.EqualTo(new[]
            { counts.Hooks, counts.Seams, counts.CallRewrites, counts.EngineFixes }));
    }

    [Test]
    public void ShouldKeepTheDocCountSentencesInStepWithTheCatalog()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
            Assert.Ignore("docs/MMAPI not found - running outside the repo checkout");

        var counts = _catalog.DeclaredCounts!;
        var sentence = new Regex(
            @"\*\*(\d+) hooks\*\*, fed by \*\*(\d+) seams\*\*, \*\*(\d+) engine fixes\*\*, and \*\*(\d+) call rewrites?\*\*");
        foreach (var page in (string[]) ["CATALOG.md", "SEAMS.md", "HOOKS.md"])
        {
            var text = File.ReadAllText(Path.Combine(repoRoot!, "docs", "MMAPI", page));
            var match = sentence.Match(text);
            Assert.That(match.Success, Is.True, $"{page} carries no catalog count sentence");
            Assert.That(int.Parse(match.Groups[1].Value), Is.EqualTo(counts.Hooks), $"{page} hook count");
            Assert.That(int.Parse(match.Groups[2].Value), Is.EqualTo(counts.Seams), $"{page} seam count");
            Assert.That(int.Parse(match.Groups[3].Value), Is.EqualTo(counts.EngineFixes), $"{page} engine fix count");
            Assert.That(int.Parse(match.Groups[4].Value), Is.EqualTo(counts.CallRewrites), $"{page} call rewrite count");
        }
    }

    [Test]
    public void ShouldHaveADocPageForEveryHookAndEntry()
    {
        // The count sentences are already gated. This holds the page set in
        // step with the catalog in both directions, since a missing page is an
        // undocumented hook or seam and an unmatched page is a leftover from a
        // rename.
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
            Assert.Ignore("docs/MMAPI not found - running outside the repo checkout");

        var hooksDir = Path.Combine(repoRoot!, "docs", "MMAPI", "hooks");
        var seamsDir = Path.Combine(repoRoot!, "docs", "MMAPI", "seams");
        var hookNames = _catalog.HookDeclarations.Select(d => d.Name).ToHashSet();
        var entryIds = _catalog.Entries.Select(e => e.Id)
            .Concat(_catalog.CallRewrites.Select(r => r.Id))
            .ToHashSet();

        foreach (var name in hookNames)
            Assert.That(File.Exists(Path.Combine(hooksDir, name + ".md")), Is.True,
                $"hook '{name}' has no docs page");
        foreach (var id in entryIds)
            Assert.That(File.Exists(Path.Combine(seamsDir, id + ".md")), Is.True,
                $"entry '{id}' has no docs page");

        foreach (var page in Directory.GetFiles(hooksDir, "*.md"))
            Assert.That(hookNames, Does.Contain(Path.GetFileNameWithoutExtension(page)),
                $"hook page '{Path.GetFileName(page)}' matches no declared hook");
        foreach (var page in Directory.GetFiles(seamsDir, "*.md"))
            Assert.That(entryIds, Does.Contain(Path.GetFileNameWithoutExtension(page)),
                $"seam page '{Path.GetFileName(page)}' matches no catalog entry");
    }

    [Test]
    public void ShouldResolveEveryDocsLink()
    {
        // A rename anywhere under docs/MMAPI strands the links into it, and
        // GitHub renders a stale anchor as a page that scrolls nowhere. This
        // walks every relative link in the tree and checks that the target
        // file exists and that a fragment names a heading the target has.
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
            Assert.Ignore("docs/MMAPI not found - running outside the repo checkout");

        var docsRoot = Path.Combine(repoRoot!, "docs", "MMAPI");
        var pages = Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories);
        var anchors = pages.ToDictionary(page => page, DocAnchors);

        List<string> problems = [];
        foreach (var page in pages)
        {
            var where = Path.GetRelativePath(repoRoot!, page);
            foreach (var (target, fragment) in DocLinks(page))
            {
                var path = target.Length == 0
                    ? page
                    : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, target));
                if (!anchors.TryGetValue(path, out var targetAnchors))
                {
                    if (!File.Exists(path))
                        problems.Add($"{where}: missing file {target}");
                    else if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        problems.Add($"{where}: link casing differs from the file {target}");
                    continue;
                }

                if (fragment is not null && !targetAnchors.Contains(fragment))
                    problems.Add($"{where}: no heading for {target}#{fragment}");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    private static readonly Regex DocLinkPattern = new(@"\]\(([^)#\s]*)(?:#([^)\s]+))?\)");

    private static readonly Regex DocHeadingPattern = new(@"^#{1,6}\s+(.*?)\s*$");

    private static readonly Regex DocCodeSpanPattern = new("`[^`]*`");

    // Lines outside fenced code blocks, fences indented under a list item
    // included.
    private static IEnumerable<string> DocProseLines(string page)
    {
        var inCode = false;
        foreach (var line in File.ReadLines(page))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCode = !inCode;
                continue;
            }

            if (!inCode) yield return line;
        }
    }

    // GitHub's anchor for a heading drops code ticks, lowercases, removes
    // everything but letters, digits, underscores, hyphens and spaces, turns
    // spaces into hyphens, and numbers a repeat.
    private static HashSet<string> DocAnchors(string page)
    {
        HashSet<string> anchors = [];
        Dictionary<string, int> seen = [];
        foreach (var line in DocProseLines(page))
        {
            var match = DocHeadingPattern.Match(line);
            if (!match.Success) continue;

            var text = match.Groups[1].Value.Replace("`", "").ToLowerInvariant();
            var anchor = Regex.Replace(text, @"[^\w\- ]", "").Replace(' ', '-');
            if (seen.TryGetValue(anchor, out var repeats))
            {
                seen[anchor] = repeats + 1;
                anchor = $"{anchor}-{repeats + 1}";
            }
            else
            {
                seen[anchor] = 0;
            }

            anchors.Add(anchor);
        }

        return anchors;
    }

    // Relative links only, with inline code spans blanked first so a link
    // quoted as code does not count.
    private static IEnumerable<(string Target, string? Fragment)> DocLinks(string page)
    {
        foreach (var line in DocProseLines(page))
        {
            foreach (Match match in DocLinkPattern.Matches(DocCodeSpanPattern.Replace(line, "")))
            {
                var target = match.Groups[1].Value;
                if (target.StartsWith("http", StringComparison.Ordinal)
                    || target.StartsWith("mailto:", StringComparison.Ordinal)) continue;
                yield return (target, match.Groups[2].Success ? match.Groups[2].Value : null);
            }
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "MMAPI", "CATALOG.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    [Test]
    public void ShouldCarryADocOnEveryHook()
    {
        var undocumented = _catalog.HookDeclarations
            .Where(d => d.Doc.Length == 0)
            .Select(d => d.Name)
            .ToList();

        Assert.That(undocumented, Is.Empty);
    }

    [Test]
    public void ShouldRenderEveryKindIntoTheGeneratedCatalog()
    {
        var rendered = HookCatalogRenderer.Render(_catalog);

        foreach (var declaration in _catalog.HookDeclarations)
            Assert.That(rendered,
                Does.Contain($"\"{declaration.Name}\", \"{declaration.Kind.CatalogName()}\","));
    }

    [Test]
    public void ShouldDeclareContentionOnEveryOverrideHook()
    {
        // the loader enforces this; the assertion documents the shipped split
        var overrides = _catalog.HookDeclarations
            .Where(d => d.Kind == HookKind.Override)
            .ToDictionary(d => d.Name, d => d.Contention);
        Assert.That(overrides["crafting.max_crafts"], Is.EqualTo(HookContention.Exclusive));
        Assert.That(overrides.Where(o => o.Key != "crafting.max_crafts"),
            Has.All.Matches<KeyValuePair<string, HookContention?>>(
                o => o.Value == HookContention.ClaimScoped));

        var rendered = HookCatalogRenderer.Render(_catalog);
        Assert.That(rendered, Does.Contain("\"crafting.max_crafts\", \"exclusive\","));
        Assert.That(rendered, Does.Contain("\"object.interact\", \"claim-scoped\","));
    }

    [Test]
    public void ShouldResolveEveryFrameworkCallInAReplaceBody()
    {
        // The catalog's own replace bodies are fixed at build time - they ship
        // inside the installer - so their check belongs here, where a typo
        // fails the moment it is written rather than in someone's game. The
        // compat dialect late-binds, so `mmapi_emitt(...)` in a replace body
        // compiles clean, installs clean, and silently never fires.
        var framework = Directory.GetFiles(Path.Combine(PayloadDir, "mmapi"), "*.gml")
            .Order(StringComparer.Ordinal)
            .SelectMany(path => GmlScanner.TopLevelDefinitions(File.ReadAllText(path)))
            .Where(span => span.Form == FunctionForm.Decl)
            .Select(span => span.Name)
            .ToHashSet();
        framework.UnionWith(GmlScanner.TopLevelDefinitions(HookCatalogRenderer.Render(_catalog))
            .Where(span => span.Form == FunctionForm.Decl)
            .Select(span => span.Name));
        Assert.That(framework, Does.Contain("mmapi_emit"));

        Dictionary<string, List<string>> unresolved = [];
        foreach (var entry in _catalog.Entries)
        {
            foreach (var (name, _) in GmlScanner.FindPrefixedCalls(entry.Replace, MmapiPrefixes))
            {
                if (framework.Contains(name)
                    || name.StartsWith(DispatchRenderer.OrigPrefix, StringComparison.Ordinal)) continue;
                if (!unresolved.TryGetValue(name, out var ids))
                {
                    ids = [];
                    unresolved[name] = ids;
                }

                ids.Add(entry.Id);
            }
        }

        Assert.That(unresolved, Is.Empty);

        // every call_rewrite's target too: it redirects real engine call
        // sites into a wrapper, so a wrapper that does not exist silently
        // breaks them
        var missing = _catalog.CallRewrites
            .Where(r => !framework.Contains(r.To))
            .Select(r => r.Id)
            .ToList();
        Assert.That(missing, Is.Empty);
    }
}
