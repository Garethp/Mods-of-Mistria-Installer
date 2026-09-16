using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

public sealed record MonsterVanillaIndex(
    IReadOnlyDictionary<string, string> Monsters,
    IReadOnlySet<string> Categories,
    IReadOnlyDictionary<string, MonsterVanillaCategory> CategoryData);

public sealed record MonsterVanillaCategory(
    IReadOnlyList<string> RequiredStates,
    TomlTable Defaults,
    IReadOnlySet<string> Objects);

public static class MonsterVanillaRoster
{
    private const string Prefix = "assets/fiddle/monsters/";

    public static MonsterVanillaIndex Index(IPristineSource pristine)
    {
        SortedDictionary<string, string> index = new(StringComparer.Ordinal);
        HashSet<string> categories = new(StringComparer.Ordinal);
        SortedDictionary<string, MonsterVanillaCategory> categoryData = new(StringComparer.Ordinal);
        foreach (var path in pristine.Entries(Prefix, ".toml"))
        {
            var category = path[Prefix.Length..^".toml".Length];
            categories.Add(category);
            var bytes = pristine.Read(path)
                        ?? throw new InvalidOperationException($"pristine monster table disappeared: {path}");
            TomlTable table;
            try { table = TomlSerializer.Deserialize<TomlTable>(Encoding.UTF8.GetString(bytes)) ?? new TomlTable(); }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"cannot read pristine monster table {path}: {exception.Message}",
                    exception);
            }
            categoryData[category] = DescribeCategory(table);

            foreach (var key in table.Keys.Where(key => key != "default"))
            {
                if (index.TryGetValue(key, out var previous) && previous != category)
                    throw new InvalidOperationException(
                        $"pristine MonsterId '{key}' appears in categories '{previous}' and '{category}'");
                index[key] = category;
            }
        }
        if (index.Count == 0)
            throw new InvalidOperationException("pristine archive has no monster keys");
        return new MonsterVanillaIndex(index, categories, categoryData);
    }

    private static MonsterVanillaCategory DescribeCategory(TomlTable table)
    {
        var defaults = table.TryGetValue("default", out var defaultValue)
                       && defaultValue is TomlTable defaultTable
            ? defaultTable
            : new TomlTable();
        var stateSets = table
            .Where(pair => pair.Key != "default" && pair.Value is TomlTable)
            .Select(pair => StateNames(defaults, (TomlTable)pair.Value))
            .ToList();
        var states = stateSets.Count == 0
            ? []
            : stateSets.Skip(1).Aggregate(
                new HashSet<string>(stateSets[0], StringComparer.Ordinal),
                (common, next) =>
                {
                    common.IntersectWith(next);
                    return common;
                }).Order(StringComparer.Ordinal).ToList();
        var objects = table.Where(pair => pair.Key != "default")
            .Select(pair => pair.Value)
            .OfType<TomlTable>()
            .Select(entry => entry.TryGetValue("gm_object", out var objectValue)
                ? objectValue as string
                : defaults.TryGetValue("gm_object", out var defaultObject)
                    ? defaultObject as string
                    : null)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        return new MonsterVanillaCategory(states, defaults, objects);
    }

    private static HashSet<string> StateNames(TomlTable defaults, TomlTable entry)
    {
        var sprites = entry.TryGetValue("sprites", out var entrySprites)
                      && entrySprites is TomlTable entrySpriteTable
            ? entrySpriteTable
            : defaults.TryGetValue("sprites", out var defaultSprites)
              && defaultSprites is TomlTable defaultSpriteTable
                ? defaultSpriteTable
                : null;
        return sprites?.Keys.Where(key => key != "misc").ToHashSet(StringComparer.Ordinal) ?? [];
    }
}
