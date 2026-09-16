using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Collector;

internal sealed record MonsterPrototypeValidation(
    string? ObjectName,
    IReadOnlySet<string> ReferencedSprites,
    IReadOnlyList<string> Problems);

// The game applies a category's default table before the monster's own table.
// Validate that merged table because it is what the prototype builder reads.
internal static partial class MonsterPrototypeValidator
{
    private static readonly string[] NumericFields =
    [
        "hp", "damage", "essence", "iframes", "damage_number_offset",
        "patience_acknowledgement_reset", "aggro_radius",
        "status_effect_offset", "fire_effect_offset",
    ];

    private static readonly string[] RewardFields =
    [
        "item", "quest", "purse", "recipe_scroll", "crafting_scroll",
        "cosmetic", "animal_cosmetic", "pet_cosmetic",
    ];

    public static MonsterPrototypeValidation Validate(IMod mod, string file, string key,
        TomlTable category, IReadOnlyList<string> states, IReadOnlySet<string>? pristineAssets,
        TomlTable? effectiveDefaults = null)
    {
        List<string> problems = [];
        HashSet<string> sprites = new(StringComparer.Ordinal);
        var packagedSpritePairs = PackagedSpritePairs(mod);
        var packagedSprites = packagedSpritePairs.Keys.ToHashSet(StringComparer.Ordinal);
        HashSet<string> duplicateSpritesReported = new(StringComparer.Ordinal);

        TomlTable? localDefaults = null;
        if (category.TryGetValue("default", out var defaultValue))
        {
            if (defaultValue is TomlTable defaultTable) localDefaults = defaultTable;
            else Add("[default] must be a table");
        }
        if (!category.TryGetValue(key, out var entryValue) || entryValue is not TomlTable entry)
        {
            Add($"[{key}] must be a table");
            return new MonsterPrototypeValidation(null, sprites, problems);
        }

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var defaults = effectiveDefaults ?? localDefaults;
        if (defaults is not null)
            foreach (var (name, value) in defaults) values[name] = value;
        foreach (var (name, value) in entry) values[name] = value;
        object? Get(string name) => values.GetValueOrDefault(name);

        foreach (var field in NumericFields)
            if (!Number(Get(field))) Add($"[{key}].{field} must be a number after [default] is applied");
        if (Get("use_circle") is { } useCircle && useCircle is not bool)
            Add($"[{key}].use_circle must be a boolean when present");
        if (Get("starting_dir") is not TomlArray starting || !NumberPair(starting))
            Add($"[{key}].starting_dir must be a two-number array");
        if (Get("coin_count") is { } coinCount
            && (coinCount is not TomlArray coins || !NumberPair(coins)))
            Add($"[{key}].coin_count must be a two-number array when present");

        var spritesTable = Get("sprites") as TomlTable;
        if (spritesTable is null)
        {
            Add($"[{key}].sprites must be a table");
        }
        else
        {
            foreach (var state in states)
            {
                if (!spritesTable.TryGetValue(state, out var sprite))
                    Add($"[{key}].sprites.{state} is required by the category");
                else
                    ValidateStateSprite(sprite, $"[{key}].sprites.{state}");
            }

            if (spritesTable.TryGetValue("misc", out var miscValue))
            {
                if (miscValue is not TomlTable misc)
                    Add($"[{key}].sprites.misc must be a table when present");
                else
                    foreach (var (name, value) in misc)
                        ValidateNamedSprite(value, $"[{key}].sprites.misc.{name}");
            }
        }

        if (Get("tango") is not TomlTable tango)
        {
            Add($"[{key}].tango must be a table (use an empty table when no state audio is needed)");
        }
        else
        {
            foreach (var (name, value) in tango)
            {
                if (name == "misc")
                {
                    if (value is not TomlTable misc)
                        Add($"[{key}].tango.misc must be a table when present");
                    else
                        foreach (var (miscName, miscValue) in misc)
                            if (miscValue is not string sound || string.IsNullOrWhiteSpace(sound))
                                Add($"[{key}].tango.misc.{miscName} must be a nonempty audio name");
                    continue;
                }

                if (value is not string stateSound || string.IsNullOrWhiteSpace(stateSound))
                    Add($"[{key}].tango.{name} must be a nonempty audio name");
            }
        }

        var objectName = Get("gm_object") as string;
        if (string.IsNullOrWhiteSpace(objectName))
        {
            Add($"[{key}].gm_object must name the mod's par_monster child");
            objectName = null;
        }

        if (Get("hurtbox") is not string hurtbox || string.IsNullOrWhiteSpace(hurtbox))
            Add($"[{key}].hurtbox must name a sprite");
        else
            ValidateNamedSprite(hurtbox, $"[{key}].hurtbox");

        if (Get("hitbox") is { } hitbox)
        {
            if (hitbox is not string hitboxName || string.IsNullOrWhiteSpace(hitboxName))
                Add($"[{key}].hitbox must name a sprite when present");
            else
                ValidateNamedSprite(hitboxName, $"[{key}].hitbox");
        }

        if (Get("drops") is not TomlArray drops)
        {
            Add($"[{key}].drops must be an array (use [] for no drops)");
        }
        else
        {
            for (var i = 0; i < drops.Count; i++)
            {
                if (drops[i] is not TomlTable drop)
                {
                    Add($"[{key}].drops[{i}] must be a table");
                    continue;
                }
                var rewards = RewardFields.Where(drop.ContainsKey).ToList();
                if (rewards.Count != 1)
                {
                    Add($"[{key}].drops[{i}] must name exactly one supported reward");
                }
                else
                {
                    var reward = rewards[0];
                    var value = drop[reward];
                    if (reward == "purse")
                    {
                        if (!Number(value)) Add($"[{key}].drops[{i}].purse must be a number");
                    }
                    else if (value is not string text || string.IsNullOrWhiteSpace(text))
                    {
                        Add($"[{key}].drops[{i}].{reward} must be a nonempty string");
                    }

                    if (reward == "animal_cosmetic"
                        && (!drop.TryGetValue("animal", out var animal)
                            || animal is not string animalName
                            || string.IsNullOrWhiteSpace(animalName)))
                        Add($"[{key}].drops[{i}].animal must be a nonempty string for an animal cosmetic");
                }
                if (drop.TryGetValue("count", out var count) && !Number(count))
                    Add($"[{key}].drops[{i}].count must be a number when present");
                if (drop.TryGetValue("chance", out var chance) && !Number(chance))
                    Add($"[{key}].drops[{i}].chance must be a number");
                if (drop.TryGetValue("count_range", out var range)
                    && (range is not TomlArray countRange || !NumberPair(countRange)))
                    Add($"[{key}].drops[{i}].count_range must be a two-number array");
                if (drop.TryGetValue("exclusive", out var exclusive) && exclusive is not bool)
                    Add($"[{key}].drops[{i}].exclusive must be a boolean");
                if (drop.TryGetValue("perfect_pick_chance", out var perfect) && !Number(perfect))
                    Add($"[{key}].drops[{i}].perfect_pick_chance must be a number");
            }
        }

        return new MonsterPrototypeValidation(objectName, sprites, problems);

        void Add(string message) => problems.Add(message);

        void ValidateStateSprite(object? value, string field)
        {
            if (value is string spriteName)
            {
                ValidateNamedSprite(spriteName, field);
                return;
            }
            if (value is not TomlTable directions)
            {
                Add($"{field} must name a sprite or provide north and south sprites");
                return;
            }
            if (Value(directions, "north") is not string north)
                Add($"{field}.north must name a sprite");
            else
                ValidateNamedSprite(north, field + ".north");
            if (Value(directions, "south") is not string south)
                Add($"{field}.south must name a sprite");
            else
                ValidateNamedSprite(south, field + ".south");
            if (directions.TryGetValue("east", out var east))
            {
                if (east is not string eastName)
                    Add($"{field}.east must name a sprite when present");
                else
                    ValidateNamedSprite(eastName, field + ".east");
            }
        }

        void ValidateNamedSprite(object? value, string field)
        {
            if (value is not string name || string.IsNullOrWhiteSpace(name))
            {
                Add($"{field} must name a sprite");
                return;
            }
            if (!SpriteShape().IsMatch(name))
            {
                Add($"{field} must name a sprite using the spr_ prefix");
                return;
            }
            sprites.Add(name);

            if (packagedSpritePairs.TryGetValue(name, out var files) && files.Count > 1
                && duplicateSpritesReported.Add(name))
                Add($"sprite '{name}' is staged more than once in this mod: "
                    + string.Join(", ", files));

            if (pristineAssets is null || pristineAssets.Contains(name) || packagedSprites.Contains(name))
                return;
            Add($"{field} references '{name}', but no pristine sprite or this mod's staged image/meta pair provides it");
        }
    }

    public static IReadOnlySet<string> PackagedSpriteNames(IMod mod) =>
        PackagedSpritePairs(mod).Keys.ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> PackagedSpritePairs(IMod mod)
    {
        return mod.GetAllFiles(".meta.toml")
            .Select(path => RelativePath(mod, path))
            .Where(path => path.StartsWith("images/", StringComparison.Ordinal)
                           && !path.StartsWith("images/replace/", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path).StartsWith("spr_", StringComparison.Ordinal)
                           && path.EndsWith(".meta.toml", StringComparison.Ordinal)
                           && mod.FileExists(path[..^".meta.toml".Length] + ".png"))
            .GroupBy(path => Path.GetFileName(path)[..^".meta.toml".Length], StringComparer.Ordinal)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<string>)group.Order(StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);
    }

    private static string RelativePath(IMod mod, string path)
    {
        var root = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? normalized[root.Length..]
            : normalized;
    }

    private static bool Number(object? value) =>
        value is byte or short or int or long or float or double or decimal;

    private static bool NumberPair(TomlArray array) => array.Count == 2 && array.All(Number);

    private static object? Value(TomlTable? table, string key) =>
        table is not null && table.TryGetValue(key, out var value) ? value : null;

    [GeneratedRegex("^spr_[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SpriteShape();
}
