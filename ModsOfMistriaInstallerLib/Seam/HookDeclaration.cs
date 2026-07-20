namespace Garethp.ModsOfMistriaInstallerLib.Seam;

public enum HookKind
{
    Event,
    Filter,
    Guard,
    Override,
}

// "seam": an engine edit dispatches the hook; "runtime": the framework emits
// it itself, with no engine edit behind it.
public enum HookProvider
{
    Seam,
    Runtime,
}

// Override hooks only: whether rival handlers coexist by claiming disjoint
// targets (claim-scoped) or genuinely conflict (exclusive).
public enum HookContention
{
    Exclusive,
    ClaimScoped,
}

// The lowercase catalog spelling of each enum lives in one mapping per enum,
// so the TOML surface and the generated GML cannot drift from the type system.
public static class CatalogEnums
{
    public const string HookKindNames = "event, filter, guard, override";
    public const string HookProviderNames = "seam, runtime";
    public const string HookContentionNames = "exclusive, claim-scoped";

    public static HookKind? ParseHookKind(string value) => value switch
    {
        "event" => HookKind.Event,
        "filter" => HookKind.Filter,
        "guard" => HookKind.Guard,
        "override" => HookKind.Override,
        _ => null,
    };

    public static string CatalogName(this HookKind kind) => kind switch
    {
        HookKind.Event => "event",
        HookKind.Filter => "filter",
        HookKind.Guard => "guard",
        HookKind.Override => "override",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static HookProvider? ParseHookProvider(string value) => value switch
    {
        "seam" => HookProvider.Seam,
        "runtime" => HookProvider.Runtime,
        _ => null,
    };

    public static string CatalogName(this HookProvider provider) => provider switch
    {
        HookProvider.Seam => "seam",
        HookProvider.Runtime => "runtime",
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    public static HookContention? ParseHookContention(string value) => value switch
    {
        "exclusive" => HookContention.Exclusive,
        "claim-scoped" => HookContention.ClaimScoped,
        _ => null,
    };

    public static string CatalogName(this HookContention contention) => contention switch
    {
        HookContention.Exclusive => "exclusive",
        HookContention.ClaimScoped => "claim-scoped",
        _ => throw new ArgumentOutOfRangeException(nameof(contention)),
    };
}

// One declared hook: its kind, its contract doc, and who provides it.
public record HookDeclaration(
    string Name,                    // dotted hook name, e.g. "items.use_guard"
    HookKind Kind,
    string Doc,                     // the contract: where it fires, ctx shape, returns
    HookProvider Provider,
    IReadOnlyList<string> Aliases,  // old names that still resolve here (warn-once)
    bool InPlace,                   // filter whose return is discarded, so handlers mutate ctx
    HookContention? Contention);    // override hooks only, null otherwise
