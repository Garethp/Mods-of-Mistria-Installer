using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// Boot-brick protection. GML hoists every top-level function into one flat
// global namespace, so a name two mods (or a mod and the engine) both define
// is a duplicate-export compile error at boot. The future tree's exports are
// scanned and a colliding mod is skipped so everything else still lands and
// the game boots. Textual; the engine compile stays the final authority.
public static class SkipPass
{
    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    // Every top-level function name the post-apply engine tree will export →
    // a human-readable origin. Scanned from the post-seam view (staged text
    // where it exists, pristine bytes otherwise, so wrap-renamed
    // __mmapi_orig_* definitions are seen) plus the staged framework files.
    // Mod files are scanned separately, per mod, so collisions attribute to
    // the right owner.
    public static Dictionary<string, string> FutureTreeExports(IPristineSource pristine,
        IReadOnlyDictionary<string, StagedFile> staged, IReadOnlyDictionary<string, byte[]> added)
    {
        Dictionary<string, string> exports = [];

        void Scan(string text, string origin)
        {
            foreach (var span in GmlScanner.TopLevelDefinitions(text))
            {
                if (span.Form == FunctionForm.Decl) exports.TryAdd(span.Name, origin);
            }
        }

        foreach (var rel in pristine.GmlFiles())
        {
            var stagedFile = staged.GetValueOrDefault(rel);
            if (stagedFile is not null)
            {
                Scan(stagedFile.Text, $"the engine ({rel})");
                continue;
            }

            try
            {
                Scan(Utf8Strict.GetString(pristine.Read(rel) ?? []), $"the engine ({rel})");
            }
            catch (DecoderFallbackException)
            {
            }
        }

        foreach (var (rel, data) in added)
        {
            if (!rel.StartsWith(SeamStager.MmapiTreePrefix, StringComparison.Ordinal)) continue;

            var name = rel[(rel.LastIndexOf('/') + 1)..];
            Scan(Encoding.UTF8.GetString(data), $"the mmapi framework ({name})");
        }

        return exports;
    }

    // Mods in apply order: the first surviving definition owns the name, a
    // collision or an intra-mod duplicate export skips the mod with reasons
    // naming both sites. A mod skipped for colliding with a mod the gate later
    // rejects stays skipped - conservative, never less safe.
    public static (List<GmlModCode> Survivors, List<(GmlModCode Mod, List<string> Reasons)> Skipped) Run(
        IReadOnlyList<GmlModCode> mods, IReadOnlyDictionary<string, ModSymbols> symbols,
        IReadOnlyDictionary<string, string> treeExports)
    {
        List<GmlModCode> survivors = [];
        List<(GmlModCode Mod, List<string> Reasons)> skipped = [];
        Dictionary<string, string> winners = [];  // fn name → surviving mod id that owns it

        foreach (var mod in mods)
        {
            var syms = symbols[mod.Id];
            List<string> reasons = [];

            foreach (var (name, (rel, line)) in syms.Functions.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                var origin = treeExports.GetValueOrDefault(name);
                if (origin is null && winners.TryGetValue(name, out var winner)) origin = $"mod '{winner}'";
                if (origin is not null)
                    reasons.Add($"defines function '{name}' ({rel}:{line}), already defined by {origin}");
            }

            foreach (var (name, sites) in syms.Duplicates.OrderBy(d => d.Key, StringComparer.Ordinal))
            {
                var (firstRel, firstLine) = syms.Functions[name];
                reasons.AddRange(sites.Select(site =>
                    $"defines function '{name}' more than once ({firstRel}:{firstLine} and "
                    + $"{site.File}:{site.Line}) - a duplicate export within one mod bricks the boot too"));
            }

            if (reasons.Count > 0)
            {
                skipped.Add((mod, reasons));
                continue;
            }

            survivors.Add(mod);
            foreach (var name in syms.Functions.Keys) winners[name] = mod.Id;
        }

        return (survivors, skipped);
    }
}
