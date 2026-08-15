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
// mod-content failure excludes that one mod and proceeds.
public static class GmlLayer
{
    // The exclusion fixpoint's backstop. Survivors shrink monotonically and
    // the loop is bounded by the mod count, but an installer must not be able
    // to spin whatever the proof says.
    private const int MaxRounds = 64;

    public static GmlLayerPlan Stage(SeamCatalog catalog, IPristineSource pristine,
        IReadOnlyList<GmlModCode> mods, ICompileGate? gate, GmlLayerOptions? options = null,
        IReadOnlyList<ExtensionRegistration>? registrations = null,
        IExtensionLedger? ledger = null)
    {
        options ??= new GmlLayerOptions();
        registrations ??= [];
        ledger ??= new MemoryExtensionLedger();
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

        // 3. The seam catalog, staged against pristine, once. This is the snapshot
        //    the exclusion loop re-expands against. Call rewrites are the
        //    expensive part and never re-run.
        var stage = SeamStager.StageAll(catalog, pristine);
        plan.Added[SeamStager.HookCatalogRel] = Encoding.UTF8.GetBytes(stage.HookCatalogGml);

        // 3b-7. A dropped mod's generated lines must disappear with it, so
        //    each round re-derives from the snapshot rather than unsplicing.
        //    The loop converges because generated text is per-registrant independent and
        //    survivors shrink monotonically.
        var symbols = live.ToDictionary(m => m.Id, GmlModLint.ScanSymbols);
        var survivors = live;
        List<string> generated = [];
        var rounds = 0;

        while (true)
        {
            if (++rounds > MaxRounds)
                throw new InvalidOperationException(
                    $"the GML layer did not settle in {MaxRounds} rounds - each round should "
                    + "drop at least one mod, so this is a bug in the exclusion loop, not "
                    + "something a mod set can cause");

            // last round's generated files describe a mod set that no longer
            // holds, so drop them before re-deriving
            foreach (var rel in generated) plan.Added.Remove(rel);
            generated.Clear();

            var staged = stage.Files.ToDictionary(f => f.Key, f => f.Value.Clone());
            var surviving = survivors.Select(m => m.Id).ToHashSet();
            var expansion = ExtensionExpander.Expand(catalog,
                registrations.Where(r => surviving.Contains(r.ModId)).ToList(),
                ledger, staged, pristine);
            foreach (var (rel, bytes) in expansion.Added)
            {
                plan.Added[rel] = bytes;
                generated.Add(rel);
            }

            plan.Seamed = staged;
            plan.NewAssignments.Clear();
            plan.NewAssignments.AddRange(expansion.NewAssignments);

            var before = survivors.Count;

            // 4. The skip pass over the future tree
            var treeExports = SkipPass.FutureTreeExports(pristine, staged, plan.Added);
            var (kept, skipped) = SkipPass.Run(survivors, symbols, treeExports);
            survivors = kept;
            foreach (var (mod, reasons) in skipped) Exclude(plan, mod, reasons);

            // 5. requires_hooks against declared hooks plus aliases; a miss
            //    excludes the mod (the remedy is a newer installer, but the
            //    other mods are fine)
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

            // 6. The three lints. StrictLints escalates file-bearing findings
            //    into exclusions, file-less cross-mod findings stay warnings.
            //    Recomputed per round, so a dropped mod's findings do
            //    not linger.
            plan.Findings.Clear();
            plan.Findings.AddRange(GmlModLint.LintHooks(survivors, catalog));
            plan.Findings.AddRange(GmlModLint.LintSymbols(survivors, symbols));
            plan.Findings.AddRange(GmlModLint.LintMmapiCalls(survivors, symbols, treeExports));
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

            // 7. The compile gate, against staged bytes materialised to a
            //    scratch dir. The shared set failing is a framework or catalog
            //    bug and throws. A single mod's failure excludes that mod.
            if (gate is not null) RunGate(gate, plan, staged, survivors);

            if (survivors.Count == before) break;

            // Nothing generated depends on the survivor set, so a second round
            // would reach the same answer at the cost of another compile gate.
            if (registrations.Count == 0) break;
        }

        foreach (var finding in plan.Findings) Logger.Log($"  ! {finding}");

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

            var shared = staged.Keys.Where(IsGml).Order(StringComparer.Ordinal)
                .Select(rel => Materialise(rel, staged[rel].Encode()))
                .Concat(plan.Added.Keys
                    .Where(rel => rel.StartsWith(SeamStager.MmapiTreePrefix, StringComparison.Ordinal))
                    .Where(IsGml)
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
                    .Where(IsGml)
                    .Order(StringComparer.Ordinal)
                    .Select(rel => Materialise(rel, plan.Added[rel]))
                    .ToList();

                // a registration-only mod ships no code, and there is nothing to
                // compile and an empty unit is not a meaningful question
                if (targets.Count == 0) continue;

                try
                {
                    gate.RunUnit(targets);
                }
                catch (InvalidOperationException exception)
                {
                    survivors.Remove(mod);
                    Exclude(plan, mod, [FormatCompileError(exception.Message, scratch)]);
                }
            }

            Logger.Log("  compile gate: OK");
        }
        finally
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
        }
    }

    // The gate compiles GML. Today every staged and added path is already a
    // .gml under assets/gml/, so this filter changes nothing, but that is a
    // property of where files happen to live, not something anything states.
    // Extension points will add data files to plan.Added (the vacancy fiddle
    // stubs), and feeding one to momi-gml-check would be a confusing compile
    // failure over a file that was never GML. Say what the gate takes.
    private static bool IsGml(string rel) => rel.EndsWith(".gml", StringComparison.Ordinal);

    // The user-facing shape of a compile-gate failure. The checker reports the
    // absolute staged path twice per diagnostic
    // (<scratch>/assets/gml/scripts/<id>/F.gml: <msg> at <same>:<line>), so
    // each line is rewritten to scripts/<id>/F.gml:<line>: <msg> under a
    // "Compile Error:" heading. Anything unrecognised passes through with only
    // the scratch prefix trimmed: a reason can get shorter here, never lost.
    public static string FormatCompileError(string message, string scratch)
    {
        // both separators, so a forward-slash path is trimmed on any platform
        string Trim(string text) => new[] { Path.DirectorySeparatorChar, '/' }
            .Select(sep => $"{scratch}{sep}assets{sep}gml{sep}")
            .Aggregate(text, (current, prefix) => current.Replace(prefix, ""));

        var lines = message.Replace("\r\n", "\n").Split('\n');

        // Only the gate's compile-failure message carries the
        // "compile pass FAILED (exit N):" scaffold on its first line. Its
        // operational throws (vanished staged file, launch failure) are
        // scaffold-less single lines and surface raw rather than losing their
        // reason to the Skip below.
        if (!lines[0].StartsWith("compile pass FAILED", StringComparison.Ordinal))
            return Trim(message);

        var diagnostics = lines.Skip(1).Select(line =>
        {
            line = Trim(line);

            var sep = line.IndexOf(": ", StringComparison.Ordinal);
            if (sep < 0) return line;
            var path = line[..sep];
            var rest = line[(sep + 2)..];

            // hoist the line number out of the trailing " at <path>:<line>";
            // a diagnostic without one keeps its original path: message shape
            var tail = $" at {path}:";
            var at = rest.LastIndexOf(tail, StringComparison.Ordinal);
            return at >= 0 && int.TryParse(rest[(at + tail.Length)..], out var lineNumber)
                ? $"{path}:{lineNumber}: {rest[..at]}"
                : $"{path}: {rest}";
        });

        return "Compile Error:\n" + string.Join("\n", diagnostics);
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
