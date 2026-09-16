using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Collector;

public sealed record MonsterProblem(IMod Mod, string File, string Message);

public sealed record MonsterCategoryDefinition(
    IMod Mod, string Key, string File, IReadOnlyList<string> States);

public sealed record MonsterDefinition(
    IMod Mod,
    string Key,
    string Category,
    string ObjectName,
    string File,
    IReadOnlySet<string> ReferencedSprites,
    bool DefinesObject = true);

public sealed record MonsterVanillaPatch(IMod Mod, string Key, string Category, string File);

public sealed record MonsterCollection(
    IReadOnlyList<MonsterDefinition> Definitions,
    IReadOnlyList<MonsterCategoryDefinition> Categories,
    IReadOnlyList<MonsterProblem> Problems,
    IReadOnlyList<LintFinding> Findings,
    IReadOnlyList<string> PristineKeys,
    IReadOnlyList<MonsterVanillaPatch> VanillaPatches)
{
    public static MonsterCollection Empty { get; } = new([], [], [], [], [], []);

    public int VanillaPatchCount => VanillaPatches.Count;

    public MonsterCollection ForMods(IReadOnlySet<IMod> mods)
    {
        var ids = mods.Select(mod => mod.GetId()).ToHashSet(StringComparer.Ordinal);
        return new MonsterCollection(
            Definitions.Where(definition => mods.Contains(definition.Mod)).ToList(),
            Categories.Where(category => mods.Contains(category.Mod)).ToList(),
            Problems.Where(problem => mods.Contains(problem.Mod)).ToList(),
            Findings.Where(finding => ids.Contains(finding.ModId)).ToList(),
            PristineKeys,
            VanillaPatches.Where(patch => mods.Contains(patch.Mod)).ToList());
    }

    internal MonsterCollection WithoutMods(IReadOnlySet<IMod> mods)
    {
        var ids = mods.Select(mod => mod.GetId()).ToHashSet(StringComparer.Ordinal);
        return new MonsterCollection(
            Definitions.Where(definition => !mods.Contains(definition.Mod)).ToList(),
            Categories.Where(category => !mods.Contains(category.Mod)).ToList(),
            Problems.Where(problem => !mods.Contains(problem.Mod)).ToList(),
            Findings.Where(finding => !ids.Contains(finding.ModId)).ToList(),
            PristineKeys,
            VanillaPatches.Where(patch => !mods.Contains(patch.Mod)).ToList());
    }
}

public static partial class MonsterDefinitionCollector
{
    private const string MonsterRoot = "fiddle/monsters/";
    private const string CategoryRoot = "momi/monster_categories/";

    public static bool HasMonsterContent(IMod mod) => mod.GetAllFiles(".toml")
        .Select(path => RelativePath(mod, path))
        .Any(path => path.StartsWith(MonsterRoot, StringComparison.Ordinal)
                     || path.StartsWith(CategoryRoot, StringComparison.Ordinal));

    public static MonsterCollection Collect(IReadOnlyList<IMod> mods, IPristineSource pristine)
    {
        var participants = mods.Where(HasMonsterContent).ToList();
        if (participants.Count == 0) return MonsterCollection.Empty;

        var order = mods.Select((mod, index) => (mod, index))
            .ToDictionary(pair => pair.mod, pair => pair.index);
        var orderById = mods.Select((mod, index) => (Id: mod.GetId(), Index: index))
            .GroupBy(pair => pair.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Min(pair => pair.Index),
                StringComparer.Ordinal);
        List<MonsterProblem> problems = [];
        List<LintFinding> findings = [];
        List<MonsterCategoryDefinition> categories = [];
        List<MonsterDefinition> definitions = [];
        List<(IMod Mod, string Key, string File)> categorySites = [];
        List<(IMod Mod, string Key, string Category, string File)> customSites = [];
        List<MonsterVanillaPatch> vanillaPatches = [];
        var gml = mods.ToDictionary(mod => mod, MonsterGmlDeclarations.Read);

        IReadOnlyDictionary<string, string> vanilla;
        IReadOnlySet<string> pristineCategories;
        IReadOnlyDictionary<string, MonsterVanillaCategory> pristineCategoryData;
        IReadOnlyList<string> pristineKeys = [];
        try
        {
            var vanillaIndex = MonsterVanillaRoster.Index(pristine);
            vanilla = vanillaIndex.Monsters;
            pristineCategories = vanillaIndex.Categories;
            pristineCategoryData = vanillaIndex.CategoryData;
            pristineKeys = vanilla.Keys.Order(StringComparer.Ordinal).ToList();
        }
        catch (Exception exception)
        {
            foreach (var mod in participants)
                Problem(mod, "fiddle/monsters",
                    "cannot read the game's MonsterId categories: " + exception.Message);
            return Finish();
        }

        var pristineAssets = pristine.AssetNames();
        var packagedSprites = mods.ToDictionary(mod => mod, MonsterPrototypeValidator.PackagedSpriteNames);

        foreach (var mod in participants)
            CollectCategories(mod);

        foreach (var group in categorySites.GroupBy(category => category.Key, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            var sites = group.ToList();
            for (var i = 0; i < sites.Count; i++)
            {
                var category = sites[i];
                var others = string.Join(", ", sites.Where((_, index) => index != i)
                    .Select(other => $"{other.Mod.GetId()} ({other.File})"));
                Problem(category.Mod, category.File,
                    $"custom monster category '{category.Key}' is also declared by {others}");
            }
        }

        var uniqueCategoryKeys = categorySites
            .GroupBy(site => site.Key, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var uniqueCategories = categories
            .Where(category => uniqueCategoryKeys.Contains(category.Key))
            .ToDictionary(category => category.Key, category => category, StringComparer.Ordinal);

        var monsterFiles = participants
            .SelectMany(mod => MonsterFiles(mod).Select(file => (Mod: mod, file.Category, file.File)))
            .ToList();

        List<(IMod Mod, string Category, string File, TomlTable Table)> parsedMonsterFiles = [];
        foreach (var (mod, category, file) in monsterFiles)
        {
            if (uniqueCategories.TryGetValue(category, out var declared) && declared.Mod != mod)
            {
                Problem(mod, file,
                    $"monster category '{category}' belongs to mod '{declared.Mod.GetId()}'; "
                    + "custom categories are private to their declaring mod");
                continue;
            }

            TomlTable table;
            try
            {
                table = TomlSerializer.Deserialize<TomlTable>(mod.ReadFile(file)) ?? new TomlTable();
            }
            catch (Exception exception)
            {
                Problem(mod, file, $"cannot read monster Fiddle TOML: {exception.Message}");
                continue;
            }
            parsedMonsterFiles.Add((mod, category, file, table));
        }

        var builtInDefaults = pristineCategoryData.ToDictionary(pair => pair.Key, pair =>
        {
            var defaults = new TomlTable();
            MOMIOperations.MergeTomlTables(defaults, pair.Value.Defaults);
            return defaults;
        }, StringComparer.Ordinal);
        foreach (var parsed in parsedMonsterFiles)
            if (builtInDefaults.TryGetValue(parsed.Category, out var defaults)
                && parsed.Table.TryGetValue("default", out var defaultValue)
                && defaultValue is TomlTable defaultPatch)
                MOMIOperations.MergeTomlTables(defaults, defaultPatch);

        foreach (var (mod, category, file, table) in parsedMonsterFiles)
        {
            foreach (var key in table.Keys.Where(key => key != "default").Order(StringComparer.Ordinal))
            {
                if (vanilla.TryGetValue(key, out var vanillaCategory))
                {
                    if (vanillaCategory == category)
                    {
                        vanillaPatches.Add(new MonsterVanillaPatch(mod, key, category, file));
                    }
                    else
                    {
                        Problem(mod, file,
                            $"monster key '{key}' belongs to built-in category '{vanillaCategory}', "
                            + $"not '{category}'");
                    }
                    continue;
                }

                if (!NameShape().IsMatch(key) || key == "default")
                {
                    Problem(mod, file,
                        $"custom monster key '{key}' must use lower snake case and cannot be 'default'");
                    continue;
                }
                customSites.Add((mod, key, category, file));

                MonsterVanillaCategory? builtInCategory = null;
                IReadOnlyList<string> states;
                if (pristineCategoryData.TryGetValue(category, out builtInCategory))
                {
                    states = builtInCategory.RequiredStates;
                    if (states.Count == 0)
                    {
                        Problem(mod, file,
                            $"cannot determine the sprite states for built-in monster category '{category}'");
                        continue;
                    }
                }
                else if (uniqueCategories.TryGetValue(category, out var customCategory)
                         && customCategory.Mod == mod)
                {
                    states = customCategory.States;
                }
                else
                {
                    Problem(mod, file,
                        $"custom monster '{key}' has no matching {CategoryRoot}{category}.toml "
                        + "declaration in this mod");
                    continue;
                }

                var validation = MonsterPrototypeValidator.Validate(
                    mod, file, key, table, states, pristineAssets,
                    builtInCategory is null ? null : builtInDefaults[category]);
                foreach (var message in validation.Problems) Problem(mod, file, message);
                if (validation.ObjectName is null) continue;

                var objectName = validation.ObjectName;
                if (!ObjectShape().IsMatch(objectName))
                    Problem(mod, file,
                        $"custom monster '{key}' object '{objectName}' must use the obj_ prefix and be a GML identifier");
                var definesObject = builtInCategory?.Objects.Contains(objectName) != true;
                if (definesObject && pristineAssets?.Contains(objectName) == true)
                    Problem(mod, file,
                        $"custom monster '{key}' uses built-in object '{objectName}'; "
                        + "use the category's object or define a different par_monster child");

                if (definesObject) CheckObjectEvidence(mod, file, key, objectName);
                definitions.Add(new MonsterDefinition(mod, key, category, objectName, file,
                    validation.ReferencedSprites, definesObject));
            }
        }

        foreach (var group in customSites.GroupBy(definition => definition.Key, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            var sites = group.ToList();
            for (var i = 0; i < sites.Count; i++)
            {
                var site = sites[i];
                var others = string.Join(", ", sites.Where((_, index) => index != i)
                    .Select(other => $"{other.Mod.GetId()} ({other.File})"));
                Problem(site.Mod, site.File,
                    $"monster key '{site.Key}' is also declared by {others}");
            }
        }

        foreach (var group in definitions.Where(definition => definition.DefinesObject)
                     .GroupBy(definition => definition.ObjectName, StringComparer.Ordinal)
                     .Where(group => group.Select(definition => definition.Mod).Distinct().Count() > 1))
            ReportDefinitionCollision(group, "monster object", definition => definition.ObjectName);

        foreach (var definition in definitions)
        {
            foreach (var sprite in definition.ReferencedSprites
                         .Where(sprite => pristineAssets?.Contains(sprite) != true)
                         .Order(StringComparer.Ordinal))
            {
                var providers = mods.Where(mod => packagedSprites[mod].Contains(sprite)).ToList();
                if (providers.Count == 1 && providers[0] == definition.Mod) continue;
                if (providers.Count == 0) continue; // the prototype validator already names the missing pair

                var message = providers.Count == 1
                    ? $"is supplied by mod '{providers[0].GetId()}', not by the monster's mod"
                    : "has ambiguous providers: "
                      + string.Join(", ", providers.Select(provider => provider.GetId()));
                Problem(definition.Mod, definition.File,
                    $"custom monster '{definition.Key}' sprite '{sprite}' {message}");
            }
        }

        foreach (var category in uniqueCategories.Values)
            if (!definitions.Any(definition => definition.Mod == category.Mod
                                               && definition.Category == category.Key))
                Problem(category.Mod, category.File,
                    $"custom monster category '{category.Key}' has no monster definition in this mod");

        return Finish();

        void CollectCategories(IMod mod)
        {
            foreach (var file in CategoryFiles(mod))
            {
                var rest = file[CategoryRoot.Length..];
                if (rest.Contains('/', StringComparison.Ordinal))
                {
                    Problem(mod, file,
                        $"monster category declaration '{file}' must be at {CategoryRoot}<category>.toml");
                    continue;
                }

                var category = rest[..^".toml".Length];
                if (!NameShape().IsMatch(category) || category == "default")
                {
                    Problem(mod, file,
                        $"custom monster category '{category}' must use lower snake case and cannot be 'default'");
                    continue;
                }
                categorySites.Add((mod, category, file));
                if (pristineCategories.Contains(category))
                {
                    Problem(mod, file,
                        $"custom monster category '{category}' collides with a vanilla category");
                    continue;
                }

                TomlTable table;
                try
                {
                    table = TomlSerializer.Deserialize<TomlTable>(mod.ReadFile(file)) ?? new TomlTable();
                }
                catch (Exception exception)
                {
                    Problem(mod, file, $"cannot read monster category declaration: {exception.Message}");
                    continue;
                }

                var unknown = table.Keys.Where(key => key != "states").Order(StringComparer.Ordinal).ToList();
                foreach (var field in unknown)
                    Problem(mod, file,
                        $"custom monster category '{category}' sets unknown field '{field}'; only 'states' is supported");
                if (!table.TryGetValue("states", out var statesValue) || statesValue is not TomlArray stateArray)
                {
                    Problem(mod, file,
                        $"custom monster category '{category}' must declare a states array");
                    continue;
                }

                var states = stateArray.OfType<string>().ToList();
                if (states.Count != stateArray.Count || states.Count == 0
                    || states.Any(string.IsNullOrWhiteSpace)
                    || states.Distinct(StringComparer.Ordinal).Count() != states.Count)
                {
                    Problem(mod, file,
                        $"custom monster category '{category}' states must be distinct, nonempty strings");
                    continue;
                }
                if (unknown.Count > 0) continue;

                categories.Add(new MonsterCategoryDefinition(mod, category, file, states));
            }
        }

        void CheckObjectEvidence(IMod mod, string file, string key, string objectName)
        {
            var source = gml[mod];
            var sites = source.Objects.Where(site => site.Name == objectName).ToList();
            if (sites.Count == 0)
            {
                if (!source.HasGml)
                    Problem(mod, file,
                        $"custom monster '{key}' names object '{objectName}', but this mod has no gml/ source to define it");
                else
                    Finding(mod, "",
                        $"{file}: custom monster '{key}' names object '{objectName}', but no literal "
                        + $"object_create(\"{objectName}\", ...) appears in this mod's gml; "
                        + "MOMI cannot check objects created indirectly");
                return;
            }

            if (sites.Count > 1)
                Problem(mod, file,
                    $"custom monster '{key}' object '{objectName}' is created more than once in this mod: "
                    + string.Join(", ", sites.Select(site => site.File)));
            foreach (var site in sites)
            {
                if (site.Parent is null)
                    Finding(mod, "",
                        $"{site.File}: monster object '{objectName}' uses a non-literal parent; "
                        + "MOMI cannot verify that it inherits par_monster");
                else if (site.Parent != "par_monster")
                    Problem(mod, site.File,
                        $"monster object '{objectName}' inherits '{site.Parent}', but custom monsters must inherit par_monster");
            }
        }

        void ReportDefinitionCollision(
            IEnumerable<MonsterDefinition> group,
            string label,
            Func<MonsterDefinition, string> value)
        {
            var sites = group.ToList();
            for (var i = 0; i < sites.Count; i++)
            {
                var definition = sites[i];
                var others = string.Join(", ", sites.Where(other => other.Mod != definition.Mod)
                    .Select(other => $"{other.Mod.GetId()} ({other.File})"));
                Problem(definition.Mod, definition.File,
                    $"{label} '{value(definition)}' is also declared by {others}");
            }
        }

        MonsterCollection Finish()
        {
            var distinctProblems = problems.Distinct().OrderBy(problem => order[problem.Mod])
                .ThenBy(problem => problem.File, StringComparer.Ordinal)
                .ThenBy(problem => problem.Message, StringComparer.Ordinal)
                .ToList();
            var rejected = distinctProblems.Select(problem => problem.Mod).ToHashSet();
            return new MonsterCollection(
                definitions.Where(definition => !rejected.Contains(definition.Mod))
                    .OrderBy(definition => definition.Key, StringComparer.Ordinal).ToList(),
                categories.Where(category => !rejected.Contains(category.Mod))
                    .OrderBy(category => category.Key, StringComparer.Ordinal).ToList(),
                distinctProblems,
                findings.Distinct().OrderBy(finding => orderById.GetValueOrDefault(finding.ModId))
                    .ThenBy(finding => finding.File, StringComparer.Ordinal)
                    .ThenBy(finding => finding.Message, StringComparer.Ordinal).ToList(),
                pristineKeys,
                vanillaPatches.Where(patch => !rejected.Contains(patch.Mod))
                    .OrderBy(patch => order[patch.Mod])
                    .ThenBy(patch => patch.File, StringComparer.Ordinal)
                    .ThenBy(patch => patch.Key, StringComparer.Ordinal)
                    .ToList());
        }

        void Problem(IMod mod, string file, string message) =>
            problems.Add(new MonsterProblem(mod, file, message));

        void Finding(IMod mod, string file, string message) =>
            findings.Add(new LintFinding(mod.GetId(), file, 0, message));
    }

    private static IEnumerable<(string Category, string File)> MonsterFiles(IMod mod)
    {
        foreach (var file in mod.GetAllFiles(".toml")
                     .Select(path => RelativePath(mod, path))
                     .Where(path => path.StartsWith(MonsterRoot, StringComparison.Ordinal)
                                    && path.EndsWith(".toml", StringComparison.Ordinal))
                     .Order(StringComparer.Ordinal))
        {
            var category = file[MonsterRoot.Length..^".toml".Length];
            yield return (category, file);
        }
    }

    private static IEnumerable<string> CategoryFiles(IMod mod) => mod.GetAllFiles(".toml")
        .Select(path => RelativePath(mod, path))
        .Where(path => path.StartsWith(CategoryRoot, StringComparison.Ordinal)
                       && path.EndsWith(".toml", StringComparison.Ordinal)
                       && path.Length > CategoryRoot.Length + ".toml".Length)
        .Order(StringComparer.Ordinal);

    private static string RelativePath(IMod mod, string path)
    {
        var root = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? normalized[root.Length..]
            : normalized;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NameShape();

    [GeneratedRegex("^obj_[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ObjectShape();
}
