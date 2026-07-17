# Hook: local.missing

Change what a missing localization key resolves to.

`local.missing` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `mmapi_local_get()` only when the native localizer misses: `local_get()` returned `undefined`, echoed the key back, or returned the engine's `"MISSING"` sentinel (`"PLACEHOLDER"` is a hit: the key exists with pending text). The filtered value starts `undefined`. ctx is `{ key, fallback }`, `fallback` being what the engine would have surfaced.

Return the text to inject for keys the base game lacks (computed strings, or fail-soft for keys that should have shipped as data). Return `undefined` to keep the engine fallback unchanged. Filter, not override, on purpose: many mods legitimately register here, each owning its key prefix, and later filters see earlier injections.

Static keys should ship as localization data through the normal registration path. This hook is the runtime complement, and the ctx struct is only allocated on the miss path.

| | |
| --- | --- |
| **Fires** | Inside `mmapi_local_get()`, only when the native localizer misses. |
| **Value** | Starts `undefined`. Later filters see text earlier filters injected. |
| **ctx** | `{ key, fallback }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `key` - the lookup key the native localizer missed.
- `fallback` - what the engine would have surfaced without an injection: `undefined`, the key echoed back, or the `"MISSING"` sentinel.

> [!NOTE]
> Static keys should ship as localization data through the normal registration path. This hook is the runtime complement for computed strings and fail-soft fallbacks. The ctx struct is only allocated on the miss path, so the hit path stays allocation-free.

## Usage

```gml
// local.missing is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the current value.
function lorekeeper_local_missing(_value, _ctx) {
    // _value starts undefined and holds whatever an earlier mod's filter
    // already injected for this key - undefined is the normal state here,
    // so do not blanket early-return on it.
    // _ctx is { key, fallback }.
    //   .key      - the lookup key the native localizer missed.
    //   .fallback - what the engine would have surfaced: undefined, the key
    //               echoed back, or "MISSING".
    if (!is_string(_ctx.key)) return undefined;
    if (string_pos("lorekeeper/", _ctx.key) != 1) return undefined; // not our prefix
    // computed text for a key you own:
    return "A tale not yet written: " + _ctx.key;
}

mmapi_filter("local.missing", lorekeeper_local_missing);
```

## Engine Wiring

- Call rewrite [`local_get_dispatch`](../seams/local_get_dispatch.md) token-rewrites every GML `local_get()` call site to `mmapi_local_get()`. The native builtin has no GML body to seam.
- Dispatch lives in the framework's `mmapi/mmapi_local.gml`: `mmapi_local_get()` calls the native localizer, detects the three miss shapes, runs this chain, then hands the result (injected or not) to [local.get](local.get.md).

## See Also

- [local.get](local.get.md) - Reword any localized text the game looks up, hit or miss.
