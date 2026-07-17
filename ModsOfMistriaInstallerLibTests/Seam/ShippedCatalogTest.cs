using System.Text;
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
        Assert.That(_catalog.Hooks, Has.Count.GreaterThanOrEqualTo(64));

        var runtime = _catalog.HookDeclarations
            .Where(d => d.Provider == HookProvider.Runtime)
            .Select(d => d.Name)
            .ToList();
        Assert.That(runtime, Does.Contain("game.room_changed"));
        Assert.That(runtime, Does.Contain("game.day_started"));
        Assert.That(runtime, Does.Contain("game.title_entered"));
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
