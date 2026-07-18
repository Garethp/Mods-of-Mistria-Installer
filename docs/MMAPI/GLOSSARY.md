# Glossary

[← MMAPI](MMAPI.md)

Plain-language definitions of the terms this documentation uses.

| Term | Meaning |
| ---- | ------- |
| **hook** | A named moment the game exposes, like `game.day_started`. A mod registers a handler against it. |
| **handler** | A mod function the framework calls when a hook dispatches. |
| **kind** | A hook's dispatch style: event, filter, guard, or override. It fixes what the handler receives and what its return value means. |
| **event** | A kind. React to a moment; the return value is ignored. An individual hook may document context fields handlers can mutate. |
| **filter** | A kind. Normally receive a value and return a replacement, or `undefined` to keep it. A hook declared **in place** instead requires mutation and discards the return. |
| **guard** | A kind. Return the Boolean value `false` to veto an action; `undefined`, `true`, numeric zero, other values, and errors allow it. |
| **override** | A kind. Return a value to replace the engine's answer; the first non-`undefined` result wins. |
| **directive** | The `mmapi_*` function that registers a handler for a kind: `mmapi_on`, `mmapi_filter`, `mmapi_guard`, `mmapi_override`. |
| **ctx** | The context struct a handler receives, carrying the moment's data. Its fields come from the hook's catalog `doc` line. |
| **contention** | For override hooks, whether one mod (**exclusive**) or many (**claim-scoped**) are expected to register. |
| **seam** | A verified edit to one engine script that makes one or more hooks fire. Authored and shipped with MOMI; mod packages never carry them. |
| **seam catalog** | The TOML file, `seams.toml`, that declares every hook and seam. Also called the catalog. |
| **provider** | What makes a hook fire. Most are `seam`-provided; a `runtime` hook is emitted by MMAPI itself without an engine edit. |
| **anchor** | Exact text that must occur once before a text seam or template context locator can apply. CRLF is normalized to LF for matching. |
| **locator** | The full rule that identifies an edit site: an exact context/anchor or a structural target. |
| **structural target** | A function name targeted at `head` with no anchor, or at `before` / `after` with one required token anchor. Matching ignores whitespace and comments but still requires one unambiguous function and site. A wrap targets the whole function by name. |
| **template seam** | A seam whose operation and fields let MOMI render the dispatch. The six ops are `emit`, `guard`, `filter`, `filter_call`, `ctx_filter`, and `wrap`. |
| **text seam** | A seam that replaces one exact snippet with another. Used when the generated operations cannot express the engine-specific control flow. |
| **marker** | A unique identity token present in a staged seam or engine fix. It catches collisions; it is not an idempotency guard. |
| **depends_on** | Catalog entry ids that must apply first. MOMI uses them to perform a stable topological order. |
| **engine fix** | An exact engine edit with no hook contract and no dispatch. Reserved for framework plumbing or a deliberate hook-less correction. |
| **call rewrite** | A tree-wide redirect of direct calls from one native function name to an MMAPI wrapper, used when no GML body exists to seam. |
| **pristine source** | The untouched game archive used as the only base for seam matching and GML staging. |
| **staged tree** | The in-memory future GML tree after earlier edits have composed but before the archive rebuild is written. |
| **compile gate** | The checker pass over the staged framework and each mod. A shared failure aborts; an individual mod failure skips that mod. |
| **boot** | A mod's top-level loading phase while the game is still starting. Memory-only: no live player or room, and no file IO. |
| **tick** | A function registered with `mmapi_register`, run every frame from the first frame on. The first safe moment for file IO. |
| **install namespace** | The script directory a mod's `gml/` tree installs into, derived from the manifest's `author` and `name`. There is no separate manifest id field. See [The Manifest](MANIFEST.md). |

See [Hooks](HOOKS.md), [Seams](SEAMS.md), and [Mod Anatomy](MOD_ANATOMY.md) for the full treatment of each.
