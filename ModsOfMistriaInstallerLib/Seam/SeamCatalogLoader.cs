using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Loads and validates the seam catalog (seams.toml: [[hook]], [[seam]],
// [[engine_fix]] and [[call_rewrite]] entries plus the optional [counts]
// integrity table, schema version 2). Anchor and
// replace are normalised from \r\n to \n; the byte-exact contract is modulo
// CRLF. Validation is batched: every problem in the catalog is reported in
// one SeamCatalogException, not just the first.
public static class SeamCatalogLoader
{
    public const int SupportedCatalogVersion = 2;

    // Hook names and markers are interpolated into generated GML, so their
    // charset is a correctness constraint, not style: a name carrying a quote
    // or a newline renders a hook catalog that does not parse, or a dispatch
    // payload whose marker comment runs off its line.
    private static readonly Regex HookNameRegex = new(@"\A[a-z0-9_]+(?:\.[a-z0-9_]+)*\z");
    private static readonly Regex GmlIdentRegex = new(@"\A[A-Za-z_][A-Za-z0-9_]*\z");

    // Extension field names are template placeholder keys, so they share the
    // lower_snake_case shape the templates spell them in.
    private static readonly Regex ExtensionFieldNameRegex = new(@"\A[a-z][a-z0-9_]*\z");

    // The four dispatchers a replace body may call → the hook kind each one
    // implies. Every hand-written replace is linted against this table.
    private static readonly Dictionary<string, HookKind> Dispatchers = new()
    {
        ["mmapi_emit"] = HookKind.Event,
        ["mmapi_apply_filters"] = HookKind.Filter,
        ["mmapi_check_guards"] = HookKind.Guard,
        ["mmapi_run_override"] = HookKind.Override,
    };

    private static readonly Regex DispatchRegex =
        new(@"\b(mmapi_emit|mmapi_apply_filters|mmapi_check_guards|mmapi_run_override)\(\s*""([^""]+)""");

    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    public static SeamCatalog Load(byte[] bytes, string sourceName)
    {
        var doc = Toml.ParseDocument(Utf8Strict.GetString(bytes));
        var version = (int)(doc.TryGetValue("version", out var versionRaw)
            ? Convert.ToInt64(versionRaw, CultureInfo.InvariantCulture)
            : 0);
        if (version != SupportedCatalogVersion)
            throw new SeamCatalogException(sourceName,
                [$"unsupported version {version} (expected {SupportedCatalogVersion})"]);

        List<string> errors = [];

        // The optional [counts] integrity table: declared totals checked against
        // the parsed stanzas at the end of validation. Optional so fixture
        // catalogs stay terse; the shipped catalog always declares it (the
        // shipped-catalog test holds it to that).
        CatalogCounts? declaredCounts = null;
        if (doc.TryGetValue("counts", out var countsRaw))
        {
            if (countsRaw is TomlTable countsTable)
            {
                Dictionary<string, int> countFields = [];
                foreach (var field in (string[]) ["hooks", "seams", "engine_fixes", "call_rewrites"])
                {
                    if (countsTable.TryGetValue(field, out var raw))
                        countFields[field] = (int)Convert.ToInt64(raw, CultureInfo.InvariantCulture);
                    else
                        errors.Add($"[counts] is missing '{field}'");
                }

                if (countFields.Count == 4)
                    declaredCounts = new CatalogCounts(countFields["hooks"], countFields["seams"],
                        countFields["engine_fixes"], countFields["call_rewrites"]);
            }
            else
            {
                errors.Add("[counts] must be a table");
            }
        }

        // hook declarations, then alias collisions in declaration order
        List<HookDeclaration> declarations = [];
        Dictionary<string, HookDeclaration> byName = [];
        foreach (var (table, index) in Tables(doc, "hook"))
        {
            var declaration = ParseHook(table, index, errors);
            if (declaration is null) continue;
            if (byName.ContainsKey(declaration.Name))
            {
                errors.Add($"duplicate [[hook]] '{declaration.Name}'");
                continue;
            }

            declarations.Add(declaration);
            byName[declaration.Name] = declaration;
        }

        Dictionary<string, string> aliasOwner = [];
        foreach (var declaration in declarations)
        {
            foreach (var alias in declaration.Aliases)
            {
                if (byName.ContainsKey(alias))
                    errors.Add($"[[hook]] '{declaration.Name}' alias '{alias}' collides with a hook name");
                else if (aliasOwner.TryGetValue(alias, out var owner))
                    errors.Add($"alias '{alias}' claimed by both '{owner}' and '{declaration.Name}'");
                else
                    aliasOwner[alias] = declaration.Name;
            }
        }

        List<SeamEntry> entries = [];
        foreach (var (table, index) in Tables(doc, "seam"))
        {
            var entry = ParseEntry(table, SeamEntryKind.Seam, "id", index, errors);
            if (entry is not null) entries.Add(entry);
        }

        foreach (var (table, index) in Tables(doc, "engine_fix"))
        {
            var entry = ParseEntry(table, SeamEntryKind.EngineFix, "name", index, errors);
            if (entry is not null) entries.Add(entry);
        }

        List<CallRewrite> rewrites = [];
        foreach (var (table, index) in Tables(doc, "call_rewrite"))
        {
            var rewrite = ParseCallRewrite(table, index, errors);
            if (rewrite is not null) rewrites.Add(rewrite);
        }

        List<ExtensionPoint> extensions = [];
        foreach (var (table, index) in Tables(doc, "extension"))
        {
            var point = ParseExtension(table, index, errors);
            if (point is not null) extensions.Add(point);
        }

        // duplicate ids and markers, provides/declared cross-checks, the replace lint
        Dictionary<string, string> seenIds = [];
        Dictionary<string, string> seenMarkers = [];
        HashSet<string> provided = [];
        foreach (var entry in entries)
        {
            if (seenIds.ContainsKey(entry.Id)) errors.Add($"duplicate id/name '{entry.Id}'");
            seenIds[entry.Id] = entry.Kind.CatalogName();
            if (seenMarkers.TryGetValue(entry.Marker, out var markerOwner))
                errors.Add($"duplicate marker '{entry.Marker}' ('{markerOwner}' and '{entry.Id}')");
            seenMarkers[entry.Marker] = entry.Id;
            foreach (var hook in entry.Hooks)
            {
                provided.Add(hook);
                if (!byName.ContainsKey(hook))
                    errors.Add($"[[seam]] '{entry.Id}' provides undeclared hook '{hook}' - "
                               + "add a [[hook]] stanza for it");
            }

            LintReplace(entry, byName, errors);
        }

        Dictionary<string, string> seenCallees = [];
        foreach (var rewrite in rewrites)
        {
            if (seenIds.ContainsKey(rewrite.Id)) errors.Add($"duplicate id/name '{rewrite.Id}'");
            seenIds[rewrite.Id] = "call_rewrite";
            if (seenCallees.TryGetValue(rewrite.Callee, out var calleeOwner))
                errors.Add($"[[call_rewrite]] '{calleeOwner}' and '{rewrite.Id}' "
                           + $"both rewrite '{rewrite.Callee}'");
            seenCallees[rewrite.Callee] = rewrite.Id;
            foreach (var hook in rewrite.Hooks)
            {
                provided.Add(hook);
                if (!byName.ContainsKey(hook))
                    errors.Add($"[[call_rewrite]] '{rewrite.Id}' provides undeclared hook '{hook}' - "
                               + "add a [[hook]] stanza for it");
            }
        }

        // extension ids share the seam/fix/rewrite id namespace. One namespace
        // for problem reporting, so `ext:npc_roster` in a batched error is
        // never ambiguous about which entry it names
        foreach (var point in extensions)
        {
            if (seenIds.ContainsKey(point.Id)) errors.Add($"duplicate id/name '{point.Id}'");
            seenIds[point.Id] = "extension";
        }

        foreach (var rewrite in rewrites)
        {
            if (seenCallees.TryGetValue(rewrite.To, out var owner))
                errors.Add($"[[call_rewrite]] '{rewrite.Id}' rewrites to '{rewrite.To}', which "
                           + $"'{owner}' rewrites away - rewrites must not chain");
        }

        foreach (var declaration in declarations)
        {
            if (declaration.Provider == HookProvider.Seam && !provided.Contains(declaration.Name))
                errors.Add($"[[hook]] '{declaration.Name}' is declared provider=seam but no seam "
                           + "provides it (declare provider=\"runtime\" if the framework emits it)");
            if (declaration.Provider == HookProvider.Runtime && provided.Contains(declaration.Name))
                errors.Add($"[[hook]] '{declaration.Name}' is declared provider=runtime but a seam "
                           + "provides it - drop the provider field");
        }

        // Declared-vs-parsed integrity: a mismatch means a truncated file or a
        // forgotten bump; either way the catalog does not load.
        if (declaredCounts is not null)
        {
            var seamCount = entries.Count(e => e.Kind == SeamEntryKind.Seam);
            var fixCount = entries.Count(e => e.Kind == SeamEntryKind.EngineFix);
            if (declaredCounts.Hooks != declarations.Count)
                errors.Add($"[counts] declares {declaredCounts.Hooks} hooks but the catalog parses {declarations.Count}");
            if (declaredCounts.Seams != seamCount)
                errors.Add($"[counts] declares {declaredCounts.Seams} seams but the catalog parses {seamCount}");
            if (declaredCounts.EngineFixes != fixCount)
                errors.Add($"[counts] declares {declaredCounts.EngineFixes} engine fixes but the catalog parses {fixCount}");
            if (declaredCounts.CallRewrites != rewrites.Count)
                errors.Add($"[counts] declares {declaredCounts.CallRewrites} call rewrites but the catalog parses {rewrites.Count}");
        }

        var ordered = OrderEntries(entries, errors);

        if (errors.Count > 0) throw new SeamCatalogException(sourceName, errors);

        return new SeamCatalog(
            version,
            ordered,
            declarations.OrderBy(d => d.Name, StringComparer.Ordinal).ToList(),
            rewrites,
            extensions,
            declaredCounts);
    }

    private static HookDeclaration? ParseHook(TomlTable table, int index, List<string> errors)
    {
        var where = $"[[hook]] #{index + 1}";
        var name = Str(table, "name");
        if (name.Length == 0)
        {
            errors.Add($"{where} is missing `name`");
            return null;
        }

        where = $"[[hook]] '{name}'";
        if (!HookNameRegex.IsMatch(name))
        {
            errors.Add($"{where} is not a dotted lowercase hook name "
                       + "(a.b_c) - the name is rendered into the generated hook "
                       + "catalog as a GML string literal");
            return null;
        }

        var kindText = Str(table, "kind");
        var kind = CatalogEnums.ParseHookKind(kindText);
        if (kind is null)
        {
            errors.Add($"{where} kind '{kindText}' is not one of {CatalogEnums.HookKindNames}");
            return null;
        }

        var providerText = table.ContainsKey("provider") ? Str(table, "provider") : "seam";
        var provider = CatalogEnums.ParseHookProvider(providerText);
        if (provider is null)
        {
            errors.Add($"{where} provider '{providerText}' is not one of {CatalogEnums.HookProviderNames}");
            return null;
        }

        var contentionText = Str(table, "contention");
        HookContention? contention = null;
        if (kind == HookKind.Override)
        {
            contention = CatalogEnums.ParseHookContention(contentionText);
            if (contention is null)
            {
                var got = contentionText.Length > 0 ? $"contention '{contentionText}' is not" : "needs `contention` -";
                errors.Add($"{where} {got} one of {CatalogEnums.HookContentionNames}: every "
                           + "override hook declares whether rival handlers coexist by "
                           + "claiming disjoint targets (claim-scoped) or genuinely "
                           + "conflict (exclusive)");
                return null;
            }
        }
        else if (contentionText.Length > 0)
        {
            errors.Add($"{where} states `contention` - only override hooks carry it");
            return null;
        }

        List<string> aliases = [];
        if (table.TryGetValue("aliases", out var aliasesRaw))
        {
            if (aliasesRaw is not TomlArray aliasArray || aliasArray.Any(a => a is not string))
            {
                errors.Add($"{where} `aliases` must be an array of strings");
                return null;
            }

            var bad = aliasArray.Cast<string>()
                .Where(a => !HookNameRegex.IsMatch(a.Trim()))
                .ToList();
            if (bad.Count > 0)
            {
                errors.Add($"{where} alias(es) {string.Join(", ", bad.Select(a => $"'{a}'"))} "
                           + "are not dotted lowercase hook names - they are rendered "
                           + "into the generated alias table as GML string literals");
                return null;
            }

            aliases = aliasArray.Cast<string>().Select(a => a.Trim()).ToList();
        }

        return new HookDeclaration(
            Name: name,
            Kind: kind.Value,
            Doc: Str(table, "doc"),
            Provider: provider.Value,
            Aliases: aliases,
            InPlace: Flag(table, "in_place"),
            Contention: contention);
    }

    // A template-form [[seam]]: the injected dispatch is generated from the op,
    // provides derives from the hook, and marker/catch_var default from the id.
    // The payload lands via one of two locators: pristine context stated once
    // (context_before/context_after, whose concatenation is the anchor), or a
    // structural target (fn + at + token anchor) resolved at apply time.
    private static SeamEntry? ParseTemplateEntry(TomlTable table, string entryId, List<string> errors)
    {
        var where = $"[[seam]] '{entryId}'";

        var opText = Str(table, "op");
        var op = DispatchOps.Parse(opText);
        if (op is null)
        {
            errors.Add($"{where} op '{opText}' is not one of {DispatchOps.Names}");
            return null;
        }

        var hook = Str(table, "hook");
        if (hook.Length == 0)
        {
            errors.Add($"{where} is missing `hook`");
            return null;
        }

        if (table.ContainsKey("provides"))
            errors.Add($"{where} states `provides` - a template seam derives it from `hook`");
        if (table.ContainsKey("replace") || table.ContainsKey("anchor"))
            errors.Add($"{where} states anchor/replace - a template seam states "
                       + "context_before/context_after or a target instead");

        var contextBefore = Norm(RawStr(table, "context_before"));
        var contextAfter = Norm(RawStr(table, "context_after"));

        var targetFn = "";
        var targetAt = "";
        var targetAnchor = "";
        if (table.TryGetValue("target", out var targetRaw))
        {
            if (contextBefore.Length > 0 || contextAfter.Length > 0)
            {
                errors.Add($"{where} states both a target and context - pick one locator");
                return null;
            }

            if (targetRaw is not TomlTable target)
            {
                errors.Add($"{where} `target` must be a table: {{ fn, at, anchor }}");
                return null;
            }

            targetFn = Str(target, "fn");
            if (targetFn.Length == 0)
            {
                errors.Add($"{where} target is missing `fn`");
                return null;
            }

            targetAt = Str(target, "at");
            targetAnchor = Str(target, "anchor");
            if (op == DispatchOp.Wrap)
            {
                if (targetAt.Length > 0 || targetAnchor.Length > 0)
                    errors.Add($"{where} a wrap targets the whole function - drop at/anchor");
            }
            else if (targetAt is not ("head" or "before" or "after"))
            {
                errors.Add($"{where} target at '{targetAt}' is not head, before, or after");
                return null;
            }
            else if (targetAt == "head")
            {
                if (targetAnchor.Length > 0)
                    errors.Add($"{where} target at=head ignores `anchor` - drop it");
            }
            else if (targetAnchor.Length == 0)
            {
                errors.Add($"{where} target at={targetAt} needs `anchor`");
                return null;
            }
        }
        else if (op == DispatchOp.Wrap)
        {
            errors.Add($"{where} a wrap needs a target: {{ fn = \"...\" }}");
            return null;
        }
        else if (contextBefore.Length == 0 && contextAfter.Length == 0)
        {
            errors.Add($"{where} needs context_before/context_after or a target");
            return null;
        }

        var required = op switch
        {
            DispatchOp.Guard => "on_veto",
            DispatchOp.Filter => "var",
            DispatchOp.FilterCall => "value",
            DispatchOp.CtxFilter => "ctx_var",
            _ => null,
        };
        if (required is not null && Str(table, required).Length == 0)
        {
            errors.Add($"{where} op `{opText}` needs `{required}`");
            return null;
        }

        List<(string Key, string Value)>? ctxFields = null;
        if (table.TryGetValue("ctx_fields", out var ctxFieldsRaw)
            && ctxFieldsRaw is TomlArray { Count: > 0 } fieldArray)
        {
            ctxFields = fieldArray
                .Select(pair => (TomlArray)pair!)
                .Select(pair => (ToStr(pair[0]), ToStr(pair[1])))
                .ToList();
        }

        var marker = Str(table, "marker");
        if (marker.Length == 0) marker = DispatchRenderer.DefaultMarker(entryId);
        var catchVar = Str(table, "catch_var");
        if (catchVar.Length == 0) catchVar = DispatchRenderer.DefaultCatchVar(entryId);
        CheckMarker(marker, where, errors);
        CheckCatchVar(catchVar, where, errors);

        string payload;
        if (op == DispatchOp.Wrap)
        {
            // the wrapper itself is rendered at apply time, once the function's
            // form and parameters are known. The filter statement it will carry
            // is fixed now, so the lint and the marker discipline see the real
            // dispatch. Blank padding rides on the payload as in RenderPayload.
            payload = (Flag(table, "blank_before") ? "\n" : "")
                      + DispatchRenderer.WrapFilterLine(hook, RawStr(table, "ctx", "undefined"), marker, catchVar)
                      + "\n"
                      + (Flag(table, "blank_after") ? "\n" : "");
        }
        else
        {
            payload = DispatchRenderer.RenderPayload(op.Value, new PayloadOptions(
                Hook: hook,
                Indent: (int)(table.TryGetValue("indent", out var indentRaw)
                    ? Convert.ToInt64(indentRaw, CultureInfo.InvariantCulture)
                    : 4),
                Marker: marker,
                CatchVar: catchVar)
            {
                Ctx = RawStr(table, "ctx", "undefined"),
                CtxFields = ctxFields,
                Var = RawStr(table, "var"),
                Value = RawStr(table, "value"),
                OnVeto = RawStr(table, "on_veto"),
                Writeback = table.TryGetValue("writeback", out var writebackRaw)
                            && writebackRaw is TomlArray writebackArray
                    ? writebackArray.Select(ToStr).ToList()
                    : [],
                CtxVar = RawStr(table, "ctx_var"),
                TryCatch = !table.TryGetValue("try_catch", out var tryCatchRaw)
                           || Convert.ToBoolean(tryCatchRaw, CultureInfo.InvariantCulture),
                BlankBefore = Flag(table, "blank_before"),
                BlankAfter = Flag(table, "blank_after"),
            });
        }

        var dependsOn = ParseDependsOn(table, where, errors);

        if (Str(table, "file").Length == 0)
        {
            errors.Add($"{where} is missing `file`");
            return null;
        }

        var entryFile = NormFilePath(ToStr(table["file"]));
        CheckFilePath(entryFile, where, errors);

        var entry = new SeamEntry(
            Id: entryId,
            Kind: SeamEntryKind.Seam,
            File: entryFile,
            Anchor: contextBefore + contextAfter,
            Replace: contextBefore + payload + contextAfter,
            Marker: marker,
            Hooks: [hook],
            DependsOn: dependsOn,
            Op: op,
            TargetFn: targetFn,
            TargetAt: targetAt,
            TargetAnchor: targetAnchor);
        if (entry.Anchor.Contains(entry.Marker, StringComparison.Ordinal))
            errors.Add($"{where} marker '{entry.Marker}' appears in the pristine context - "
                       + "it cannot identify this entry's edit");
        return entry;
    }

    private static SeamEntry? ParseEntry(TomlTable table, SeamEntryKind kind, string idKey, int index,
        List<string> errors)
    {
        var kindName = kind.CatalogName();
        var where = $"[[{kindName}]] #{index + 1}";
        var entryId = Str(table, idKey);
        if (entryId.Length == 0)
        {
            errors.Add($"{where} is missing `{idKey}`");
            return null;
        }

        if (kind == SeamEntryKind.Seam && table.ContainsKey("op"))
            return ParseTemplateEntry(table, entryId, errors);
        where = $"[[{kindName}]] '{entryId}'";

        var missing = new[] { "file", "anchor", "replace", "marker" }
            .Where(key => Str(table, key).Length == 0)
            .ToList();
        if (missing.Count > 0)
        {
            errors.Add($"{where} is missing {string.Join(", ", missing)}");
            return null;
        }

        var anchor = Norm(ToStr(table["anchor"]));
        var replace = Norm(ToStr(table["replace"]));
        var marker = ToStr(table["marker"]).Trim();
        CheckMarker(marker, where, errors);
        if (anchor == replace) errors.Add($"{where} has identical anchor and replace");
        if (!replace.Contains(marker, StringComparison.Ordinal))
            errors.Add($"{where} marker '{marker}' does not appear in `replace` - "
                       + "the entry would be unidentifiable in staged output");
        if (anchor.Contains(marker, StringComparison.Ordinal))
            errors.Add($"{where} marker '{marker}' appears in the pristine `anchor` - "
                       + "it cannot identify this entry's edit");

        List<string> hooks = [];
        if (kind == SeamEntryKind.Seam)
        {
            if (!table.TryGetValue("provides", out var providesRaw)) providesRaw = new TomlArray();
            if (providesRaw is not TomlArray providesArray || providesArray.Any(h => h is not string))
                errors.Add($"{where} `provides` must be an array of hook-name strings");
            else
                hooks = providesArray.Cast<string>().Select(h => h.Trim()).ToList();
            if (hooks.Count == 0)
                errors.Add($"{where} provides no hooks (use [[engine_fix]] for plain edits)");
        }

        var dependsOn = ParseDependsOn(table, where, errors);
        var entryFile = NormFilePath(ToStr(table["file"]));
        CheckFilePath(entryFile, where, errors);

        return new SeamEntry(
            Id: entryId,
            Kind: kind,
            File: entryFile,
            Anchor: anchor,
            Replace: replace,
            Marker: marker,
            Hooks: hooks,
            DependsOn: dependsOn,
            Op: null,
            TargetFn: "",
            TargetAt: "",
            TargetAnchor: "");
    }

    private static CallRewrite? ParseCallRewrite(TomlTable table, int index, List<string> errors)
    {
        var where = $"[[call_rewrite]] #{index + 1}";
        var rewriteId = Str(table, "id");
        if (rewriteId.Length == 0)
        {
            errors.Add($"{where} is missing `id`");
            return null;
        }

        where = $"[[call_rewrite]] '{rewriteId}'";

        var ok = true;
        var callee = Str(table, "callee");
        var to = Str(table, "to");
        foreach (var (key, value) in new[] { ("callee", callee), ("to", to) })
        {
            if (value.Length == 0)
            {
                errors.Add($"{where} is missing `{key}`");
                ok = false;
            }
            else if (!GmlIdentRegex.IsMatch(value))
            {
                errors.Add($"{where} {key} '{value}' is not a plain identifier");
                ok = false;
            }
        }

        if (ok && callee == to)
        {
            errors.Add($"{where} rewrites '{callee}' to itself");
            ok = false;
        }

        table.TryGetValue("args", out var argsRaw);
        if (argsRaw is not long args || args < 0)
        {
            errors.Add($"{where} `args` must be a non-negative integer - the arity "
                       + "every rewritten call must pass");
            ok = false;
            args = 0;
        }

        List<string> hooks = [];
        if (!table.TryGetValue("provides", out var providesRaw)) providesRaw = new TomlArray();
        if (providesRaw is not TomlArray providesArray || providesArray.Any(h => h is not string))
        {
            errors.Add($"{where} `provides` must be an array of hook-name strings");
            ok = false;
        }
        else
        {
            hooks = providesArray.Cast<string>().Select(h => h.Trim()).ToList();
            if (hooks.Count == 0)
            {
                errors.Add($"{where} provides no hooks - the wrapper it redirects "
                           + "into exists to dispatch them");
                ok = false;
            }
        }

        if (!ok) return null;
        return new CallRewrite(rewriteId, callee, to, (int)args, hooks);
    }

    // One [[extension]] stanza, carrying the ordinal domain, the typed fields a registration
    // supplies, the sites generated lines land at, and the vacancy text for a
    // ledger entry whose mod is gone. Everything here is catalog-authored -
    // a mod never ships anchors, templates or engine text, and this
    // parser is where that stays true.
    private static ExtensionPoint? ParseExtension(TomlTable table, int index, List<string> errors)
    {
        var where = $"[[extension]] #{index + 1}";
        var pointId = Str(table, "id");
        if (pointId.Length == 0)
        {
            errors.Add($"{where} is missing `id`");
            return null;
        }

        where = $"[[extension]] '{pointId}'";

        if (Str(table, "file").Length == 0)
        {
            errors.Add($"{where} is missing `file`");
            return null;
        }

        var pointFile = NormFilePath(ToStr(table["file"]));
        CheckFilePath(pointFile, where, errors);

        // [extension.ordinal] is required. Every expressible domain is a
        // roster enum with a LEN sentinel, the expander makes ordinal order
        // the determinism basis, and an optional form would be a second
        // ordering path with no users.
        if (!table.TryGetValue("ordinal", out var ordinalRaw) || ordinalRaw is not TomlTable ordinal)
        {
            errors.Add($"{where} is missing `[extension.ordinal]` - it is required: every "
                       + "extension point extends a GML-declared roster enum, and ordinal order "
                       + "is what makes staged output deterministic. This is a deliberate "
                       + "restriction, not an oversight; add the ordinal-less form when a "
                       + "concrete point needs one");
            return null;
        }

        var ordinalEnum = Str(ordinal, "enum");
        var ordinalSentinel = Str(ordinal, "sentinel");
        foreach (var (key, value) in new[] { ("enum", ordinalEnum), ("sentinel", ordinalSentinel) })
        {
            if (value.Length == 0) errors.Add($"{where} `[extension.ordinal]` is missing `{key}`");
            else if (!GmlIdentRegex.IsMatch(value))
                errors.Add($"{where} `[extension.ordinal].{key}` '{value}' is not a plain identifier");
        }

        List<ExtensionField> fields = [];
        HashSet<string> fieldNames = [];
        foreach (var (fieldTable, fieldIndex) in Tables(table, "fields"))
        {
            var field = ParseExtensionField(fieldTable, fieldIndex, where, errors);
            if (field is null) continue;
            if (!fieldNames.Add(field.Name))
            {
                errors.Add($"{where} declares field '{field.Name}' twice");
                continue;
            }

            fields.Add(field);
        }

        // vacancy templates are keyed by site id, so sites must be parsed first
        var vacancy = table.TryGetValue("vacancy", out var vacancyRaw) && vacancyRaw is TomlTable vacancyTable
            ? vacancyTable
            : null;

        List<ExtensionSite> sites = [];
        HashSet<string> siteIds = [];
        foreach (var (siteTable, siteIndex) in Tables(table, "sites"))
        {
            var site = ParseExtensionSite(siteTable, siteIndex, where, pointFile, fieldNames, vacancy, errors);
            if (site is null) continue;
            if (!siteIds.Add(site.Id))
            {
                errors.Add($"{where} declares site '{site.Id}' twice");
                continue;
            }

            sites.Add(site);
        }

        var enumMembers = sites.Count(s => s.Kind == ExtensionSiteKind.EnumMember);
        if (enumMembers != 1)
            errors.Add($"{where} declares {enumMembers} `enum_member` site(s) - exactly one is "
                       + "required: the ordinal domain needs precisely one place to grow");

        var enumSite = sites.FirstOrDefault(s => s.Kind == ExtensionSiteKind.EnumMember);
        if (enumSite is not null && enumSite.File != pointFile)
            errors.Add($"{where} site '{enumSite.Id}' overrides `file` - the `enum_member` site "
                       + "cannot: the ordinal enum lives in the point's own file by definition");

        // An unknown vacancy key is an error, not a no-op. A typo would
        // otherwise silently mean "no vacancy line for that site", and for the
        // enum_member site that silence is the fatal-hole case. A gap ordinal
        // kills the game at launch, before the main menu.
        if (vacancy is not null)
        {
            foreach (var key in vacancy.Keys.Where(k => !siteIds.Contains(k)).Order(StringComparer.Ordinal))
                errors.Add($"{where} `[extension.vacancy]` key '{key}' names no declared site - "
                           + "a typo here silently drops that site's vacancy line, and for the "
                           + "enum_member site a missing vacancy line is a gap ordinal, which "
                           + "crashes the game at launch");
        }

        List<ExtensionVacancyFile> vacancyFiles = [];
        HashSet<string> vacancyPaths = [];
        foreach (var (fileTable, fileIndex) in Tables(table, "vacancy_files"))
        {
            var vacancyFile = ParseExtensionVacancyFile(fileTable, fileIndex, where, errors);
            if (vacancyFile is null) continue;
            if (!vacancyPaths.Add(vacancyFile.Path))
            {
                errors.Add($"{where} declares two `vacancy_files` with path template "
                           + $"'{vacancyFile.Path}' - they would render over each other");
                continue;
            }

            vacancyFiles.Add(vacancyFile);
        }

        List<ExtensionCompanion> companions = [];
        HashSet<string> companionPaths = [];
        foreach (var (companionTable, companionIndex) in Tables(table, "companions"))
        {
            var companion = ParseExtensionCompanion(companionTable, companionIndex, where, errors);
            if (companion is null) continue;
            if (!companionPaths.Add(companion.Path))
            {
                errors.Add($"{where} declares two `companions` with path template "
                           + $"'{companion.Path}' - one of them can never say anything new");
                continue;
            }

            companions.Add(companion);
        }

        // An error-level companion asserts "a live member without this data
        // breaks the boot". A vacancy is the same member with the same needs,
        // so a point making that assertion must also say what a vacancy gets.
        if (companions.Any(c => c.Level == ExtensionCompanionLevel.Error) && vacancyFiles.Count == 0)
            errors.Add($"{where} declares an error-level companion but no "
                       + "`[[extension.vacancy_files]]` - the companion says absent data breaks "
                       + "the boot, and a ledger vacancy is a member with no mod behind it, so "
                       + "it needs a generated stub or every excluded registrant bricks the game");

        return new ExtensionPoint(
            Id: pointId,
            File: pointFile,
            Doc: Str(table, "doc"),
            OrdinalEnum: ordinalEnum,
            OrdinalSentinel: ordinalSentinel,
            Fields: fields,
            Sites: sites,
            VacancyFiles: vacancyFiles,
            Companions: companions);
    }

    private static ExtensionField? ParseExtensionField(TomlTable table, int index, string where,
        List<string> errors)
    {
        var name = Str(table, "name");
        if (name.Length == 0)
        {
            errors.Add($"{where} `[[extension.fields]]` #{index + 1} is missing `name`");
            return null;
        }

        if (!ExtensionFieldNameRegex.IsMatch(name))
        {
            errors.Add($"{where} field name '{name}' is not lower_snake_case "
                       + "(^[a-z][a-z0-9_]*$) - field names are template placeholder keys");
            return null;
        }

        var typeText = Str(table, "type");
        var type = ExtensionEnums.ParseFieldType(typeText);
        if (type is null)
        {
            errors.Add($"{where} field '{name}' type '{typeText}' is not one of "
                       + ExtensionEnums.FieldTypeNames);
            return null;
        }

        return new ExtensionField(name, type.Value, Str(table, "doc"));
    }

    private static ExtensionSite? ParseExtensionSite(TomlTable table, int index, string where,
        string pointFile, IReadOnlySet<string> fieldNames, TomlTable? vacancy, List<string> errors)
    {
        var siteId = Str(table, "id");
        if (siteId.Length == 0)
        {
            errors.Add($"{where} `[[extension.sites]]` #{index + 1} is missing `id`");
            return null;
        }

        var siteWhere = $"{where} site '{siteId}'";
        var kindText = Str(table, "kind");
        var kind = ExtensionEnums.ParseSiteKind(kindText);
        if (kind is null)
        {
            errors.Add($"{siteWhere} kind '{kindText}' is not one of {ExtensionEnums.SiteKindNames}");
            return null;
        }

        var siteFile = Str(table, "file").Length > 0 ? NormFilePath(ToStr(table["file"])) : pointFile;
        if (Str(table, "file").Length > 0) CheckFilePath(siteFile, siteWhere, errors);

        var anchor = Norm(RawStr(table, "anchor"));
        var place = Str(table, "place");
        if (kind == ExtensionSiteKind.Anchor)
        {
            if (anchor.Trim().Length == 0) errors.Add($"{siteWhere} is an anchor site and needs `anchor`");
            if (place is not ("before" or "after"))
                errors.Add($"{siteWhere} place '{place}' is not before or after");
        }
        else
        {
            if (anchor.Length > 0) errors.Add($"{siteWhere} is a {kind.Value.CatalogName()} site - drop `anchor`");
            if (place.Length > 0) errors.Add($"{siteWhere} is a {kind.Value.CatalogName()} site - drop `place`");
        }

        var template = RawStr(table, "template");
        if (template.Trim().Length == 0)
        {
            errors.Add($"{siteWhere} is missing `template`");
            return null;
        }

        // One registrant, one line, one site. A multi-line template would make
        // the marker comment, appended by the renderer, land on only the last
        // line, leaving the rest unidentifiable.
        if (template.Contains('\n') || template.Contains('\r'))
        {
            errors.Add($"{siteWhere} template spans lines - a site renders exactly one line "
                       + "per registrant, and the marker comment is appended to it");
            return null;
        }

        CheckPlaceholders(template, fieldNames, $"{siteWhere} template", errors);

        var vacancyTemplate = "";
        if (vacancy is null || !vacancy.TryGetValue(siteId, out var vacancyRaw))
        {
            errors.Add($"{siteWhere} has no `[extension.vacancy]` entry - every site needs one "
                       + "(the empty string means 'no line for this site'), because a ledger "
                       + "entry whose mod is absent still renders at every site");
        }
        else
        {
            vacancyTemplate = ToStr(vacancyRaw);
            if (vacancyTemplate.Contains('\n') || vacancyTemplate.Contains('\r'))
                errors.Add($"{siteWhere} vacancy template spans lines - same one-line rule as `template`");
            // a vacancy has no mod behind it, so the mod's field values are gone
            CheckPlaceholders(vacancyTemplate, new HashSet<string>(), $"{siteWhere} vacancy template", errors);
        }

        var indent = (int)(table.TryGetValue("indent", out var indentRaw)
            ? Convert.ToInt64(indentRaw, CultureInfo.InvariantCulture)
            : 0);
        if (indent < 0)
        {
            errors.Add($"{siteWhere} indent {indent} is negative");
            indent = 0;
        }

        // The marker-comment leader. An append site may target a TOML file
        // (the schedule), where a `//` marker is a syntax error, so the
        // leader is explicit, "#" allowed on append sites only. Non-append
        // sites splice into GML and stay `//`.
        var comment = Str(table, "comment");
        if (comment.Length == 0) comment = "//";
        if (comment is not ("//" or "#"))
            errors.Add($"{siteWhere} comment '{comment}' is not // or #");
        else if (comment == "#" && kind != ExtensionSiteKind.Append)
            errors.Add($"{siteWhere} states comment '#' - only append sites may target "
                       + "non-GML files; anchor and enum_member sites splice into GML, where "
                       + "the marker must be a // comment");

        return new ExtensionSite(siteId, kind.Value, siteFile, anchor, place, template, indent,
            vacancyTemplate, comment);
    }

    // Loader-checkable rules only. Collision with an existing archive entry is
    // a stage-time check (the loader has no pristine source) and lives in the
    // expander.
    private static ExtensionVacancyFile? ParseExtensionVacancyFile(TomlTable table, int index, string where,
        List<string> errors)
    {
        var fileWhere = $"{where} `[[extension.vacancy_files]]` #{index + 1}";
        var path = Str(table, "path");
        if (path.Length == 0)
        {
            errors.Add($"{fileWhere} is missing `path`");
            return null;
        }

        // the path carries placeholders, so check the shape of what it renders
        // to rather than the template, because a symbol is a plain identifier by
        // construction, so a stand-in proves the surrounding path is safe
        var probe = NormFilePath(RenderProbe(path));
        var problem = PathSafety.PathProblem(probe, $"{fileWhere} `path`");
        if (problem is not null) errors.Add(problem);

        // A vacancy file is data, a fiddle prototype standing in for an
        // uninstalled mod. Under assets/gml/ it would land in the compile
        // gate's collection and be handed to momi-gml-check as if it were
        // source. The gate filters by extension too, but a stub belongs
        // nowhere near the GML tree in the first place, so refuse it here
        // where the error can say so.
        if (probe.StartsWith("assets/gml/", StringComparison.Ordinal))
            errors.Add($"{fileWhere} `path` '{path}' is under assets/gml/ - vacancy files are "
                       + "data, not source; a data file in the GML tree reaches the compile gate");

        CheckPlaceholders(path, new HashSet<string>(), $"{fileWhere} path", errors);

        var content = RawStr(table, "content");
        if (content.Trim().Length == 0) errors.Add($"{fileWhere} is missing `content`");
        CheckPlaceholders(content, new HashSet<string>(), $"{fileWhere} content", errors);

        return new ExtensionVacancyFile(path, content);
    }

    // A file a registration must ship alongside itself. Existence checks only.
    // The other two lints are content-shaped ("the object value appears in an
    // object_create call", "the companion toml declares a spring outfit key"),
    // neither is expressible as a path. Both now live as targeted advisory
    // checks in ExtensionCollector (CheckObjectCreation / CheckNpcRosterOutfits)
    // - code, not schema. A generic content-check language for two advisory
    // rules remains the wrong thing to build.
    private static ExtensionCompanion? ParseExtensionCompanion(TomlTable table, int index, string where,
        List<string> errors)
    {
        var companionWhere = $"{where} `[[extension.companions]]` #{index + 1}";
        var path = Str(table, "path");
        if (path.Length == 0)
        {
            errors.Add($"{companionWhere} is missing `path`");
            return null;
        }

        // mod-relative, so it is checked as it stands rather than under
        // assets/, the same PathSafety rule mod gml placement uses
        var problem = PathSafety.PathProblem($"assets/{RenderProbe(path)}", $"{companionWhere} `path`");
        if (problem is not null) errors.Add(problem);
        CheckPlaceholders(path, new HashSet<string>(), $"{companionWhere} path", errors);

        var levelText = Str(table, "level");
        var level = ExtensionEnums.ParseCompanionLevel(levelText);
        if (level is null)
        {
            errors.Add($"{companionWhere} level '{levelText}' is not one of "
                       + ExtensionEnums.CompanionLevelNames);
            return null;
        }

        // the doc is the message a mod author reads when the check fires, so a
        // companion without one reports a missing file and no reason
        var doc = Str(table, "doc");
        if (doc.Length == 0)
        {
            errors.Add($"{companionWhere} is missing `doc` - it becomes the message a mod "
                       + "author sees when the check fires, and 'file missing' alone does not "
                       + "say why the file matters");
            return null;
        }

        return new ExtensionCompanion(path, level.Value, doc);
    }

    // Every {{placeholder}} must be a declared field or one of the two the
    // expander always supplies. An undeclared one would render literally into
    // engine code, which compiles to garbage rather than failing loudly.
    private static void CheckPlaceholders(string template, IReadOnlySet<string> fieldNames, string where,
        List<string> errors)
    {
        List<string> unknown = [];
        foreach (Match match in ExtensionPlaceholders.Regex.Matches(template))
        {
            var name = match.Groups[1].Value;
            if (name is ExtensionPlaceholders.Symbol or ExtensionPlaceholders.Ordinal) continue;
            if (fieldNames.Contains(name)) continue;
            if (!unknown.Contains(name)) unknown.Add(name);
        }

        if (unknown.Count == 0) return;
        var allowed = fieldNames.Count > 0
            ? $"declared fields ({string.Join(", ", fieldNames.Order(StringComparer.Ordinal))}) plus "
            : "";
        errors.Add($"{where} references unknown placeholder(s) "
                   + $"{string.Join(", ", unknown.Select(u => $"{{{{{u}}}}}"))} - allowed here: "
                   + $"{allowed}{{{{symbol}}}} and {{{{ordinal}}}}");
    }

    // A rendered stand-in for a path template, for the PathSafety shape check.
    private static string RenderProbe(string template) => ExtensionPlaceholders.Regex.Replace(template, "x");

    // Cross-check a hand-written replace body against the hook declarations.
    // Every dispatcher call must name a declared hook of the dispatcher's kind
    // and be covered by the entry's own provides. An mmapi_apply_filters call
    // in statement position discards its result, which is a bug unless the
    // hook is declared in_place (handlers mutate the ctx instead).
    private static void LintReplace(SeamEntry entry, Dictionary<string, HookDeclaration> declarations,
        List<string> errors)
    {
        var where = $"[[{entry.Kind.CatalogName()}]] '{entry.Id}'";
        HashSet<string> dispatched = [];
        foreach (Match match in DispatchRegex.Matches(entry.Replace))
        {
            var fn = match.Groups[1].Value;
            var hookName = match.Groups[2].Value;
            dispatched.Add(hookName);
            if (!declarations.TryGetValue(hookName, out var declaration))
            {
                errors.Add($"{where} dispatches undeclared hook '{hookName}' via {fn}");
                continue;
            }

            var want = Dispatchers[fn];
            if (declaration.Kind != want)
                errors.Add($"{where} dispatches '{hookName}' via {fn} ({want.CatalogName()}), but the "
                           + $"hook is declared kind `{declaration.Kind.CatalogName()}`");
            if (entry.Kind == SeamEntryKind.Seam && !entry.Hooks.Contains(hookName))
                errors.Add($"{where} dispatches '{hookName}' but does not list it in `provides`");
        }

        if (entry.Kind == SeamEntryKind.EngineFix && dispatched.Count > 0)
            errors.Add($"{where} dispatches hooks ({string.Join(", ", dispatched.Order(StringComparer.Ordinal))}) - "
                       + "hook-feeding edits belong in [[seam]]");

        foreach (var line in entry.Replace.Split('\n'))
        {
            var statement = line.Trim();
            if (statement.StartsWith("try {", StringComparison.Ordinal))
                statement = statement["try {".Length..].Trim();
            if (!statement.StartsWith("mmapi_apply_filters(", StringComparison.Ordinal)) continue;

            var match = DispatchRegex.Match(statement);
            var hookName = match.Success ? match.Groups[2].Value : "?";
            if (declarations.TryGetValue(hookName, out var declaration) && declaration.InPlace) continue;
            errors.Add($"{where} discards the mmapi_apply_filters result for '{hookName}' - "
                       + "assign it, or declare the hook `in_place = true`");
        }
    }

    // Catalog order plus depends_on edges. A stable topological sort: each
    // round places the earliest catalog entry whose dependencies are all
    // placed, so entries the edges do not constrain keep their catalog order.
    private static List<SeamEntry> OrderEntries(List<SeamEntry> entries, List<string> errors)
    {
        var known = entries.Select(e => e.Id).ToHashSet();
        foreach (var entry in entries)
        {
            foreach (var dep in entry.DependsOn.Where(d => !known.Contains(d)))
                errors.Add($"[[{entry.Kind.CatalogName()}]] '{entry.Id}' depends_on unknown entry '{dep}'");
        }

        List<SeamEntry> ordered = [];
        HashSet<string> placed = [];
        var remaining = new List<SeamEntry>(entries);
        while (remaining.Count > 0)
        {
            var pick = remaining
                .FirstOrDefault(e => e.DependsOn.All(dep => placed.Contains(dep) || !known.Contains(dep)));
            if (pick is null)
            {
                errors.Add($"depends_on cycle involving '{remaining[0].Id}'");
                ordered.AddRange(remaining);
                break;
            }

            remaining.Remove(pick);
            placed.Add(pick.Id);
            ordered.Add(pick);
        }

        return ordered;
    }

    private static IReadOnlyList<string> ParseDependsOn(TomlTable table, string where, List<string> errors)
    {
        if (!table.TryGetValue("depends_on", out var raw)) return [];
        if (raw is not TomlArray array || array.Any(d => d is not string))
        {
            errors.Add($"{where} `depends_on` must be an array of entry-id strings");
            return [];
        }

        return array.Cast<string>().Select(d => d.Trim()).ToList();
    }

    // A catch variable is rendered as `catch (VALUE)`, and ctx_filter also
    // renders `catch (VALUE_field)`, so it has to be a plain GML identifier.
    private static void CheckCatchVar(string value, string where, List<string> errors)
    {
        if (!GmlIdentRegex.IsMatch(value))
            errors.Add($"{where} catch_var '{value}' is not a plain GML identifier - "
                       + $"it is rendered verbatim as `catch ({value})`");
    }

    // A marker is an identity token searched for verbatim, not an identifier:
    // the shipped catalog legitimately uses a whole statement as one. What it
    // may never carry is a line break - a template seam renders it as the
    // trailing `// marker` comment on its payload.
    private static void CheckMarker(string value, string where, List<string> errors)
    {
        if (value.Contains('\n') || value.Contains('\r'))
            errors.Add($"{where} marker '{value}' spans lines - a marker is rendered "
                       + "as a trailing `// marker` comment, so the rest of it would "
                       + "become code");
    }

    // The normalised `file`, proven safe to join under the install root. The
    // write phase joins every staged key onto the install, so the invariant is
    // stated here, in the loader's batched error list.
    private static void CheckFilePath(string entryFile, string where, List<string> errors)
    {
        var problem = PathSafety.PathProblem(entryFile, $"{where} `file`");
        if (problem is not null) errors.Add(problem);
    }

    private static IEnumerable<(TomlTable Table, int Index)> Tables(TomlTable doc, string key)
    {
        if (!doc.TryGetValue(key, out var raw) || raw is not TomlTableArray tables) yield break;
        for (var i = 0; i < tables.Count; i++) yield return (tables[i], i);
    }

    private static string Norm(string text) => text.Replace("\r\n", "\n");

    // Catalog `file` values are repo-style ("gml/objects/..."). Zip entries
    // carry the "assets/" prefix. Accept both.
    private static string NormFilePath(string raw)
    {
        var path = raw.Replace('\\', '/').TrimStart('/');
        return path.StartsWith("assets/", StringComparison.Ordinal) ? path : $"assets/{path}";
    }

    // Absent, empty and whitespace-only string fields all read as missing; the
    // one shared read helper keeps that rule from diverging check by check.
    private static string Str(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) ? ToStr(value).Trim() : "";

    private static string RawStr(TomlTable table, string key, string fallback = "") =>
        table.TryGetValue(key, out var value) ? ToStr(value) : fallback;

    private static bool Flag(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) && Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    private static string ToStr(object? value) =>
        value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
}
