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
    public void ShouldDeclareTheFishSelectionEventAtTheFishingHubBoundary()
    {
        var hook = _catalog.Hook("fishing.fish_selected");
        Assert.That(hook, Is.Not.Null);
        Assert.That(hook!.Kind, Is.EqualTo(HookKind.Event));
        Assert.That(hook.Doc, Does.Contain("selected fish"));

        var seam = _catalog.Seams.Single(s => s.Id == "fishing_fish_selected");
        Assert.That(seam.File, Is.EqualTo("assets/gml/scripts/Player/FishingHub.gml"));
        Assert.That(seam.Hooks, Is.EqualTo(new[] { "fishing.fish_selected" }));
        Assert.That(seam.Marker, Is.EqualTo("mmapi_fishing_run_fish_selected_callbacks"));
        Assert.That(seam.Op, Is.EqualTo(DispatchOp.Emit));
    }

    [Test]
    public void ShouldDeclareTheMuseumDonationAttemptEventBeforeRegistration()
    {
        var hook = _catalog.Hook("museum.donation_attempted");
        Assert.That(hook, Is.Not.Null);
        Assert.That(hook!.Kind, Is.EqualTo(HookKind.Event));
        Assert.That(hook.Doc, Does.Contain("attempted donation"));

        var seam = _catalog.Seams.Single(s => s.Id == "museum_donation_attempted");
        Assert.That(seam.File, Is.EqualTo("assets/gml/scripts/Museum.gml"));
        Assert.That(seam.Hooks, Is.EqualTo(new[] { "museum.donation_attempted" }));
        Assert.That(seam.Marker, Is.EqualTo("mmapi_museum_run_donation_attempted_callbacks"));
        Assert.That(seam.Op, Is.EqualTo(DispatchOp.Emit));
    }

    [Test]
    public void ShouldDeclareBothPetRewardSitesForOnePetRewardEvent()
    {
        var hook = _catalog.Hook("pet.reward_generated");
        Assert.That(hook, Is.Not.Null);
        Assert.That(hook!.Kind, Is.EqualTo(HookKind.Event));
        Assert.That(hook.Doc, Does.Contain("each concrete item"));

        var seams = _catalog.Seams
            .Where(s => s.Hooks.Contains("pet.reward_generated"))
            .ToList();
        Assert.That(seams.Select(s => s.Id), Is.EquivalentTo(new[]
        {
            "pet_reward_generated_forageable",
            "pet_reward_generated_item",
        }));
        Assert.That(seams, Has.All.Matches<SeamEntry>(s =>
            s.File == "assets/gml/scripts/Pet.gml" && s.Op == DispatchOp.Emit));
    }

    [Test]
    public void ShouldDeclareTheCropHarvestDestroyFilterBeforeTheDestroyBranch()
    {
        var hook = _catalog.Hook("crop.harvest_destroy");
        Assert.That(hook, Is.Not.Null);
        Assert.That(hook!.Kind, Is.EqualTo(HookKind.Filter));
        Assert.That(hook.Doc, Does.Contain("item drops, XP"));

        var seam = _catalog.Seams.Single(s => s.Id == "crop_harvest_destroy");
        Assert.That(seam.File, Is.EqualTo("assets/gml/scripts/GameplaySystems/Data/Grid/Crops.gml"));
        Assert.That(seam.Hooks, Is.EqualTo(new[] { "crop.harvest_destroy" }));
        Assert.That(seam.Marker, Is.EqualTo("mmapi_crop_run_harvest_destroy_filters"));
        Assert.That(seam.Op, Is.EqualTo(DispatchOp.Filter));
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
