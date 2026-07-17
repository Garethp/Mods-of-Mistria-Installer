using System.Text;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// Lints a behavioural mod's code against the seam catalog and the other mods.
// Textual, on the same comment- and string-aware token stream the seam engine
// uses: a hook name riding in a variable is invisible, and the registration
// checks inside mmapi remain the runtime authority. Findings are warn-tier;
// StrictLints escalates the file-bearing ones (D12).
public static class GmlModLint
{
    // The reserved framework namespace. Every name under it is known at apply
    // time, so a call that resolves to nothing is a typo, never a late bind.
    public static readonly string[] MmapiPrefixes = ["mmapi_", "__mmapi_"];

    private static readonly Regex RegisterRegex = new(@"\bmmapi_(on|filter|guard|override)\(\s*""([^""]+)""");

    // Scan one mod's gml for its exports and global writes. Lossy decode: a
    // stray byte is the compiler's diagnostic to make, not a scanner crash.
    public static ModSymbols ScanSymbols(GmlModCode mod)
    {
        var symbols = new ModSymbols(mod.Id);
        foreach (var rel in mod.GmlFiles)
        {
            var text = Decode(mod.Read(rel));
            var tokens = GmlScanner.Tokenize(text);
            foreach (var span in GmlScanner.TopLevelDefinitions(text, tokens))
            {
                if (span.Form != FunctionForm.Decl) continue;

                var line = LineOf(text, span.Start);
                if (symbols.Functions.ContainsKey(span.Name))
                {
                    if (!symbols.Duplicates.TryGetValue(span.Name, out var sites))
                        symbols.Duplicates[span.Name] = sites = [];
                    sites.Add((rel, line));
                }
                else
                {
                    symbols.Functions[span.Name] = (rel, line);
                }
            }

            foreach (var write in GmlScanner.FindGlobalWrites(text, tokens))
            {
                var known = symbols.GlobalRoots.GetValueOrDefault(write.Name);
                if (known is null || (write.Bare && !known.Bare))
                    symbols.GlobalRoots[write.Name] = new GlobalRoot(rel, LineOf(text, write.Start), write.Bare);
            }
        }

        return symbols;
    }

    // Warn-tier symbol findings: unprefixed top-level functions, writes into
    // reserved or foreign namespace roots, the same root replaced by two mods.
    // Duplicate exports are the skip pass's to resolve, not re-reported here.
    // Deep writes into a foreign root (global.__other.flag = ...) are the
    // sanctioned inter-mod protocol and never flagged.
    public static List<LintFinding> LintSymbols(IReadOnlyList<GmlModCode> mods,
        IReadOnlyDictionary<string, ModSymbols> symbols)
    {
        List<LintFinding> findings = [];

        // namespace form → owning mod id
        Dictionary<string, string> namespaces = [];
        foreach (var mod in mods)
        {
            namespaces[mod.DirName] = mod.Id;
            namespaces[mod.Symbol] = mod.Id;
        }

        foreach (var mod in mods)
        {
            var syms = symbols[mod.Id];
            HashSet<string> own = [mod.DirName, mod.Symbol];
            var prefixes = own.SelectMany(ns => new[] { $"{ns}_", $"__{ns}_" }).ToArray();

            foreach (var (name, (rel, line)) in syms.Functions.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                if (prefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal))) continue;

                findings.Add(new LintFinding(mod.Id, rel, line,
                    $"top-level function '{name}' is not namespaced - prefix it {mod.DirName}_ "
                    + $"(or __{mod.DirName}_ for private helpers) so it cannot collide with "
                    + "another mod or a future engine export"));
            }

            foreach (var (root, info) in syms.GlobalRoots.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                // the mod's own namespace roots are exempt from every root
                // check first, even when another mod's namespace is an
                // underscore-prefix of this one's or the mod is named mmapi_*
                if (own.Any(ns => root == $"__{ns}" || root.StartsWith($"__{ns}_", StringComparison.Ordinal)))
                    continue;

                if (root.StartsWith("__mmapi", StringComparison.Ordinal))
                {
                    findings.Add(new LintFinding(mod.Id, info.File, info.Line,
                        $"writes global.{root} - the __mmapi global namespace is reserved for the framework"));
                    continue;
                }

                if (!info.Bare) continue;

                // longest namespace first, so root __beta_fx attributes to a
                // mod named beta_fx over one named beta
                var foreign = namespaces
                    .OrderByDescending(ns => ns.Key.Length)
                    .Where(ns => ns.Value != mod.Id)
                    .Where(ns => root == $"__{ns.Key}" || root.StartsWith($"__{ns.Key}_", StringComparison.Ordinal))
                    .Select(ns => ns.Value)
                    .FirstOrDefault();
                if (foreign is not null)
                {
                    findings.Add(new LintFinding(mod.Id, info.File, info.Line,
                        $"replaces global.{root}, mod '{foreign}'s namespace root - set fields "
                        + "on it (guarded) instead of replacing the struct"));
                }
            }
        }

        // bare-written root → first-writing mod id
        Dictionary<string, string> owners = [];
        foreach (var mod in mods)
        {
            foreach (var (root, info) in symbols[mod.Id].GlobalRoots.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                if (!info.Bare) continue;

                if (!owners.TryGetValue(root, out var owner))
                {
                    owners[root] = mod.Id;
                    continue;
                }

                if (owner != mod.Id)
                {
                    findings.Add(new LintFinding(mod.Id, info.File, info.Line,
                        $"writes global.{root}, which mod '{owner}' also writes - the later "
                        + "boot writer silently clobbers the earlier one"));
                }
            }
        }

        return findings;
    }

    // Warn on every call into the reserved mmapi namespace that nothing
    // defines. treeExports is the post-apply tree's exports (engine + staged
    // framework + hook catalog); every mod's own top-level functions join it.
    // __mmapi_orig_* is exempt: a wrap seam pairs it with its wrapper by
    // construction.
    public static List<LintFinding> LintMmapiCalls(IReadOnlyList<GmlModCode> mods,
        IReadOnlyDictionary<string, ModSymbols> symbols, IReadOnlyDictionary<string, string> treeExports)
    {
        HashSet<string> known = [.. treeExports.Keys];
        foreach (var syms in symbols.Values) known.UnionWith(syms.Functions.Keys);

        List<LintFinding> findings = [];
        foreach (var mod in mods)
        {
            foreach (var rel in mod.GmlFiles)
            {
                var text = Decode(mod.Read(rel));
                foreach (var (name, start) in GmlScanner.FindPrefixedCalls(text, MmapiPrefixes))
                {
                    if (known.Contains(name)
                        || name.StartsWith(DispatchRenderer.OrigPrefix, StringComparison.Ordinal)) continue;

                    findings.Add(new LintFinding(mod.Id, rel, LineOf(text, start),
                        $"calls '{name}', which nothing defines - the mmapi namespace is the "
                        + "framework's, so this is a typo or a call into a newer mmapi than "
                        + "this installer ships. The compat dialect late-binds unknown names, "
                        + "so it would compile and fail only in-game"));
                }
            }
        }

        return findings;
    }

    // Every hook-registration finding across every mod, in mod order, the
    // cross-mod override contention report last. Contention findings are
    // file-less and added once per participating mod, so each mod's expander
    // shows the conflict (D12).
    public static List<LintFinding> LintHooks(IReadOnlyList<GmlModCode> mods, SeamCatalog catalog)
    {
        var declarations = catalog.HookDeclarations.ToDictionary(d => d.Name);
        var aliases = catalog.HookDeclarations
            .SelectMany(d => d.Aliases.Select(a => (Alias: a, Canonical: d.Name)))
            .ToDictionary(a => a.Alias, a => a.Canonical);

        List<LintFinding> findings = [];
        Dictionary<string, List<string>> overriders = [];  // hook name → overriding mod ids

        foreach (var mod in mods)
        {
            foreach (var rel in mod.GmlFiles)
            {
                var text = Decode(mod.Read(rel));
                foreach (Match match in RegisterRegex.Matches(text))
                {
                    var directive = match.Groups[1].Value;
                    var name = match.Groups[2].Value;
                    var line = LineOf(text, match.Index);

                    if (aliases.TryGetValue(name, out var canonical))
                    {
                        findings.Add(new LintFinding(mod.Id, rel, line,
                            $"registers alias '{name}' for '{canonical}' - update to the canonical name"));
                        name = canonical;
                    }

                    if (!declarations.TryGetValue(name, out var declaration))
                    {
                        findings.Add(new LintFinding(mod.Id, rel, line,
                            $"registers unknown hook '{name}' - a typo, or a hook this seam "
                            + "catalog does not provide"));
                        continue;
                    }

                    var want = DirectiveKind(directive);
                    if (declaration.Kind != want)
                    {
                        findings.Add(new LintFinding(mod.Id, rel, line,
                            $"registers '{name}' with mmapi_{directive}, but the hook is declared "
                            + $"kind `{declaration.Kind.CatalogName()}` - use {KindDirective(declaration.Kind)}"));
                    }

                    if (want != HookKind.Override) continue;

                    if (!overriders.TryGetValue(name, out var modsForHook))
                        overriders[name] = modsForHook = [];
                    if (!modsForHook.Contains(mod.Id)) modsForHook.Add(mod.Id);
                }
            }
        }

        foreach (var name in overriders.Keys.Order(StringComparer.Ordinal))
        {
            var modIds = overriders[name];
            if (modIds.Count < 2) continue;

            var contention = declarations.GetValueOrDefault(name)?.Contention;
            var message = contention switch
            {
                HookContention.ClaimScoped =>
                    $"hook '{name}' is claim-scoped and overridden by {modIds.Count} mods "
                    + $"({string.Join(", ", modIds)}) - they coexist when each handler returns "
                    + "undefined for targets it does not own; priority order decides simultaneous claims",
                HookContention.Exclusive =>
                    $"hook '{name}' is exclusive and overridden by {modIds.Count} mods "
                    + $"({string.Join(", ", modIds)}) - only the first non-undefined result wins; "
                    + "the other mods' handlers never take effect",
                _ =>
                    $"hook '{name}' is overridden by {modIds.Count} mods "
                    + $"({string.Join(", ", modIds)}) - only the first non-undefined result wins",
            };
            findings.AddRange(modIds.Select(modId => new LintFinding(modId, "", 0, message)));
        }

        return findings;
    }

    private static HookKind DirectiveKind(string directive) => directive switch
    {
        "on" => HookKind.Event,
        "filter" => HookKind.Filter,
        "guard" => HookKind.Guard,
        _ => HookKind.Override,
    };

    private static string KindDirective(HookKind kind) => kind switch
    {
        HookKind.Event => "mmapi_on",
        HookKind.Filter => "mmapi_filter",
        HookKind.Guard => "mmapi_guard",
        _ => "mmapi_override",
    };

    // Mod gml decodes lossily for scan and lint (fidelity note 5)
    private static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n");

    private static int LineOf(string text, int pos) => text.AsSpan(0, pos).Count('\n') + 1;
}
