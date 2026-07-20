using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// StrictLints escalates file-bearing lint findings into exclusions; FailOnSkip
// turns any exclusion into an abort before the rebuild begins. Both are CLI
// flags for CI and mod development; the GUI never sets them.
public class GmlLayerOptions
{
    public bool StrictLints { get; init; }

    public bool FailOnSkip { get; init; }
}

// Stages the whole GML layer in memory: the mmapi framework, each behavioural
// mod's gml, the seamed engine files and the generated hook catalog. Nothing
// is written; a stale anchor throws before the store is touched, and every
// mod-content failure excludes that one mod and proceeds (D12).
public static class GmlLayer
{
    public static GmlLayerPlan Stage(SeamCatalog catalog, IPristineSource pristine,
        IReadOnlyList<GmlModCode> mods, ICompileGate? gate, GmlLayerOptions? options = null)
    {
        options ??= new GmlLayerOptions();
        var plan = new GmlLayerPlan();

        // 1. The mmapi framework, delivered verbatim (MMAPI-001..015)
        foreach (var (name, bytes) in PayloadResolver.MmapiSources())
            plan.Added[SeamStager.MmapiTreePrefix + name] = bytes;

        // 2. Each mod's gml under its own symbol dir. A symbol clash excludes
        //    the later mod; an unsafe path is mod content, not a crash.
        Dictionary<string, string> symbolOwners = new() { { "mmapi", "the mmapi framework" } };
        List<GmlModCode> live = [];
        foreach (var mod in mods)
        {
            if (symbolOwners.TryGetValue(mod.Symbol, out var owner))
            {
                // removeFiles false: the prefix belongs to the earlier owner
                Exclude(plan, mod,
                    [$"shares the install namespace 'scripts/{mod.Symbol}/' with {owner} - give one of them a distinct manifest id"],
                    removeFiles: false);
                continue;
            }

            var pathProblems = mod.GmlFiles
                .Select(rel => Utils.PathSafety.PathProblem(
                    $"assets/gml/scripts/{mod.Symbol}/{rel["gml/".Length..]}", $"mod '{mod.Id}' gml"))
                .OfType<string>()
                .ToList();
            if (pathProblems.Count > 0)
            {
                Exclude(plan, mod, pathProblems, removeFiles: false);
                continue;
            }

            symbolOwners[mod.Symbol] = $"mod '{mod.Id}'";
            foreach (var rel in mod.GmlFiles)
                plan.Added[$"assets/gml/scripts/{mod.Symbol}/{rel["gml/".Length..]}"] = mod.Read(rel);
            live.Add(mod);
        }

        // 3. The seam catalog, staged against pristine. A stale anchor, a
        //    decode failure or a marker collision throws here, batched, with
        //    the previous install still live (SeamStagingException).
        var stage = SeamStager.StageAll(catalog, pristine);
        plan.Seamed = stage.Files;
        plan.Added[SeamStager.HookCatalogRel] = Encoding.UTF8.GetBytes(stage.HookCatalogGml);

        // 4. The skip pass over the future tree
        var symbols = live.ToDictionary(m => m.Id, GmlModLint.ScanSymbols);
        var treeExports = SkipPass.FutureTreeExports(pristine, stage.Files, plan.Added);
        var (survivors, skipped) = SkipPass.Run(live, symbols, treeExports);
        foreach (var (mod, reasons) in skipped) Exclude(plan, mod, reasons);

        // 5. requires_hooks against declared hooks plus aliases; a miss
        //    excludes the mod (the remedy is a newer installer, but the other
        //    mods are fine)
        HashSet<string> declared = [.. catalog.Hooks];
        foreach (var declaration in catalog.HookDeclarations) declared.UnionWith(declaration.Aliases);
        foreach (var mod in survivors.ToList())
        {
            var missing = mod.RequiredHooks.Where(h => !declared.Contains(h)).ToList();
            if (missing.Count == 0) continue;

            survivors.Remove(mod);
            Exclude(plan, mod,
                [string.Format(Resources.CoreModRequiresMissingHooks, string.Join(", ", missing))]);
        }

        // 6. The three lints; StrictLints escalates file-bearing findings into
        //    exclusions, file-less cross-mod findings stay warnings (D12)
        plan.Findings.AddRange(GmlModLint.LintHooks(survivors, catalog));
        plan.Findings.AddRange(GmlModLint.LintSymbols(survivors, symbols));
        plan.Findings.AddRange(GmlModLint.LintMmapiCalls(survivors, symbols, treeExports));
        foreach (var finding in plan.Findings) Logger.Log($"  ! {finding}");
        if (options.StrictLints)
        {
            foreach (var mod in survivors.ToList())
            {
                var blocking = plan.Findings
                    .Where(f => f.ModId == mod.Id && f.File.Length > 0)
                    .Select(f => $"strict-lints: {f.File}:{f.Line}: {f.Message}")
                    .ToList();
                if (blocking.Count == 0) continue;

                survivors.Remove(mod);
                Exclude(plan, mod, blocking);
            }
        }

        // 7. The compile gate, against staged bytes materialised to a scratch
        //    dir. The shared set failing is a framework or catalog bug and
        //    throws; a single mod's failure excludes that mod.
        if (gate is not null) RunGate(gate, plan, stage.Files, survivors);

        if (options.FailOnSkip && plan.Excluded.Count > 0)
        {
            var listing = string.Join("\n", plan.Excluded
                .SelectMany(e => e.Reasons.Select(r => $"  - mod '{e.Mod.Id}' v{e.Mod.Version}: {r}")));
            throw new InvalidOperationException(
                $"{plan.Excluded.Count} mod(s) would be skipped and fail-on-skip is set:\n{listing}");
        }

        plan.Survivors.AddRange(survivors);
        return plan;
    }

    private static void RunGate(ICompileGate gate, GmlLayerPlan plan,
        IReadOnlyDictionary<string, StagedFile> staged, List<GmlModCode> survivors)
    {
        var scratch = Path.Combine(Path.GetTempPath(), $"momi_stage_{Guid.NewGuid():N}");
        try
        {
            // mirror the real tree: an injective mapping, keeping the .gml
            // suffix so the compat dialect applies
            string Materialise(string rel, byte[] data)
            {
                var target = Path.Combine(scratch, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, data);
                return target;
            }

            var shared = staged.Keys.Order(StringComparer.Ordinal)
                .Select(rel => Materialise(rel, staged[rel].Encode()))
                .Concat(plan.Added.Keys
                    .Where(rel => rel.StartsWith(SeamStager.MmapiTreePrefix, StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal)
                    .Select(rel => Materialise(rel, plan.Added[rel])))
                .ToList();
            Logger.Log($"  compile gate: {shared.Count} seamed + framework file(s)...");
            gate.RunFiles(shared);

            foreach (var mod in survivors.ToList())
            {
                // the mod's chunks as one unit, the way the boot's
                // global-script compile sees them
                var prefix = $"assets/gml/scripts/{mod.Symbol}/";
                var targets = plan.Added.Keys
                    .Where(rel => rel.StartsWith(prefix, StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal)
                    .Select(rel => Materialise(rel, plan.Added[rel]))
                    .ToList();
                try
                {
                    gate.RunUnit(targets);
                }
                catch (InvalidOperationException exception)
                {
                    survivors.Remove(mod);
                    Exclude(plan, mod, [$"does not compile: {exception.Message}"]);
                }
            }

            Logger.Log("  compile gate: OK");
        }
        finally
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
        }
    }

    private static void Exclude(GmlLayerPlan plan, GmlModCode mod, List<string> reasons,
        bool removeFiles = true)
    {
        if (removeFiles)
        {
            var prefix = $"assets/gml/scripts/{mod.Symbol}/";
            foreach (var rel in plan.Added.Keys.Where(r => r.StartsWith(prefix, StringComparison.Ordinal)).ToList())
                plan.Added.Remove(rel);
        }

        var excluded = new ExcludedMod(mod);
        excluded.Reasons.AddRange(reasons);
        plan.Excluded.Add(excluded);
        foreach (var reason in reasons)
            Logger.Log($"  ! skipped mod '{mod.Id}' v{mod.Version}: {reason}");
    }
}
