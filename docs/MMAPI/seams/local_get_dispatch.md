# Call Rewrite: local_get_dispatch

Reroutes every GML `local_get()` call through the framework's localisation waist.

`local_get_dispatch` is the catalog's one **call rewrite** (`[[call_rewrite]]`), a token rewrite of every call site of a native builtin onto a framework wrapper. It feeds [local.get](../hooks/local.get.md) and [local.missing](../hooks/local.missing.md), whose dispatch lives in `mmapi/mmapi_local.gml`. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **Callee** | `local_get` - a native builtin (fabricator extension) with no GML body |
| **Rewritten to** | `mmapi_local_get()` in `mmapi/mmapi_local.gml` |
| **Call sites** | 107 calls on 102 lines across 35 files at catalog time, every one a direct single-argument call |
| **Excluded** | `scripts/mmapi/` - the wrapper's own native call survives |
| **Feeds** | [`local.get`](../hooks/local.get.md), [`local.missing`](../hooks/local.missing.md) |

## The Edit

`local_get` is a native builtin: the pristine tree has no GML body for it, so no template, target, text, or wrap form can reach inside it, and a wrap has nothing to rename. The waist is created instead by redirecting every GML call site to `mmapi_local_get()`. The match is token-level (the identifier `local_get` immediately applied with an open paren) so the distinct identifiers `local_get_info`, `local_get_pronouns`, and `local_get_default_pronouns` can never match. At catalog time that is 107 calls on 102 lines across 35 files, every one a direct single-argument call, none in member-access or reference position (verified against pristine). The rewrite is applied after all `[[seam]]`/`[[engine_fix]]` edits, so anchored seams keep matching pristine text, and call tokens inside seam-injected text are rewritten too, the correct semantics. `scripts/mmapi/` is excluded, so the wrapper's own native call survives and a handler calling `local_get` hits the native directly, with no re-entry.

`mmapi_local_get(key)` calls the native `local_get(key)`, then decides whether the localizer missed. A miss is one of three results: `undefined`, the key echoed back, or the `"MISSING"` sentinel (`"PLACEHOLDER"` is a hit, the key exists with pending text, so `local.missing` does not fire for it). On a miss it dispatches `local.missing` with the value starting `undefined` and ctx `{ key, fallback }`, `fallback` being what the engine would have surfaced. A non-`undefined` return replaces the text, and the ctx struct is allocated only on this miss path. Then, on every lookup, it dispatches `local.get` with the resolved text as the value and the key itself as ctx (no allocation) after miss handling, so text injected by `local.missing` is visible to `local.get` filters. A non-string key forwards to the native unchanged. `==` across types is falsy and structs compare by reference, so the miss comparisons tolerate it.

Zero handlers means behavioral (not byte) equivalence, the wrap tradeoff writ wide: each lookup costs one extra call frame, at most three comparisons, and `mmapi_apply_filters`' empty-registry early-outs. In exchange, engine updates that add `local_get` call sites are covered automatically. Handler errors are isolated by the try/catch around each dispatch inside the wrapper, the same protection the catalog injects at engine dispatch sites.

## See Also

- [local.get](../hooks/local.get.md) - This hook fires on every localization lookup, after miss handling.
- [local.missing](../hooks/local.missing.md) - This hook fires only when the native localizer misses.
