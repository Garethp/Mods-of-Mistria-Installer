using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

public enum LintGateOutcome
{
    Ran,
    NoBackend,
    NotReached,
}

// The read-only lint result for one mod: would the apply install it, and what
// would it say along the way? ExitCode is the CLI contract: 0 when the mod
// installs (findings and warnings may still print), 1 when its manifest is
// invalid or the apply would exclude it.
public class ModLintResult(string modId, string version, string? symbol, int gmlFileCount,
    LintGateOutcome gate,
    IReadOnlyList<string> manifestErrors, IReadOnlyList<string> manifestWarnings,
    IReadOnlyList<LintFinding> findings, IReadOnlyList<string> exclusionReasons,
    int registrationCount = 0)
{
    public string ModId { get; } = modId;

    public string Version { get; } = version;

    // Null when the mod ships neither a gml/ tree nor extension registrations
    // (manifest checks still ran)
    public string? Symbol { get; } = symbol;

    public int GmlFileCount { get; } = gmlFileCount;

    // Extension registrations the mod ships. They stage with it, as on the
    // apply, so a mod of registrations alone still enters the GML layer.
    public int RegistrationCount { get; } = registrationCount;

    public LintGateOutcome Gate { get; } = gate;

    public bool GateRan => Gate == LintGateOutcome.Ran;

    public IReadOnlyList<string> ManifestErrors { get; } = manifestErrors;

    public IReadOnlyList<string> ManifestWarnings { get; } = manifestWarnings;

    public IReadOnlyList<LintFinding> Findings { get; } = findings;

    public IReadOnlyList<string> ExclusionReasons { get; } = exclusionReasons;

    public bool Ok => ManifestErrors.Count == 0 && ExclusionReasons.Count == 0;

    public int ExitCode => Ok ? 0 : 1;
}

// Would the apply install this mod? Runs the manifest validation and the same
// GML staging as the apply - the skip pass, the three lints and the compile
// gate - against a pristine zip, and writes nothing. The rendered output is
// mod-development and bug-report material, so it stays literal English like
// the findings it carries.
public static class ModLinter
{
    // Stages the whole layer with this one mod in the apply set. StrictLints
    // rides options exactly as it does on the apply; the caller resolves the
    // gate so --compile-check composes. Throws SeamStagingException when the
    // catalog no longer matches the pristine zip, which says nothing about
    // the mod - the CLI reports that as "cannot lint", not as a failure.
    public static ModLintResult Lint(IMod mod, IPristineSource pristine, ICompileGate? gate,
        GmlLayerOptions? options = null, SeamCatalog? catalog = null)
    {
        if (catalog is null)
        {
            var (name, bytes) = PayloadResolver.SeamCatalog();
            catalog = SeamCatalogLoader.Load(bytes, name);
        }

        // The same gate the install flow applies: a mod whose manifest is
        // invalid is skipped before its GML is ever read, but lint still runs
        // the GML checks so the modder sees everything in one pass.
        var validation = mod.Validate();
        var manifestErrors = validation.Errors.Select(e => e.Message).ToList();
        var manifestWarnings = validation.Warnings.Select(w => w.Message).ToList();

        // Registrations lint the way they install. A collector problem
        // excludes the mod before staging. Valid registrations stage with
        // the mod, so the generated registry resolves. The ledger is
        // in-memory only.
        ExtensionCollection? collected = null;
        if (ExtensionCollector.HasRegistrations(mod)) collected = ExtensionCollector.Collect(mod, catalog);
        var registrations = collected?.Registrations ?? [];
        IReadOnlyList<LintFinding> registrationFindings = collected?.Findings ?? [];

        // The letters advisory runs for every mod, gml tree or not. Lint sees
        // this one mod, so the valid set is the vanilla roster plus its own
        // registrations, and a sender another installed mod registers still
        // warns here.
        List<LintFinding> letterFindings = [];
        var natives = ExtensionCollector.NpcNativeNames(catalog, pristine);
        if (natives is not null)
        {
            var valid = new HashSet<string>(natives, StringComparer.Ordinal);
            valid.UnionWith(registrations.Select(r => r.Symbol));
            valid.UnionWith(registrations.Select(r => r.LocalName));
            ExtensionCollector.CheckLetterSenders(mod, valid, letterFindings);
        }

        // A mod of registrations alone still owns an install namespace and
        // still has to be excludable, so it enters the layer like the apply
        // lets it.
        var code = GmlModCollector.Collect(mod, evenWithoutGml: collected is not null);
        if (code is null)
            return new ModLintResult(mod.GetId(), mod.GetVersion(), null, 0, LintGateOutcome.NotReached,
                manifestErrors, manifestWarnings, letterFindings, []);

        if (collected is not null && collected.Problems.Count > 0)
            return new ModLintResult(mod.GetId(), mod.GetVersion(), code.Symbol, code.GmlFiles.Count,
                LintGateOutcome.NotReached, manifestErrors, manifestWarnings,
                registrationFindings.Concat(letterFindings).ToList(),
                collected.Problems.ToList(), registrations.Count);

        var plan = GmlLayer.Stage(catalog, pristine, [code], gate, options,
            registrations, new MemoryExtensionLedger());

        return new ModLintResult(mod.GetId(), mod.GetVersion(), code.Symbol, code.GmlFiles.Count,
            gate is not null ? LintGateOutcome.Ran : LintGateOutcome.NoBackend, manifestErrors, manifestWarnings,
            plan.Findings.Concat(registrationFindings).Concat(letterFindings).ToList(),
            plan.Excluded.SelectMany(e => e.Reasons).ToList(), registrations.Count);
    }

    // The human-readable report. Findings print even on OK: they are the
    // warnings the apply would log.
    public static string RenderText(ModLintResult result, string source)
    {
        List<string> lines = [$"lint {result.ModId} v{result.Version} ({source})"];

        if (result.Symbol is null)
            lines.Add("  no gml/ tree and no registrations: manifest checks only");
        else
            lines.Add($"  gml: {result.GmlFileCount} file(s) installing under scripts/{result.Symbol}/"
                      + result.Gate switch
                      {
                          LintGateOutcome.Ran => "",
                          LintGateOutcome.NoBackend => "; compile gate skipped (no checker backend)",
                          _ => "; not staged, the registration problems below exclude the mod",
                      });

        if (result.RegistrationCount > 0)
            lines.Add($"  registrations: {result.RegistrationCount} staged with the mod");

        lines.AddRange(result.ManifestErrors.Select(error => $"  manifest ERROR: {error}"));
        lines.AddRange(result.ManifestWarnings.Select(warning => $"  manifest warning: {warning}"));
        lines.AddRange(result.Findings.Select(finding => $"  finding: {finding}"));
        lines.AddRange(result.ExclusionReasons.Select(reason => $"  EXCLUDED: {reason}"));

        lines.Add(result.Ok
            ? "  RESULT: OK - the apply would install this mod"
              + (result.Findings.Count > 0 || result.ManifestWarnings.Count > 0 ? " (with warnings)" : "")
            : "  RESULT: FAIL - the apply would skip this mod");

        return string.Join("\n", lines);
    }
}
