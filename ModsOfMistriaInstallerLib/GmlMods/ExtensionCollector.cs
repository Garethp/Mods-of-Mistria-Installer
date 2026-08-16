using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// One mod's registrations, rejection reasons, and advisory findings.
// Problems are mod-level. Any one excludes the whole mod, content included.
public record ExtensionCollection(
    IReadOnlyList<ExtensionRegistration> Registrations,
    IReadOnlyList<string> Problems,
    IReadOnlyList<LintFinding> Findings);

// Collects momi/extensions/<point_id>/<local_name>.toml, mirroring the
// content installers' folder convention. A registration is data. It carries typed field
// values and nothing else, no anchors, no templates, no engine text
// (SEAMS.md rule 8), and this collector enforces that on the way in.
public static class ExtensionCollector
{
    public const string ExtensionsFolder = "momi/extensions";

    // Keeps a registrant symbol short enough to stay readable as an enum
    // member once prefixed, and plain enough to be one.
    private static readonly Regex LocalNameRegex = new(@"\A[a-z][a-z0-9_]{0,40}\z");

    // Rejects prefixes that would compose unrecoverable or invalid symbols,
    // at declaration. The composed symbol is also held to
    // ExtensionSymbols.Shape, which caps the total length.
    private static readonly Regex PrefixRegex = new(@"\A[a-z][a-z0-9_]*\z");

    // Does this mod ship any registration at all? A cheap check, so an install
    // with no extension content anywhere never loads the catalog on their
    // account or enters the GML path it would otherwise skip.
    public static bool HasRegistrations(IMod mod) => mod.GetAllFiles(".toml")
        .Select(path => RelativePath(mod, path))
        .Any(rel => rel.StartsWith($"{ExtensionsFolder}/", StringComparison.Ordinal));

    public static ExtensionCollection Collect(IMod mod, SeamCatalog catalog)
    {
        List<ExtensionRegistration> registrations = [];
        List<string> problems = [];
        List<LintFinding> findings = [];

        var modId = mod.GetId();
        var modSymbol = GmlModCode.SymbolFor(modId);
        var byPoint = catalog.Extensions.ToDictionary(p => p.Id, p => p);
        HashSet<string> seenSymbols = [];

        // GetAllFiles, not GetFilesInFolder. The latter is a single directory
        // listing on every container, and registrations live one level down
        // under their point id. GmlModCollector reads the gml/ tree the same
        // way, so all three container types behave identically here too.
        var files = mod.GetAllFiles(".toml")
            .Select(path => RelativePath(mod, path))
            .Where(rel => rel.StartsWith($"{ExtensionsFolder}/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        // A prefix outside the symbol alphabet poisons every registration the
        // same way, so it is one mod-level problem, not one per file. The
        // check only runs for mods that actually register, and the message
        // names the derivation so the author knows what to rename.
        if (files.Count > 0 && !PrefixRegex.IsMatch(modSymbol))
        {
            problems.Add($"mod id '{modId}' derives extension symbol prefix '{modSymbol}', which must "
                         + "start with a lowercase letter and contain only lowercase letters, digits, "
                         + "and underscores - the prefix comes from the manifest's author and name, so "
                         + "adjust those to start with a letter");
            return new ExtensionCollection([], problems, findings);
        }

        foreach (var rel in files)
        {

            // momi/extensions/<point_id>/<local_name>.toml carries exactly one level
            // of nesting, so the point id is unambiguous
            var parts = rel.Split('/');
            if (parts.Length != 4)
            {
                problems.Add($"registration '{rel}' is not at {ExtensionsFolder}/<point>/<name>.toml");
                continue;
            }

            var pointId = parts[2];
            var localName = Path.GetFileNameWithoutExtension(parts[3]);

            if (!byPoint.TryGetValue(pointId, out var point))
            {
                // a newer installer may know this point. An older one never will
                problems.Add($"registration '{rel}' targets unknown extension point '{pointId}' - "
                             + "this installer does not provide it, so the mod needs a newer MOMI");
                continue;
            }

            if (!LocalNameRegex.IsMatch(localName))
            {
                problems.Add($"registration '{rel}' name '{localName}' must match "
                             + "^[a-z][a-z0-9_]{0,40}$ - it becomes part of a GML enum member name");
                continue;
            }

            var registration = Parse(mod, rel, point, modId, modSymbol, localName, problems);
            if (registration is null) continue;

            // The prefix and local name are each in shape by the checks
            // above, so the only way the composed symbol can fail the shared
            // shape is total length. Over-length symbols would stamp saves
            // the reseed harvesters cap out of recovering.
            if (!ExtensionSymbols.Shape.IsMatch(registration.Symbol))
            {
                problems.Add($"registration '{rel}' resolves to symbol '{registration.Symbol}' "
                             + $"({registration.Symbol.Length} chars), which is over the 81-char "
                             + "symbol limit - shorten the registration file name, or the mod's "
                             + "author or name");
                continue;
            }

            // guards same-mod duplicates. Cross-mod duplicates are possible
            // too (differently split prefixes can compose the same symbol)
            // and the expander fails those closed with a mod-naming problem
            if (!seenSymbols.Add(registration.Symbol))
            {
                problems.Add($"registration '{rel}' resolves to symbol '{registration.Symbol}', "
                             + "which another registration in this mod already claims");
                continue;
            }

            CheckCompanions(mod, point, registration, rel, problems, findings);
            CheckNpcRosterOutfits(mod, point, registration, rel, findings);
            registrations.Add(registration);
        }

        CheckObjectCreation(mod, registrations, findings);

        return new ExtensionCollection(
            registrations.OrderBy(r => r.Symbol, StringComparer.Ordinal).ToList(),
            problems,
            findings);
    }

    // A registration's `object` should appear in an object_create call in
    // the mod's own gml. Creation can be indirect, so absence is a warning,
    // never an error.
    private static void CheckObjectCreation(IMod mod,
        List<ExtensionRegistration> registrations, List<LintFinding> findings)
    {
        var wanted = registrations
            .Where(r => r.RenderedValues.ContainsKey("object"))
            .Select(r => (Registration: r, Value: r.RenderedValues["object"]))
            .Where(x => x.Value.Length > 0)
            .ToList();
        if (wanted.Count == 0) return;

        HashSet<string> created = [];
        foreach (var path in mod.GetAllFiles(".gml"))
        {
            var rel = RelativePath(mod, path);
            if (!rel.StartsWith("gml/", StringComparison.Ordinal)) continue;

            string text;
            using (var stream = mod.ReadFileAsStream(rel))
            using (var reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }

            foreach (var site in GmlScanner.FindCalls(text, "object_create"))
            {
                var arg = Regex.Match(text[site.NameEnd..], @"^\s*\(\s*""([A-Za-z0-9_]+)""");
                if (arg.Success) created.Add(arg.Groups[1].Value);
            }
        }

        foreach (var (registration, value) in wanted)
        {
            if (created.Contains(value)) continue;

            findings.Add(new LintFinding(registration.ModId, "", 0,
                $"registration '{registration.PointId}/{registration.LocalName}' names object "
                + $"'{value}' but no object_create(\"{value}\", ...) appears in this mod's gml - "
                + "fine when creation is indirect, fatal at first spawn when the object is "
                + "never registered at all"));
        }
    }

    // The engine's default outfit selector falls back to "spring", so a
    // prototype without it resolves a missing wardrobe key. Warning rather
    // than error, since a mod may ship its own selector through the
    // wardrobe seam.
    private static void CheckNpcRosterOutfits(IMod mod, ExtensionPoint point,
        ExtensionRegistration registration, string rel, List<LintFinding> findings)
    {
        if (point.Id != "npc_roster") return;

        // Either spelling works, because the install-time pass renames the local form to the
        // symbol form, so an author may have shipped fiddle/npcs/<local>.toml.
        var fiddle = $"fiddle/npcs/{registration.Symbol}.toml";
        if (!mod.FileExists(fiddle)) fiddle = $"fiddle/npcs/{registration.LocalName}.toml";
        if (!mod.FileExists(fiddle)) return; // the error-level companion already reports absence

        TomlTable table;
        try
        {
            using var stream = mod.ReadFileAsStream(fiddle);
            using var reader = new StreamReader(stream);
            table = Toml.ParseDocument(reader.ReadToEnd());
        }
        catch (Exception)
        {
            return; // malformed toml is the content pipeline's diagnostic to make
        }

        var hasSpring = table.TryGetValue("outfits", out var outfits)
                        && outfits is TomlArray arr
                        && arr.OfType<string>().Contains("spring");
        if (hasSpring) return;

        findings.Add(new LintFinding(registration.ModId, rel, 0,
            $"companion '{fiddle}' declares no \"spring\" outfit - the engine's default "
            + "outfit selector falls back to the literal \"spring\" for every season, so "
            + "this NPC would resolve a wardrobe key that does not exist (fine only when "
            + "the mod ships its own outfit selector)"));
    }

    // A letter names its sender in an `npc` value the mailbox resolves by
    // enum member name, and an unresolved sender falls back to a generic
    // icon at render. Advisory only, because resolution is an install-wide
    // question and this check sees one install's registrations.
    public static void CheckLetterSenders(IMod mod, IReadOnlySet<string> validSenders,
        List<LintFinding> findings)
    {
        const string lettersRel = "fiddle/letters.toml";
        if (!mod.FileExists(lettersRel)) return;

        string text;
        TomlTable table;
        try
        {
            using var stream = mod.ReadFileAsStream(lettersRel);
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
            table = Toml.ParseDocument(text);
        }
        catch (Exception)
        {
            return; // malformed toml is the content pipeline's diagnostic to make
        }

        foreach (var key in table.Keys.Order(StringComparer.Ordinal))
        {
            if (key == "default") continue;
            if (table[key] is not TomlTable letter) continue;
            if (!letter.TryGetValue("npc", out var raw) || raw is not string sender) continue;
            if (validSenders.Contains(sender)) continue;

            findings.Add(new LintFinding(mod.GetId(), lettersRel, LineOf(text, key),
                $"letter '{key}' names sender '{sender}', which is neither a vanilla NPC "
                + "nor a custom NPC this install registers - the mailbox will show a "
                + "fallback icon for it"));
        }
    }

    // The 1-based line of the letter's [key] header, for the finding.
    private static int LineOf(string text, string key)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].TrimStart().StartsWith($"[{key}]", StringComparison.Ordinal))
                return i + 1;
        return 0;
    }

    // The vanilla sender names, read from the npc_roster point's pristine
    // enum in the native form letters use. Null when the point or its file
    // is unavailable, which skips the letters advisory rather than
    // mis-reporting.
    public static HashSet<string>? NpcNativeNames(SeamCatalog catalog, IPristineSource pristine)
    {
        var point = catalog.Extensions.FirstOrDefault(p => p.Id == "npc_roster");
        if (point is null) return null;

        try
        {
            var raw = pristine.Read(point.File);
            if (raw is null) return null;

            List<SeamProblem> problems = [];
            var scan = ExtensionExpander.ScanOrdinalEnum(point,
                StagingText.Norm(StagingText.Decode(raw)), problems);
            if (scan is null) return null;

            return scan.Members
                .Where(m => m.Name != point.OrdinalSentinel)
                .Select(m => ExtensionSymbols.ToNativeName(m.Name))
                .ToHashSet(StringComparer.Ordinal);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The files a registration must ship alongside itself. A registration
    // wires up identity. The mod supplies the data behind it. For npc_roster
    // the missing-data case is not a degraded NPC but a crash during Setup -
    // the roster array is built for every ordinal and the prototype lookup
    // resolves by name, so its companion is declared `error` and excluding the
    // mod is strictly better than shipping a game that will not boot.
    private static void CheckCompanions(IMod mod, ExtensionPoint point,
        ExtensionRegistration registration, string rel, List<string> problems, List<LintFinding> findings)
    {
        if (point.Companions.Count == 0) return;

        Dictionary<string, string> values = new()
        {
            [ExtensionPlaceholders.Symbol] = registration.Symbol,
            // the ordinal is not known until staging, so a companion path that
            // uses it would not be checkable here. The loader permits it for
            // symmetry with vacancy_files. Nothing declares one today.
            [ExtensionPlaceholders.Ordinal] = "",
        };

        // The install-time local-name pass renames fiddle/npcs/<local>.toml to
        // the <symbol> form, so a companion the author named with the local
        // name satisfies the requirement too. Accept either spelling here.
        Dictionary<string, string> localValues = new(values)
        {
            [ExtensionPlaceholders.Symbol] = registration.LocalName,
        };

        foreach (var companion in point.Companions)
        {
            var wanted = ExtensionPlaceholders.Render(companion.Path, values);
            if (mod.FileExists(wanted)) continue;

            var localForm = ExtensionPlaceholders.Render(companion.Path, localValues);
            if (localForm != wanted && mod.FileExists(localForm)) continue;

            var message = $"registration '{rel}' is missing its companion file '{wanted}': "
                          + companion.Doc;
            if (companion.Level == ExtensionCompanionLevel.Error) problems.Add(message);
            else findings.Add(new LintFinding(registration.ModId, rel, 0, message));
        }
    }

    private static ExtensionRegistration? Parse(IMod mod, string rel, ExtensionPoint point,
        string modId, string modSymbol, string localName, List<string> problems)
    {
        TomlTable table;
        try
        {
            using var stream = mod.ReadFileAsStream(rel);
            using var reader = new StreamReader(stream);
            table = Toml.ParseDocument(reader.ReadToEnd());
        }
        catch (Exception exception)
        {
            problems.Add($"registration '{rel}' is not readable TOML: {exception.Message}");
            return null;
        }

        var ok = true;
        Dictionary<string, string> rendered = [];
        foreach (var field in point.Fields)
        {
            table.TryGetValue(field.Name, out var raw);
            var value = ExtensionFieldRenderer.Render(field, raw, out var problem);
            if (value is null)
            {
                problems.Add($"registration '{rel}': {problem}");
                ok = false;
                continue;
            }

            rendered[field.Name] = value;
        }

        // An unknown key is a mistake worth naming. Silently ignoring it means
        // a typo'd field reads as a missing one, and the registrant installs
        // with a default it never asked for.
        var declared = point.Fields.Select(f => f.Name).ToHashSet();
        foreach (var key in table.Keys.Where(k => !declared.Contains(k)).Order(StringComparer.Ordinal))
        {
            problems.Add($"registration '{rel}' sets unknown field '{key}' - point "
                         + $"'{point.Id}' declares {FieldList(point)}");
            ok = false;
        }

        if (!ok) return null;

        return new ExtensionRegistration(
            PointId: point.Id,
            Symbol: $"{modSymbol}_{localName}",
            LocalName: localName,
            ModId: modId,
            RenderedValues: rendered);
    }

    private static string FieldList(ExtensionPoint point) => point.Fields.Count == 0
        ? "none"
        : string.Join(", ", point.Fields.Select(f => $"{f.Name} ({f.Type.CatalogName()})"));

    private static string RelativePath(IMod mod, string absolutePath)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');
        if (normalizedBase.Length > 1
            && normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}
