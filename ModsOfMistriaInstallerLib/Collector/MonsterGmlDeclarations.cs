using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.Collector;

internal sealed record MonsterObjectSite(string Name, string? Parent, string File);

internal sealed class MonsterGmlDeclarations
{
    public IReadOnlyList<MonsterObjectSite> Objects { get; private init; } = [];

    public bool HasGml { get; private init; }

    public static MonsterGmlDeclarations Read(IMod mod)
    {
        List<MonsterObjectSite> objects = [];
        var files = mod.GetAllFiles(".gml")
            .Select(path => RelativePath(mod, path))
            .Where(path => path.StartsWith("gml/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var path in files)
        {
            var source = mod.ReadFile(path);
            var tokens = GmlScanner.Tokenize(source);
            foreach (var call in GmlScanner.FindCalls(source, "object_create", tokens)
                         .Where(call => call.Kind == CallKind.Call))
            {
                var at = tokens.FindIndex(token => token.Start == call.NameStart);
                var name = Literal(source, tokens, at + 2);
                if (name is null) continue;

                string? parent = null;
                if (Is(source, tokens, at + 3, ","))
                {
                    parent = Literal(source, tokens, at + 4);
                    if (parent is null && Is(source, tokens, at + 4, "object_reserve")
                                       && Is(source, tokens, at + 5, "("))
                        parent = Literal(source, tokens, at + 6);
                }

                objects.Add(new MonsterObjectSite(name, parent, path));
            }
        }

        return new MonsterGmlDeclarations { Objects = objects, HasGml = files.Count > 0 };
    }

    internal static IReadOnlyList<MonsterProblem> FindObjectConflicts(
        IReadOnlyList<IMod> mods, MonsterCollection monsters)
    {
        var modSet = mods.ToHashSet();
        var definitions = monsters.Definitions
            .Where(definition => definition.DefinesObject && modSet.Contains(definition.Mod))
            .ToList();
        if (definitions.Count == 0) return [];

        var order = mods.Select((mod, index) => (mod, index))
            .ToDictionary(pair => pair.mod, pair => pair.index);
        var gml = mods.ToDictionary(mod => mod, Read);
        List<MonsterProblem> problems = [];

        foreach (var definition in definitions)
        {
            var claims = gml
                .Where(pair => pair.Key != definition.Mod)
                .SelectMany(pair => pair.Value.Objects
                    .Where(site => site.Name == definition.ObjectName)
                    .Select(site => (Mod: pair.Key, Site: site)))
                .OrderBy(claim => order[claim.Mod])
                .ThenBy(claim => claim.Site.File, StringComparer.Ordinal)
                .ToList();
            if (claims.Count == 0) continue;

            problems.Add(new MonsterProblem(definition.Mod, definition.File,
                $"custom monster '{definition.Key}' object '{definition.ObjectName}' is also created by "
                + string.Join(", ", claims.Select(claim => $"{claim.Mod.GetId()} ({claim.Site.File})"))));
            problems.AddRange(claims.Select(claim => new MonsterProblem(claim.Mod, claim.Site.File,
                $"object '{definition.ObjectName}' is claimed by custom monster "
                + $"'{definition.Key}' from mod '{definition.Mod.GetId()}' ({definition.File})")));
        }

        return problems.Distinct()
            .OrderBy(problem => order[problem.Mod])
            .ThenBy(problem => problem.File, StringComparer.Ordinal)
            .ThenBy(problem => problem.Message, StringComparer.Ordinal)
            .ToList();
    }

    private static string? Literal(string source, IReadOnlyList<GmlToken> tokens, int at)
    {
        if (at < 0 || at >= tokens.Count) return null;
        var text = source[tokens[at].Start..tokens[at].End];
        return text.Length >= 2 && text[0] == '"' && text[^1] == '"'
            ? text[1..^1] : null;
    }

    private static bool Is(string source, IReadOnlyList<GmlToken> tokens, int at, string value) =>
        at >= 0 && at < tokens.Count && source[tokens[at].Start..tokens[at].End] == value;

    private static string RelativePath(IMod mod, string path)
    {
        var root = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? normalized[root.Length..]
            : normalized;
    }
}
