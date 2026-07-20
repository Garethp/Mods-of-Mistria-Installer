using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// The read-only lint result for one mod: would the apply install it, and what
// would it say along the way? ExitCode is the CLI contract: 0 when the mod
// installs (findings and warnings may still print), 1 when its manifest is
// invalid or the apply would exclude it.
public class ModLintResult(string modId, string version, string? symbol, int gmlFileCount, bool gateRan,
    IReadOnlyList<string> manifestErrors, IReadOnlyList<string> manifestWarnings,
    IReadOnlyList<LintFinding> findings, IReadOnlyList<string> exclusionReasons)
{
    public string ModId { get; } = modId;

    public string Version { get; } = version;

    // Null when the mod ships no gml/ tree (manifest checks still ran)
    public string? Symbol { get; } = symbol;

    public int GmlFileCount { get; } = gmlFileCount;

    public bool GateRan { get; } = gateRan;

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
// the findings it carries (D16).
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

        var code = GmlModCollector.Collect(mod);
        if (code is null)
            return new ModLintResult(mod.GetId(), mod.GetVersion(), null, 0, false,
                manifestErrors, manifestWarnings, [], []);

        var plan = GmlLayer.Stage(catalog, pristine, [code], gate, options);

        return new ModLintResult(mod.GetId(), mod.GetVersion(), code.Symbol, code.GmlFiles.Count,
            gate is not null, manifestErrors, manifestWarnings, plan.Findings,
            plan.Excluded.SelectMany(e => e.Reasons).ToList());
    }

    // The human-readable report. Findings print even on OK: they are the
    // warnings the apply would log.
    public static string RenderText(ModLintResult result, string source)
    {
        List<string> lines = [$"lint {result.ModId} v{result.Version} ({source})"];

        if (result.Symbol is null)
            lines.Add("  no gml/ tree: manifest checks only");
        else
            lines.Add($"  gml: {result.GmlFileCount} file(s) installing under scripts/{result.Symbol}/"
                      + (result.GateRan ? "" : "; compile gate skipped (no checker backend)"));

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
