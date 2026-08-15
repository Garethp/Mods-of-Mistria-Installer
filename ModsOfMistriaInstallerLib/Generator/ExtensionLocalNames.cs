using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Rewrites a registering mod's npc_roster content from the local short name
// to the derived symbol. It covers the fiddle prototype, t2 keys and npc values, and
// art file names. Matching is by whole _-separated segment, longest local
// first, never rescanned, so the pass is idempotent.
public static class ExtensionLocalNames
{
    public static void Expand(IMod mod, IEnumerable<ExtensionRegistration> registrations,
        Dictionary<string, string> generated, Dictionary<string, string> redirects,
        HashSet<string> hidden)
    {
        var map = registrations
            .Where(r => r.PointId == "npc_roster")
            .Select(r => (LocalSegments: r.LocalName.Split('_'), r.LocalName, r.Symbol))
            .OrderByDescending(r => r.LocalSegments.Length)
            .ThenByDescending(r => r.LocalName.Length)
            .ToList();
        if (map.Count == 0) return;

        RewriteFiddle(mod, map, generated, hidden);
        RewriteT2(mod, map, generated);
        RenameArt(mod, map, redirects, hidden);
    }

    private static void RewriteFiddle(IMod mod,
        List<(string[] LocalSegments, string LocalName, string Symbol)> map,
        Dictionary<string, string> generated, HashSet<string> hidden)
    {
        foreach (var (_, local, symbol) in map)
        {
            var localPath  = $"fiddle/npcs/{local}.toml";
            var symbolPath = $"fiddle/npcs/{symbol}.toml";
            if (!mod.FileExists(localPath)) continue;
            if (mod.FileExists(symbolPath)) continue; // full-symbol form wins

            generated[symbolPath] = RewriteTokens(mod.ReadFile(localPath), map);
            hidden.Add(localPath);
        }
    }

    private static void RewriteT2(IMod mod,
        List<(string[] LocalSegments, string LocalName, string Symbol)> map,
        Dictionary<string, string> generated)
    {
        foreach (var path in mod.GetAllFiles(".toml"))
        {
            var rel = RelativePath(mod, path);
            if (!rel.StartsWith("t2/", StringComparison.Ordinal)) continue;

            var text = mod.ReadFile(path);
            var rewritten = text;
            foreach (var (_, local, symbol) in map)
            {
                var escaped = Regex.Escape(local);
                // [luna."6:00am"] and [[luna....]] table headers
                rewritten = Regex.Replace(rewritten,
                    $@"(?m)^(\s*\[\[?\s*){escaped}(?=[.\]])", $"${{1}}{symbol}");
                // luna."6:00am" = ... dotted-assignment keys (the basement form)
                rewritten = Regex.Replace(rewritten,
                    $@"(?m)^(\s*){escaped}(?=\."")", $"${{1}}{symbol}");
                // npc = "luna" condition values
                rewritten = Regex.Replace(rewritten,
                    $@"(npc\s*=\s*""){escaped}("")", $"${{1}}{symbol}${{2}}");
            }

            if (!ReferenceEquals(rewritten, text) && rewritten != text)
                generated[rel] = rewritten;
        }
    }

    private static void RenameArt(IMod mod,
        List<(string[] LocalSegments, string LocalName, string Symbol)> map,
        Dictionary<string, string> redirects, HashSet<string> hidden)
    {
        foreach (var extension in new[] { ".png", ".meta.toml" })
        {
            foreach (var path in mod.GetAllFiles(extension))
            {
                var rel = RelativePath(mod, path);
                if (!rel.StartsWith("animations/", StringComparison.Ordinal)
                    && !rel.StartsWith("shapes/", StringComparison.Ordinal)) continue;

                var slash = rel.LastIndexOf('/');
                var dir = rel[..(slash + 1)];
                var file = rel[(slash + 1)..];
                var renamed = RewriteTokens(file, map);
                if (renamed == file) continue;

                redirects[dir + renamed] = rel;
                hidden.Add(rel);
            }
        }
    }

    // Every lowercase identifier run in the text, segment-run rewritten.
    // Prose is safe because an uppercase letter breaks the run, so "Luna" never
    // contains a matchable `luna` token.
    public static string RewriteTokens(string text,
        List<(string[] LocalSegments, string LocalName, string Symbol)> map)
    {
        return Regex.Replace(text, "[a-z0-9_]+", match => RewriteIdentifier(match.Value, map));
    }

    // A run already spelling a full symbol is consumed whole before local
    // matching, which keeps the pass idempotent.
    public static string RewriteIdentifier(string token,
        List<(string[] LocalSegments, string LocalName, string Symbol)> map)
    {
        if (!token.Contains('_') && map.All(m => m.LocalName != token)) return token;

        var segments = token.Split('_');
        List<string> output = [];
        var i = 0;
        while (i < segments.Length)
        {
            var matched = false;
            foreach (var (_, _, symbol) in map)
            {
                var symbolSegments = symbol.Split('_');
                if (!MatchesAt(segments, i, symbolSegments)) continue;
                output.Add(symbol);
                i += symbolSegments.Length;
                matched = true;
                break;
            }

            if (!matched)
            {
                foreach (var (localSegments, _, symbol) in map)
                {
                    if (!MatchesAt(segments, i, localSegments)) continue;
                    output.Add(symbol);
                    i += localSegments.Length;
                    matched = true;
                    break;
                }
            }

            if (matched) continue;
            output.Add(segments[i]);
            i++;
        }

        return string.Join('_', output);
    }

    private static bool MatchesAt(string[] segments, int at, string[] wanted)
    {
        if (at + wanted.Length > segments.Length) return false;
        for (var j = 0; j < wanted.Length; j++)
            if (segments[at + j] != wanted[j]) return false;
        return true;
    }

    private static string RelativePath(IMod mod, string path)
    {
        var normalized = path.Replace('\\', '/');
        var basePath = mod.GetBasePath().Replace('\\', '/').TrimEnd('/');
        if (basePath.Length > 0 && normalized.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase))
            return normalized[(basePath.Length + 1)..];
        return normalized.TrimStart('/');
    }
}
