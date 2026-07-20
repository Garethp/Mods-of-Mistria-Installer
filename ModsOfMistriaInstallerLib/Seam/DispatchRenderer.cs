namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The six template-form operations. filter_call is the in_place filter shape:
// the struct rides in the value position and the result is discarded.
public enum DispatchOp
{
    Emit,
    Guard,
    Filter,
    FilterCall,
    CtxFilter,
    Wrap,
}

public static class DispatchOps
{
    public const string Names = "emit, guard, filter, filter_call, ctx_filter, wrap";

    public static DispatchOp? Parse(string value) => value switch
    {
        "emit" => DispatchOp.Emit,
        "guard" => DispatchOp.Guard,
        "filter" => DispatchOp.Filter,
        "filter_call" => DispatchOp.FilterCall,
        "ctx_filter" => DispatchOp.CtxFilter,
        "wrap" => DispatchOp.Wrap,
        _ => null,
    };

    public static string CatalogName(this DispatchOp op) => op switch
    {
        DispatchOp.Emit => "emit",
        DispatchOp.Guard => "guard",
        DispatchOp.Filter => "filter",
        DispatchOp.FilterCall => "filter_call",
        DispatchOp.CtxFilter => "ctx_filter",
        DispatchOp.Wrap => "wrap",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };
}

// The Python reference's keyword surface for one payload render. Marker and
// catch_var default from the seam id upstream, so converted entries keep
// their historical, already-shipped tokens.
public record PayloadOptions(string Hook, int Indent, string Marker, string CatchVar)
{
    public string Ctx { get; init; } = "undefined";

    public IReadOnlyList<(string Key, string Value)>? CtxFields { get; init; }

    public string Var { get; init; } = "";

    public string Value { get; init; } = "";

    public string OnVeto { get; init; } = "";

    public IReadOnlyList<string>? Writeback { get; init; }

    public string CtxVar { get; init; } = "";

    public bool TryCatch { get; init; } = true;  // false for sites predating the convention

    public bool BlankBefore { get; init; }

    public bool BlankAfter { get; init; }
}

// Renders the generated dispatch payloads for template-form seams: one
// canonical shape per hook kind, so the try/catch wrapping, the marker comment
// and the catch variable stop being hand-uniquified per seam. When marker and
// catch_var coincide the trailing marker comment is dropped (the catch
// variable is the marker).
public static class DispatchRenderer
{
    public const string OrigPrefix = "__mmapi_orig_";

    public static string DefaultMarker(string seamId) => $"mmapi_{seamId}";

    public static string DefaultCatchVar(string seamId) => $"__mmapi_{seamId}";

    // op → the hook kind its dispatcher serves
    public static HookKind OpKind(DispatchOp op) => op switch
    {
        DispatchOp.Emit => HookKind.Event,
        DispatchOp.Guard => HookKind.Guard,
        DispatchOp.Filter => HookKind.Filter,
        DispatchOp.FilterCall => HookKind.Filter,
        DispatchOp.CtxFilter => HookKind.Filter,
        DispatchOp.Wrap => HookKind.Filter,
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    // The injected GML for one template-form seam, newline-terminated. The
    // caller places it between context_before and context_after.
    public static string RenderPayload(DispatchOp op, PayloadOptions options)
    {
        var i = new string(' ', options.Indent);
        var tag = options.Marker == options.CatchVar ? "" : $" // {options.Marker}";

        string body;
        if (op == DispatchOp.Emit && options.CtxFields is { Count: > 0 })
        {
            List<string> lines =
            [
                $"{i}try {{",
                $"{i}    mmapi_emit(\"{options.Hook}\", {{{tag}",
            ];
            lines.AddRange(options.CtxFields.Select(f => $"{i}        {f.Key}: {f.Value},"));
            lines.Add($"{i}    }});");
            lines.Add($"{i}}} catch ({options.CatchVar}) {{}}");
            body = string.Join("\n", lines) + "\n";
        }
        else if (op == DispatchOp.Emit)
        {
            body = Wrap(i, $"mmapi_emit(\"{options.Hook}\", {options.Ctx});", options, tag);
        }
        else if (op == DispatchOp.Guard)
        {
            body = Wrap(i, $"if (mmapi_check_guards(\"{options.Hook}\", {options.Ctx}) == false) "
                           + $"{{ {options.OnVeto} }}", options, tag);
        }
        else if (op == DispatchOp.Filter)
        {
            body = Wrap(i, $"{options.Var} = mmapi_apply_filters(\"{options.Hook}\", {options.Var}, "
                           + $"{options.Ctx});", options, tag);
        }
        else if (op == DispatchOp.FilterCall)
        {
            body = Wrap(i, $"mmapi_apply_filters(\"{options.Hook}\", {options.Value}, {options.Ctx});",
                options, tag);
        }
        else if (op == DispatchOp.CtxFilter)
        {
            body = RenderCtxFilter(i, options, tag);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(op), op, "not a rendered dispatch op");
        }

        return (options.BlankBefore ? "\n" : "") + body + (options.BlankAfter ? "\n" : "");
    }

    // The filter statement a wrap's generated wrapper carries. Rendered at
    // load time for the lint, and embedded verbatim into the wrapper at apply
    // time.
    public static string WrapFilterLine(string hook, string ctx, string marker, string catchVar)
    {
        var tag = marker == catchVar ? "" : $" // {marker}";
        return $"try {{ __mmapi_wrap_result = mmapi_apply_filters(\"{hook}\", "
               + $"__mmapi_wrap_result, {ctx}); }} catch ({catchVar}) {{}}{tag}";
    }

    // The generated wrapper for one wrapped function, newline-terminated. The
    // caller renames the pristine definition to __mmapi_orig_<name> and places
    // this right after its body. Internal and external call sites both resolve
    // to the wrapper, since only the wrapper still carries the original name.
    public static string RenderWrap(FunctionForm form, string name, string parameters,
        IReadOnlyList<string> args, string indentText, string filterLine,
        bool blankBefore = false, bool blankAfter = false)
    {
        var i = indentText;
        var orig = OrigPrefix + name;
        var call = string.Join(", ", args);
        var target = form is FunctionForm.Static or FunctionForm.Assign
            ? $"self.{orig}({call})"
            : $"{orig}({call})";
        var head = form switch
        {
            FunctionForm.Static => $"static {name} = function({parameters})",
            FunctionForm.Assign => $"{name} = function({parameters})",
            _ => $"function {name}({parameters})",
        };
        var body = string.Join("\n",
        [
            $"{i}{head} {{",
            $"{i}    var __mmapi_wrap_result = {target};",
            $"{i}    {filterLine}",
            $"{i}    return __mmapi_wrap_result;",
            $"{i}}}",
        ]) + "\n";
        return (blankBefore ? "\n" : "") + body + (blankAfter ? "\n" : "");
    }

    private static string Wrap(string i, string statement, PayloadOptions options, string tag)
    {
        if (options.TryCatch)
            return $"{i}try {{ {statement} }} catch ({options.CatchVar}) {{}}{tag}\n";
        return $"{i}{statement} // {options.Marker}\n";
    }

    // The pack/filter/write-back block. Each field is packed from its
    // expression, the struct is filtered, and every write-back lands under its
    // own try/catch so a handler returning a partial struct keeps the engine
    // values for the rest.
    private static string RenderCtxFilter(string i, PayloadOptions options, string tag)
    {
        List<string> lines = [$"{i}var {options.CtxVar} = {{"];
        lines.AddRange((options.CtxFields ?? []).Select(f => $"{i}    {f.Key}: {f.Value},"));
        lines.Add($"{i}}};");
        lines.Add($"{i}try {{ {options.CtxVar} = mmapi_apply_filters(\"{options.Hook}\", "
                  + $"{options.CtxVar}, {options.Ctx}); }} catch ({options.CatchVar}) {{}}{tag}");
        lines.Add($"{i}if ({options.CtxVar} != undefined) {{");
        lines.AddRange((options.Writeback ?? []).Select(name =>
            $"{i}    try {{ {name} = {options.CtxVar}.{name}; }} catch ({options.CatchVar}_{name}) {{}}"));
        lines.Add($"{i}}}");
        return string.Join("\n", lines) + "\n";
    }
}
