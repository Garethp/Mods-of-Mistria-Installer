using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The {{placeholder}} vocabulary shared by the loader (which validates that a
// template names nothing else) and the expander (which substitutes). One regex,
// so a template that validates is a template that renders.
public static class ExtensionPlaceholders
{
    // Always supplied by the expander, never declared as a field.
    public const string Symbol = "symbol";

    public const string Ordinal = "ordinal";

    public static readonly Regex Regex = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}");

    // Every distinct placeholder a template names, in first-appearance order.
    public static List<string> Names(string template) =>
        Regex.Matches(template).Select(m => m.Groups[1].Value).Distinct().ToList();

    // Substitute from `values`. Every placeholder is known to be present, because the
    // loader proved the template names only declared fields plus symbol and
    // ordinal, and the expander supplies all of those.
    public static string Render(string template, IReadOnlyDictionary<string, string> values) =>
        Regex.Replace(template, match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value
            : match.Value);
}

// The four value types a registration field may declare. The type decides how
// a registrant's value is escaped into GML, which is why the set is closed:
// `identifier` is the only one that lands as a bare token, and its charset
// forbids anything but a single identifier, so a registration cannot inject a
// statement into catalog-authored text.
public enum ExtensionFieldType
{
    Identifier,
    String,
    Int,
    Bool,
}

// Where a generated line lands. All three are line-oriented. If a
// non-line-oriented need ever appears, generalise enum_member into a
// list_member locator rather than adding a fourth kind.
public enum ExtensionSiteKind
{
    EnumMember,
    Anchor,
    Append,
}

public static class ExtensionEnums
{
    public static string CatalogName(this ExtensionFieldType type) => type switch
    {
        ExtensionFieldType.Identifier => "identifier",
        ExtensionFieldType.String => "string",
        ExtensionFieldType.Int => "int",
        ExtensionFieldType.Bool => "bool",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static string CatalogName(this ExtensionSiteKind kind) => kind switch
    {
        ExtensionSiteKind.EnumMember => "enum_member",
        ExtensionSiteKind.Anchor => "anchor",
        ExtensionSiteKind.Append => "append",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string FieldTypeNames =>
        string.Join(", ", Enum.GetValues<ExtensionFieldType>().Select(t => t.CatalogName()));

    public static string SiteKindNames =>
        string.Join(", ", Enum.GetValues<ExtensionSiteKind>().Select(k => k.CatalogName()));

    public static ExtensionFieldType? ParseFieldType(string text) => Enum
        .GetValues<ExtensionFieldType>()
        .Cast<ExtensionFieldType?>()
        .FirstOrDefault(t => t!.Value.CatalogName() == text);

    public static ExtensionSiteKind? ParseSiteKind(string text) => Enum
        .GetValues<ExtensionSiteKind>()
        .Cast<ExtensionSiteKind?>()
        .FirstOrDefault(k => k!.Value.CatalogName() == text);

    public static string CatalogName(this ExtensionCompanionLevel level) => level switch
    {
        ExtensionCompanionLevel.Error => "error",
        ExtensionCompanionLevel.Warning => "warning",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };

    public static string CompanionLevelNames =>
        string.Join(", ", Enum.GetValues<ExtensionCompanionLevel>().Select(l => l.CatalogName()));

    public static ExtensionCompanionLevel? ParseCompanionLevel(string text) => Enum
        .GetValues<ExtensionCompanionLevel>()
        .Cast<ExtensionCompanionLevel?>()
        .FirstOrDefault(l => l!.Value.CatalogName() == text);
}

// One typed value a registration must supply.
public record ExtensionField(string Name, ExtensionFieldType Type, string Doc);

// One place generated lines land, and the line template rendered there. A site
// renders one line per registrant. A ledger entry whose mod is absent renders
// VacancyTemplate instead (empty = no line for this site).
public record ExtensionSite(
    string Id,
    ExtensionSiteKind Kind,
    string File,             // normalised to "assets/..."; defaults to the point's file
    string Anchor,           // anchor sites only
    string Place,            // anchor sites only: "before" | "after"
    string Template,
    int Indent,
    string VacancyTemplate,
    string Comment = "//");  // marker-comment leader; append sites may say "#" (TOML targets)

// A data file emitted (as an added file) for every ledger vacancy. The
// schema and its validation land ahead of the renderer so the catalog
// format is settled before anything depends on it.
public record ExtensionVacancyFile(string Path, string Content);

// What a failed companion check does. `error` excludes the whole mod (the
// registration TOML wires up identity, and without the data the identity resolves
// to nothing and the game crashes at boot). `warning` reports and installs.
public enum ExtensionCompanionLevel
{
    Error,
    Warning,
}

// A file a registration must ship alongside itself, elsewhere in the mod. The
// registration wires up identity. The mod supplies the data. Existence checks
// only. The two content-shaped lints are not path-expressible and live as
// advisory checks in ExtensionCollector instead.
public record ExtensionCompanion(
    string Path,                       // mod-relative template, {{symbol}}/{{ordinal}}
    ExtensionCompanionLevel Level,
    string Doc);                       // becomes the message a mod author reads

// One [[extension]] stanza, saying where the engine can be extended and what the generated
// code looks like. The catalog is the sole author of that text. A mod ships
// typed field values and nothing else.
public record ExtensionPoint(
    string Id,
    string File,             // the point's default site file, normalised to "assets/..."
    string Doc,
    string OrdinalEnum,      // [extension.ordinal].enum - required
    string OrdinalSentinel,  // [extension.ordinal].sentinel - required
    IReadOnlyList<ExtensionField> Fields,
    IReadOnlyList<ExtensionSite> Sites,
    IReadOnlyList<ExtensionVacancyFile> VacancyFiles,
    IReadOnlyList<ExtensionCompanion> Companions)
{
    // Every file this point's sites touch, sorted. A point may span files. The
    // npc point touches NpcId.gml and object_manifest.gml.
    public IReadOnlyList<string> Files => Sites
        .Select(s => s.File)
        .Distinct()
        .Order(StringComparer.Ordinal)
        .ToList();

    public ExtensionSite EnumMemberSite =>
        Sites.First(s => s.Kind == ExtensionSiteKind.EnumMember);
}
