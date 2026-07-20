# Hook: local.get

Reword any localized text the game looks up.

`local.get` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires on every engine localization lookup: the localizer (`local_get`) is a native builtin with no GML body, so the framework token-rewrites every GML `local_get()` call site through `mmapi_local_get()`, the one GML point that sees every lookup. Those call sites include static UI via `Node.set_key`, dialogue via the Mist `__get_localization_value` binding and the textbox, toasts, the `{Local}` format specifier, item names and descriptions.

The filtered value is the resolved text, after the [local.missing](local.missing.md) chain has run on a miss. ctx is the lookup key exactly as the engine passed it (usually a key string like `"misc_local/warning"`, but `local_wrap()`-wrapped raw text and non-string values pass through here too, so exact-match the keys you own and return `undefined` otherwise). Return the replacement string. Return `undefined` to keep the current value.

Resolved UI text is cached on the text node (`Node.set_key` early-outs on an unchanged key) and re-resolves through this hook on language change, so lookups run at interaction and menu-build frequency with bursts when list menus build. Keep handlers cheap, and prefer language-aware text (`local_language()`) since debug validators temporarily force `eng`.

| | |
| --- | --- |
| **Fires** | On every engine localization lookup, inside `mmapi_local_get()`. |
| **Value** | The resolved text, after the `local.missing` chain has run on a miss. |
| **ctx** | The lookup key, exactly as the engine passed it. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- The lookup key as the engine passed it. This is usually a key string like `"misc_local/warning"`, but `local_wrap()`-wrapped raw text and non-string values pass through here too. Exact-match the keys you own and return `undefined` for everything else. Passing the key itself as ctx means no struct is allocated per lookup.

> [!IMPORTANT]
> Hot path. Lookups run at interaction and menu-build frequency, with bursts when list menus build. Make the callback's first check its cheapest early-exit, and return `undefined` for every key you do not own.

> [!NOTE]
> A handler that calls `local_get()` itself hits the native localizer directly. `scripts/mmapi/` is excluded from the call rewrite, so there is no re-entry.

## Usage

```gml
// local.get is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function tidy_text_local_get(_value, _ctx) {
    // _value is the resolved text. The local.missing chain has already run
    // on a miss, so it may be injected text - or still undefined - and
    // undefined is meaningful here: match on the key, not the value.
    // _ctx is the lookup key exactly as the engine passed it: usually a key
    // string like "misc_local/warning", but local_wrap()-wrapped raw text
    // and non-string values pass through here too.
    // HOT PATH: exact-match the keys you own, cheapest check first.
    if (_ctx == "tidy_text/sign_greeting") {
        // prefer language-aware text: debug validators temporarily force eng.
        if (local_language() == "eng") return "Welcome to the tidy farm!";
    }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("local.get", tidy_text_local_get);
```

## Engine Wiring

- Call rewrite [`local_get_dispatch`](../seams/local_get_dispatch.md) token-rewrites every GML `local_get()` call site to `mmapi_local_get()`. The native builtin has no GML body to seam, so the rewrite is the waist that sees every lookup.
- Dispatch lives in the framework's `mmapi/mmapi_local.gml`: `mmapi_local_get()` calls the native localizer, runs the `local.missing` chain on a miss, then filters the result through `local.get`, so injected text is visible to this hook.

## See Also

- [local.missing](local.missing.md) - Change what a missing localization key resolves to.
- [dialogue.line](dialogue.line.md) - Reword a dialogue line at the textbox instead of the localizer.
- [item.display_description](item.display_description.md) - Reword the tooltip description an item renders.
