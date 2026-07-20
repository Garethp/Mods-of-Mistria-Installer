using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// The read-only verification result for one build. Ok only when every seam,
// engine fix and call rewrite lands cleanly; Problems carries the same records
// the apply would have thrown. ExitCode is the CLI contract: 0 when every
// anchor holds, 1 when any broke.
public class VerifyResult(bool ok, int seamCount, int engineFixCount, int callRewriteCount,
    int engineFileCount, IReadOnlyList<SeamProblem> problems)
{
    public bool Ok { get; } = ok;

    public int SeamCount { get; } = seamCount;

    public int EngineFixCount { get; } = engineFixCount;

    public int CallRewriteCount { get; } = callRewriteCount;

    public int EngineFileCount { get; } = engineFileCount;

    public IReadOnlyList<SeamProblem> Problems { get; } = problems;

    public int ExitCode => Ok ? 0 : 1;
}

// Does a build still satisfy the catalog's anchors? Runs the same staging as
// the apply against an arbitrary build zip, renders the problems instead of
// throwing, and writes nothing (e.g. to pre-check a game update before
// installing). The rendered output is re-anchoring and bug-report material,
// so it stays literal English like the problems it carries (D16).
public static class SeamVerifier
{
    public static VerifyResult Verify(IPristineSource pristine, SeamCatalog? catalog = null)
    {
        if (catalog is null)
        {
            var (name, bytes) = PayloadResolver.SeamCatalog();
            catalog = SeamCatalogLoader.Load(bytes, name);
        }

        IReadOnlyList<SeamProblem> problems = [];
        try
        {
            SeamStager.StageAll(catalog, pristine);
        }
        catch (SeamStagingException exception)
        {
            problems = exception.Problems;
        }

        return new VerifyResult(problems.Count == 0, catalog.Seams.Count, catalog.EngineFixes.Count,
            catalog.CallRewrites.Count, catalog.Files.Count, problems);
    }

    // The install's pristine backup, for a seam check with no explicit zip. The
    // seam check never migrates (D14), so a not-yet-migrated install's legacy
    // backup name is accepted as it stands; MOMI's own name wins when both exist.
    public static string LocateBackup(string fomLocation)
    {
        var backup = new AssetsStore(fomLocation).BackupPath;
        if (File.Exists(backup)) return backup;

        var legacy = Path.Combine(fomLocation, "assets_backup.zip");
        if (File.Exists(legacy)) return legacy;

        throw new FileNotFoundException(string.Format(Resources.CoreVerifyNoBackup, fomLocation), backup);
    }

    // The machine-readable report. The kind values are D8's fixed wire names;
    // this is the JSON contract's one home.
    public static string ToJson(VerifyResult result, string source) => new JObject
    {
        ["ok"] = result.Ok,
        ["source"] = source,
        ["seams"] = result.SeamCount,
        ["engine_fixes"] = result.EngineFixCount,
        ["call_rewrites"] = result.CallRewriteCount,
        ["engine_files"] = result.EngineFileCount,
        ["problems"] = new JArray(result.Problems.Select(p => p.Message)),
        ["problem_records"] = new JArray(result.Problems.Select(p => new JObject
        {
            ["kind"] = p.Kind.WireName(),
            ["entry_id"] = p.EntryId,
            ["file"] = p.File,
            ["line"] = p.Line,
            ["hint"] = p.Hint,
            ["message"] = p.Message,
            ["context"] = p.Context,
        })),
    }.ToString(Formatting.Indented);

    // The human-readable report. The context excerpts print on failure without
    // a flag: they are the re-anchoring payload.
    public static string RenderText(VerifyResult result, string source)
    {
        List<string> lines =
        [
            $"seam-check {source}",
            $"  catalog: {result.SeamCount} seams + {result.EngineFixCount} fixes + "
            + $"{result.CallRewriteCount} call-rewrite(s), {result.EngineFileCount} engine files",
        ];

        if (result.Ok)
        {
            lines.Add("  RESULT: OK - every anchor holds against this build");
            return string.Join("\n", lines);
        }

        lines.Add("  RESULT: FAIL");
        lines.AddRange(result.Problems.Select(p => $"  - {p.Message}"));
        foreach (var problem in result.Problems.Where(p => p.Context.Length > 0))
        {
            lines.Add("");
            lines.Add($"  {(problem.EntryId.Length > 0 ? problem.EntryId : problem.Kind.WireName())}: "
                      + $"{problem.File}:{problem.Line} (closest match in this build)");
            lines.Add(problem.Context);
        }

        return string.Join("\n", lines);
    }
}
