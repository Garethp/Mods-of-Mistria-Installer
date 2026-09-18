# Seams

[← MMAPI](MMAPI.md)

A **seam** is a small, verified edit to one engine script that makes one or more hooks fire. It inserts a single dispatch call at the precise point in the game's own GML where the hook's moment or value lives, and the call matches the hook's kind (see [Choose The Hook Contract](#2-choose-the-hook-contract)). With no handlers registered, every seam leaves the game behaving as vanilla.

> [!IMPORTANT]
> **A mod package never carries its own seams.** Mods register handlers for the hooks MOMI already ships. Seams are authored in MOMI's catalog by framework contributors, then verified against the pristine game before release. If you only want to write a mod, use the [Catalog](CATALOG.md) and [Hooks](HOOKS.md). The contributor reference begins at [Authoring A Hook And Seam](#authoring-a-hook-and-seam).

## The Seam Catalog

The catalog is `ModsOfMistriaInstallerLib/Seam/Payload/seams.toml`, embedded into MOMI at build time. It uses schema version 2 and carries four record types:

| Record | What it declares |
| ------ | ---------------- |
| `[[hook]]` | A public hook name, kind, contract, provider, aliases, and any special dispatch semantics. |
| `[[seam]]` | An engine edit that helps provide one or more declared hooks. It may be generated from a template or written as a verbatim text replacement. |
| `[[engine_fix]]` | A hook-less engine edit. It applies like a text seam but dispatches nothing. |
| `[[call_rewrite]]` | A tree-wide redirect of direct calls to a native function that has no GML body to seam. |

The catalog opens with a `[counts]` table stating how many records of each type follow. The loader refuses a catalog whose records do not match that table, so a truncated or mis-merged file fails at load time instead of shipping a partial hook surface.

The shipped catalog currently declares **137 hooks**, fed by **153 seams**, **8 engine fixes**, and **1 call rewrite**. The [Catalog](CATALOG.md) gives each one its own page, under `docs/MMAPI/hooks/` for hooks and `docs/MMAPI/seams/` for the rest. The shipped-catalog test holds the count sentences on this page, the Catalog, and [Hooks](HOOKS.md) to those totals, and the page set to the catalog, so a stale count, a record without a page, or a page without a record fails the suite.

MOMI also renders the hook declarations into `mmapi_hook_catalog.gml` at install time, so the runtime can check registrations and answer introspection. See [The Installed Catalog](HOOKS.md#the-installed-catalog).

### Runtime Hooks

Some hooks use `provider = "runtime"`. The framework emits those itself, with no engine edit behind them. Three hooks are runtime-provided today:

- [combat.damage_injected](hooks/combat.damage_injected.md) fires from the framework's own `mmapi_deal_damage()`.
- [game.room_changed](hooks/game.room_changed.md) is derived by polling `room()`.
- [game.day_changed](hooks/game.day_changed.md) is derived by polling `total_days()`.

The two polled hooks lag the change they report, and the first poll of a session only records the baseline, so no event fires for the state a session starts in. Treat them as edge triggers. React to the change, but read the live state at any later decision point rather than caching their ctx (see the warning on [game.room_changed](hooks/game.room_changed.md)).

For the day boundary specifically, the in-engine [game.new_day](hooks/game.new_day.md) fires from `new_day()` itself, before the end-of-day autosave and never on a load. The derived `game.day_changed` lags both.

## How MOMI Applies The Catalog

MOMI stages the GML layer in memory against the pristine archive before it starts the rebuild. The layer is staged only when at least one selected mod contains a `gml/` tree.

1. MOMI loads and validates the catalog, then orders seams and engine fixes by catalog order plus `depends_on` edges.
2. It adds the framework sources, applies every seam and engine fix to pristine engine files, performs the tree-wide call rewrites, and renders `mmapi_hook_catalog.gml`.
3. It adds each mod's GML, checks export and install-namespace collisions, verifies `requires_hooks`, and runs the warning-tier lints.
4. It runs the compile gate when a checker backend is available. A release normally carries one, while an `Auto` development build logs and skips this pass when none resolves.
5. Only the completed plan is written into the whole-archive rebuild.

A text anchor or a pristine-context pair must occur exactly once in its file, with CRLF normalized to LF before the comparison. A structural target follows a different rule. It finds one named function, then optionally one token sequence inside it, and whitespace and comments never affect the match. Call rewrites run last across the whole engine GML tree.

Catalog or staging errors are reported as a batch where possible, so one run can show every stale locator rather than making a contributor repair them one at a time.

### What A Stale Catalog Costs

A normal install handles a catalog that no longer fits the game by skipping **every GML-bearing mod** and continuing with content-only mods. Because the archive is rebuilt from pristine, the previously installed GML layer is not kept. The installer log carries the full seam report, and each skipped mod gets the short "game GML changed" reason.

For development or CI, `--fail-on-skip` turns that outcome into a hard stop before the rebuild. Use the read-only [`--seam-check`](#check-against-the-real-game) before installing when testing a new game build.

## Authoring A Hook And Seam

This section is for contributors changing MOMI itself. You need the current pristine GML tree or its `assets.zip`, enough engine context to identify the real control-flow point, and a concrete mod that needs the capability.

### 1. Prove A New Hook Is Needed

Start with the mod-facing need, not the patch shape.

- If the mod wants to **do** something, can it call the engine directly? Giving an item, teleporting, playing a sound, and reading live state do not need hooks. See [Calling The Engine Directly](API_REFERENCE.md#calling-the-engine-directly).
- If the state is readable, can a cheap `mmapi_register` tick observe the change? Polling is often more durable than an engine edit.
- Does an existing hook already expose the moment or value, perhaps from another dispatch site? Search the [Catalog](CATALOG.md) before adding a near-duplicate.
- Is there a real consumer? A hook contract should be designed around behavior a mod needs now, not a speculative engine location.

A hook is warranted only when no direct call, tick, or existing hook covers the need, and a real mod is waiting on it. The engine controls the moment, and a mod needs to observe, transform, veto, or replace it.

### 2. Choose The Hook Contract

Choose the kind from what handlers are allowed to do:

| Intent | Kind | Engine-side dispatcher |
| ------ | ---- | ---------------------- |
| Publish a moment for handlers to react to | `event` | `mmapi_emit(name, ctx)` |
| Transform a value | `filter` | `value = mmapi_apply_filters(name, value, ctx)` |
| Veto an action | `guard` | `mmapi_check_guards(name, ctx)` |
| Replace an answer | `override` | `mmapi_run_override(name, ctx)` |

Each kind carries one more rule:

- An **event** is an event because handlers can only observe it, not because of when it fires. It may fire before, during, or after the engine action it reports, and the hook's contract states which.
- A **filter** must carry the value the engine will actually use, so a handler's replacement takes effect.
- A **guard** belongs before irreversible work, where a veto can still prevent the action.
- An **override** must state exactly which engine answer a non-`undefined` result replaces and what work the engine then skips.

The hook's name and contract outlive any one engine layout. Name the capability in dotted lowercase (`domain.moment_or_value`), not the function MOMI happens to patch today. Keep `ctx` as small and stable as the use case permits, state exactly when dispatch occurs, and spell out what every return value means.

### 3. Declare The Hook

Every public hook gets one `[[hook]]` record. Keep declarations together before the seam records for readability:

```toml
[[hook]]
name = "audio.play_guard"
kind = "guard"
doc  = "Fires at the top of TANGO.play(asset_name, ...), before a sound effect starts. ctx is { asset_name }. Return boolean false to suppress the sound; every other value allows."
```

| Field | Required | Meaning |
| ----- | -------- | ------- |
| `name` | yes | Dotted lowercase name. Each segment may contain lowercase letters, digits, and underscores. Names and aliases must be unique. |
| `kind` | yes | `event`, `filter`, `guard`, or `override`. It must agree with the seam's dispatcher or template op. |
| `doc` | yes for the shipped catalog | The complete public contract, covering timing, value, `ctx`, return behavior, and important frequency or edge cases. The shipped-catalog tests reject an empty doc. |
| `provider` | no | Defaults to `seam`. Use `runtime` only when framework GML emits the hook itself. A runtime hook must not also be listed by a seam or rewrite. |
| `aliases` | no | Old dotted names that resolve to this hook with a warning. An alias cannot collide with another hook or alias. |
| `in_place` | no | `true` for a filter whose dispatch return is deliberately discarded because handlers mutate the supplied value. The current examples are `combat.tarball_grid` and `ui.item_node`. |
| `contention` | overrides only | Required on every override and forbidden on other kinds. `exclusive` means rival providers conflict, and `claim-scoped` means handlers claim disjoint targets and defer elsewhere. |

A seam-provided hook must appear in at least one seam or call rewrite's provider list. A runtime-provided hook must appear in neither.

Every added or removed record also moves the `[counts]` integrity table at the top of the file. Bump `hooks` now, and bump `seams`, `engine_fixes`, or `call_rewrites` as the matching records land in step 4. The loader compares that table to the stanzas it parses and refuses the whole catalog on any mismatch, so a forgotten bump fails every shipped-catalog test and the seam check at once, before a single anchor is examined.

### 4. Choose The Smallest Seam Form

Start with a template seam. Move to text only when the generated forms cannot express the control flow.

| Need | Form |
| ---- | ---- |
| Insert one standard dispatch between stable pristine lines | [Template with `context_before` / `context_after`](#locator-pristine-context) |
| Insert at a named function's head or beside one statement | [Template with a structural `target`](#locator-structural-target) |
| Filter a function's return without rewriting all exits | [Template `wrap`](#locator-whole-function-wrap) |
| Capture locals, thread arguments, add an override, rewrite control flow, dispatch several hooks, or perform custom defensive write-back | [Text seam with `anchor` / `replace`](#text-seams) |
| Support another seam without dispatching at this site | [Text companion seam whose `provides` names the contract it helps implement](#text-seams) |
| Make a hook-less correction | [`[[engine_fix]]`](#engine-fixes) |
| Intercept every direct call to a native function with no GML body | [`[[call_rewrite]]`](#call-rewrites) |

The template and text forms are catalog syntax, not runtime categories. Both become one `SeamEntry` and obey the same file, marker, order, and fail-closed rules. The sections that follow describe each form in depth.

## Template Seams

A template seam states **where** a payload lands and **what standard dispatch** to generate. `provides`, `anchor`, and `replace` are derived and must not be written on the record.

This is the complete structural-target form of `audio_play_guard`:

```toml
[[seam]]
id      = "audio_play_guard"
file    = "gml/scripts/Tango.gml"
target  = { fn = "play", at = "head" }
op      = "guard"
hook    = "audio.play_guard"
ctx     = "{ asset_name: asset_name }"
on_veto = "return undefined;"
marker  = "mmapi_audio_run_play_guards"
```

It renders a line of this shape at the head of `play()`:

```gml
    try { if (mmapi_check_guards("audio.play_guard", { asset_name: asset_name }) == false) { return undefined; } } catch (__mmapi_audio_play_guard) {} // mmapi_audio_run_play_guards
```

### Locator: Pristine Context

State the original text once, split at the insertion point:

```toml
context_before = '''
function pick_node(grid, x_pos, y_pos, item, modifier, effect_override, doppel, is_burn=false) {
'''
context_after = '''
    var is_rug_pick = false;
'''
```

MOMI concatenates `context_before + context_after` into the pristine anchor and inserts the generated payload between them. At least one half must be nonempty. The concatenated text must occur exactly once in the file at the moment this entry applies. Matching is exact apart from CRLF normalization, so indentation and comments are part of the contract.

Use context when the nearby text is already a small, distinctive anchor or when the payload belongs inside a closure that has no useful name.

### Locator: Structural Target

A target first finds a function by name, then places the payload within its body:

```toml
target = { fn = "use_item", at = "after", anchor = "if !is_struct(item) { item = new LiveItem(item); }" }
```

| `at` | Placement |
| ---- | --------- |
| `head` | The first line inside the function body. Do not state `anchor`. |
| `before` | The line before the single token-anchor match inside the function. |
| `after` | The line after the single token-anchor match inside the function. |

The named function must be defined exactly once in the file. A `before` or `after` anchor must match exactly once **inside that function**. Structural matching works on GML tokens rather than raw text. Whitespace and comments never affect a match, and string contents and identifier names must agree exactly. Because an identifier is one token, an anchor cannot match the leading part of a longer name. For example, an anchor of `count` never matches `counter`.

Insertion is line-wise. The opening brace for `head` must finish its line, and a `before` or `after` match must own its first and last lines. If the target shares a line with other code, use a text seam instead.

### Locator: Whole-Function Wrap

A wrap target names only the function:

```toml
target = { fn = "choose_random_artifact" }
op     = "wrap"
hook   = "items.dig_artifact"
ctx    = "self"
```

MOMI renames the original definition to `__mmapi_orig_<fn>`, leaves its body untouched, and appends a new function under the original name. The wrapper calls the original with the same parameters and defaults, filters the return, and returns the result. The renderer handles ordinary declaration, `static name = function(...)`, and assignment forms after they pass real-tree verification. The generated wrapper does not preserve constructor, inherited-constructor, or `self.name = function(...)` shapes, so do not wrap those forms.

The function must be defined exactly once and must not call itself by name. A self-call would route back through the new wrapper after the rename and double-filter or recurse, so the stager rejects it. A wrap is always a filter. An event, guard, or override needs a dispatch inside the function body, which means a structural target or a text seam.

### The Six Template Ops

| `op` | Hook kind | Generated behavior | Required fields |
| ---- | --------- | ------------------ | --------------- |
| `emit` | event | Calls `mmapi_emit(hook, ctx)`. When `ctx_fields` is present, builds the struct across multiple lines. | none |
| `guard` | guard | Checks `mmapi_check_guards(hook, ctx)` and runs the supplied statement on veto. | `on_veto` |
| `filter` | filter | Reassigns one local or parameter through the chain, rendering `var = mmapi_apply_filters(...)`. | `var` |
| `filter_call` | in-place filter | Calls `mmapi_apply_filters(...)` and discards the return. The hook declaration must carry `in_place = true`. | `value` |
| `ctx_filter` | filter | Packs fields into a struct, filters it, then writes named fields back one by one under their own catches. Supported by the renderer, though no shipped seam currently uses it. | `ctx_var`, normally with `ctx_fields` and `writeback` |
| `wrap` | filter | Renames the original function and filters its return in a generated wrapper. | structural `target = { fn = "..." }` |

There is no template override op. Overrides normally need a temporary result and an early return with engine-specific semantics, so they use text seams.

### Template Field Reference

| Field | Default | Meaning |
| ----- | ------- | ------- |
| `id` | - | Unique entry id. Also seeds the default marker and catch variable. |
| `file` | - | Engine path such as `gml/scripts/Tango.gml`. `assets/gml/...` is also accepted, and backslashes normalize to slashes. |
| `op` | - | One of the six ops above. Its generated dispatcher must agree with the hook's declared kind. |
| `hook` | - | The one hook this template seam provides. |
| `context_before`, `context_after` | empty | Exact-text locator authored from pristine context. Use this pair or `target`, never both. |
| `target` | absent | Structural locator `{ fn, at, anchor }`, with the wrap exception described above. |
| `ctx` | `undefined` | GML expression passed as context. |
| `ctx_fields` | absent | Array of `["field", "GML expression"]` pairs used to build a context or filtered-value struct. |
| `var` | - | GML lvalue reassigned by `filter`. |
| `value` | - | Value passed to `filter_call`, whose return is discarded. |
| `on_veto` | - | GML statement run when a guard returns boolean `false`, such as `return;` or `destroy_instance(); return;`. |
| `ctx_var` | - | Temporary struct name used by `ctx_filter`. |
| `writeback` | empty | Field names `ctx_filter` copies from the filtered struct back to same-named GML lvalues. Each copy is isolated. |
| `indent` | `4` | Spaces before the generated payload. Match the surrounding engine source. |
| `try_catch` | `true` | Wrap a simple `emit`, `guard`, `filter`, or `filter_call` line in a site-level catch. `emit` with `ctx_fields`, `ctx_filter`, and `wrap` always render their required catches. |
| `marker` | `mmapi_<id>` | Distinctive identity token rendered on the generated payload. |
| `catch_var` | `__mmapi_<id>` | Plain GML identifier used by the generated catch. |
| `blank_before`, `blank_after` | `false` | Add one formatting-only blank line around the payload. |
| `depends_on` | empty | Entry ids that must apply first. See [Order And Dependencies](#order-and-dependencies). |

`ctx`, field expressions, lvalues, and veto statements are emitted as GML, not parsed as a second language. Inspect the rendered edit and run the compile check, because a TOML value can be well-formed while the GML it contains is not.

## Text Seams

A text seam replaces one exact snippet with another. Author the anchor from pristine source. The one exception is an area that an earlier same-file entry already changed, where the anchor must match the staged text as it stands when this entry applies (see [Order And Dependencies](#order-and-dependencies)).

Text matching is deliberately strict. Anchor against the smallest snippet that is unique and semantically meaningful, but include enough surrounding control flow that a game update that changes the site's logic makes the seam fail rather than land in the wrong place.

Use text when a template cannot state the edit clearly, which covers overrides, dispatching several hooks from one site, value capture, argument threading, control-flow changes, and custom defensive write-back.

```toml
[[seam]]
id   = "object_interact"
file = "gml/scripts/GameplaySystems/Data/Grid/GridActions/Interact.gml"
anchor = '''
function interact(node) {
'''
replace = '''
function interact(node) {
    var __mmapi_object_interact = mmapi_run_override("object.interact", node); // mmapi_object_interact_override
    if (__mmapi_object_interact != undefined) { return __mmapi_object_interact; }
'''
marker   = "mmapi_object_interact_override"
provides = ["object.interact"]
```

| Field | Required | Meaning |
| ----- | -------- | ------- |
| `id` | yes | Unique across seams, engine fixes, and call rewrites. |
| `file` | yes | The one engine file this edit changes. |
| `anchor` | yes | Exact original snippet, normalized from CRLF to LF. It must match once at apply time. |
| `replace` | yes | Complete replacement snippet, including the original code that must survive. It must differ from `anchor`. |
| `marker` | yes | Single-line identity token present in `replace` and absent from `anchor`. |
| `provides` | yes | Nonempty hook-name array. Every literal dispatcher in `replace` must be listed. The array may also name a hook this entry helps provide without dispatching it itself. |
| `depends_on` | no | Entry ids that must apply before this one. |

The loader inspects every literal call to one of the four dispatchers in `replace` and checks three things. The named hook must be declared, the dispatcher must match the hook's declared kind, and the seam's `provides` must list that hook. It also rejects a bare `mmapi_apply_filters(...)` statement unless the hook is declared `in_place`, because a filter result that is never assigned silently discards every replacement.

Beyond those checks, MOMI applies the replacement verbatim. It adds no try/catch, no marker comment, and none of the original code you leave out. Preserve every pristine statement that must survive, and write any site-level isolation the engine location requires directly in `replace` (see [Error Isolation And Zero-Handler Behavior](#error-isolation-and-zero-handler-behavior)).

## Markers

A marker identifies the edit in staged output. It is not an idempotency guard. Every install starts from pristine again, so there is never an earlier copy of the edit to detect.

- Markers are unique across seams and engine fixes.
- A marker must fit on one line.
- Text seams put it in `replace`, never `anchor`.
- A template marker normally appears as the trailing `// marker` comment. When marker and catch variable are the same token, the catch itself carries the identity and the duplicate comment is omitted.
- Staging rejects a marker found anywhere in the pristine file or in that file's already-staged text. Pick something globally distinctive, normally prefixed `mmapi_` or `__mmapi_`.

A marker collision with an earlier edit can also mean the entries are in the wrong order. Check `depends_on` before merely renaming it.

## Order And Dependencies

MOMI collects every `[[seam]]` before every `[[engine_fix]]`, even if the TOML records are interleaved. Within those groups, entries keep catalog order unless `depends_on` requires another order. The stable topological sort leaves unrelated entries in that collected order. Unknown dependency ids fail catalog loading, and cycles are rejected.

The shipped catalog file groups records by type under section banners, in the order hook declarations, seams, call rewrites, and engine fixes. New records go at the end of their type's section, and a shipped-catalog test enforces the layout. The grouping is presentational, and the loader semantics above do not depend on it.

Use a dependency in any of these cases:

- An edit refers to a local, wrapper, or code block that another edit introduces.
- An edit anchors against text that an earlier replacement re-emits.
- An edit must be present before a later structural insertion is resolved.
- An edit is a companion required for the hook's full behavior.

`depends_on` controls **application order**. Source order can differ. In particular, two structural `head` insertions both land immediately after the opening brace, so the later-applied payload appears above the earlier one at runtime. For example, `dialogue_play_guard` relies on this. It depends on `dialogue_path`, so it applies afterwards, and its guard therefore sits above the path dispatch and runs first in the function.

Do not add dependency edges as decoration. They become part of the catalog's update contract.

## Engine Fixes

An engine fix is the text form without hooks:

```toml
[[engine_fix]]
name    = "game_step_begin_installs"
file    = "gml/objects/Game.gml"
anchor  = '''...'''
replace = '''... mmapi_run_installs(); ...'''
marker  = "mmapi_run_installs();"
```

It uses `name`, `file`, `anchor`, `replace`, `marker`, and optional `depends_on`. It has no `provides`, and its replacement may not dispatch a hook. Use one only for framework plumbing or a deliberate engine correction that has no mod-facing handler contract. If mods can observe or influence the behavior, it belongs in a hook and seam instead.

## Call Rewrites

A call rewrite is the last resort for a native function with no GML definition to target. The shipped `local_get_dispatch` redirects every direct `local_get(...)` call to the framework's `mmapi_local_get(...)` wrapper:

```toml
[[call_rewrite]]
id       = "local_get_dispatch"
callee   = "local_get"
to       = "mmapi_local_get"
args     = 1
provides = ["local.get", "local.missing"]
```

| Field | Meaning |
| ----- | ------- |
| `id` | Unique catalog entry id. |
| `callee` | Plain identifier to find in direct call position. Only `callee(...)` matches. |
| `to` | Plain wrapper identifier that replaces it. The framework must define this function. |
| `args` | Non-negative integer giving the exact argument count every rewritten direct call must pass. |
| `provides` | Nonempty list of hooks dispatched by the wrapper. |

Call rewrites run after every seam and engine fix across all engine `.gml` files, so they also see calls introduced by replacements. Files under `assets/gml/scripts/mmapi/` are excluded, allowing the wrapper to reach the native function without re-entering itself.

The scanner ignores comments, strings, and longer sibling identifiers. It fails closed if the callee appears as a member call, gains a GML definition, is called with another arity, or has no direct call sites anywhere. Rewrites cannot share a callee, rewrite to themselves, or chain into another rewrite's callee. Bare references that do not call the function are outside this scan, so audit them manually before choosing a tree-wide rewrite.

One rewrite can touch dozens of files and automatically covers any direct call site a game update adds, so it must meet stricter requirements than a normal seam. The callee must be a stable native function with one proven arity, and the wrapper must be behaviorally equivalent with no handlers.

## Authoring A Runtime Hook

Not every hook needs an engine edit. If the framework owns the moment directly, or can derive it from state it already polls, declare `provider = "runtime"` and emit it from the appropriate `mmapi/*.gml` module. Do not add a placeholder seam. The loader rejects a runtime hook that any seam or rewrite claims to provide.

Runtime polling still needs an explicit timing contract. State whether the first poll records a baseline, which frame phase emits, and whether the event arrives too late to alter the engine action. The [runtime hook pages](hooks/game.room_changed.md) show the current pattern. The shipped runtime hooks and their timing behavior are listed under [Runtime Hooks](#runtime-hooks).

## Error Isolation And Zero-Handler Behavior

The generated template payload defaults to a site-level `try/catch`, and each dispatcher separately isolates every mod handler. Keep both layers:

- Dispatcher isolation protects one mod from another and applies the kind's failure rule.
- Site isolation protects the engine from a bad context expression, result assignment, or framework-site assumption around an ordinary dispatch. A `ctx_filter` builds its initial struct before entering its catch, so its field expressions must be safe on their own.

For a simple line op, `try_catch = false` emits that line bare. Existing entries use it only at tested sites whose dispatch line cannot fail on its own. In practice that means a ctx that cannot fail to construct, such as the player delta family's `{ player: self }`. It is not a performance switch. Leave it true for new seams unless the exact engine location requires otherwise and the reason is recorded on the seam page.

With zero handlers registered, a seam must leave the game behaving as vanilla. A template or text insertion can leave the original code around it byte-for-byte intact, so only the added dispatch line differs. A wrap or call rewrite changes the original code itself, by renaming a function or redirecting its callers, so it can only ever be behaviorally equivalent. Engine fixes are deliberate behavior changes and are the exception.

Check the zero-handler path explicitly:

- `emit` does nothing.
- `filter` returns the input unchanged.
- `guard` allows.
- `override` returns `undefined` and the engine continues.
- `filter_call` must only expose an in-place value whose untouched state already means pristine behavior.
- A wrap or call rewrite adds a call frame but must preserve the same answer and side effects.

For hot paths, keep context allocation out of the empty-registry path when hand-writing a seam where practical. Then document the frequency so handlers know to put their cheapest check first.

## Verifying A Catalog Change

Verification runs in layers, each described below.

### Load And Synthetic Stage

Run the test suite from the repository root:

```powershell
dotnet test ModsOfMistriaInstallerLibTests
```

The shipped-catalog tests load the real TOML, validate declarations and generated dispatches, check markers and dependencies, resolve framework calls, and stage against a pristine stand-in synthesized from the catalog's own anchors. This catches a malformed catalog without requiring game files. It cannot prove that a newly written anchor matches the real game.

### Check Against The Real Game

Point the CLI at the catalog under edit and a pristine game archive:

```powershell
$env:MOMI_SEAM_CATALOG = (Resolve-Path "ModsOfMistriaInstallerLib/Seam/Payload/seams.toml")
dotnet run --project ModsOfMistriaCommandLine -- --seam-check "C:\path\to\pristine-assets.zip"
```

`MOMI_SEAM_CATALOG` overrides the embedded resource. A set-but-missing path fails loudly, which prevents accidentally checking the last built copy. Without an explicit zip, `--seam-check` uses the located game's pristine backup (`assets.bak.zip`, or the legacy `assets_backup.zip`). The check stages the same seams and call rewrites as an install and writes nothing.

The check also sweeps the framework payload's own call names against the build. Every function the mmapi runtime calls must be defined in the engine tree, called by the engine tree, or defined by the framework. The middle case is how the sweep recognizes engine natives without a hand-kept list, since a name the game's own GML calls must exist at runtime.

A name resolving nowhere reports as a `framework` problem naming the payload file and line. This is what protects the runtime-provided hooks, whose wiring lives in the framework rather than in any seam. Member calls and bare global reads stay outside the sweep, and a call site whose line carries a `framework-reads-exempt` comment opts out, for the rare case where calling a possibly absent name is the point, such as a capability probe.

Exit codes are part of the command-line contract:

| Exit | Meaning |
| ---- | ------- |
| `0` | Every seam, engine fix, and call rewrite still fits. |
| `1` | At least one staging problem was found, of any kind in the [failure table](#reading-seam-check-failures). |
| `2` | No source archive could be located, or the named archive was missing. Other malformed input or catalog errors are reported as command failures outside this exit-code contract. |

For automation, substitute `--seam-check-json`. Its `problem_records` include `kind`, `entry_id`, `file`, `line`, `hint`, `message`, and `context`. When a closest source location is available, `context` is a numbered excerpt, and problem kinds without a source site leave it empty.

### Triage A Game Update

The seam check proves that every anchor still holds on a given build. It cannot see the opposite failure, where an anchor keeps holding while the code around it changes meaning. The seam diff covers that blind spot. It compares two pristine builds across every region the catalog anchors into and reports each one that changed.

```powershell
dotnet run --project ModsOfMistriaCommandLine -- --seam-diff "C:\old\assets.zip" "C:\new\assets.zip"
```

Given no archive paths, the diff uses the game install MOMI locates, with its pristine backup as the old side and its live `assets.zip` as the new side. A game update leaves exactly that pair behind, since the update replaces `assets.zip` while the backup still holds the previous build, so running the diff with no arguments shows what the update changed.

Both sides must be pristine. An archive that carries a catalog marker is a modded build, and the diff refuses it, because its regions would show MOMI's own edits instead of the game's. For the argument-free form, that means it only works immediately after a game update, while the live `assets.zip` is still pristine and `assets.bak.zip` still holds the previous game version.

> [!WARNING]
> After a game update, the next install overwrites the pristine backup with the new build, so run the diff before reinstalling.

The region compared for each entry depends on how it is located:

| Locator | Compared region |
| ------- | --------------- |
| Structural `target` | The target function. |
| Text anchor inside a function | The function enclosing the anchor, extended to cover the whole anchor where the anchor runs past it. |
| Text anchor at file top level | A fixed window of lines around the anchor, since no enclosing function exists. |

The comparison works on tokens, so indentation and comment changes never flag a region.

A changed region is review material, not a failure. The seam may still stage cleanly while its timing, value range, or context meaning drifted, which no structural check can detect. Review every changed region against the seam's promises and its hook's documented contract before trusting the catalog on the new build.

Diffs print as hunks with new-build line numbers. Each entry states its region's line range, and each elided stretch states how many unchanged lines it holds and, when more of the region follows, the line it resumes at, so a reviewer can open the new build at the right place without searching:

```text
seam-diff C:\old\assets.zip -> C:\new\assets.zip
  catalog: 146 regions compared
  RESULT: REVIEW - 41 region(s) changed, 3 missing

  - seam 'player_incoming_damage': the lines around the anchor in assets/gml/objects/characters/obj_ari.gml (lines 1801-1834): CHANGED
      ... (18 lines unchanged, resuming at 1819)
                          var damage_was_acid = has_flag(next_dmg.tarball.flags, CombatFlag.Acid);
                          var is_dead = ARI.modify_health(final_dmg, damage_was_acid == false);

    -                     if GS_MINES_RUN != undefined && GS_MINES_FLOOR != undefined && next_dmg.tarball.stats_entry != undefined {
    +                     if next_dmg.tarball.stats_entry != undefined {
                              next_dmg.tarball.stats_entry.damage_dealt += final_dmg;
                              next_dmg.tarball.stats_entry.damage_dealt_count += 1;
                          }
      ... (9 lines unchanged)
```

For automation, substitute `--seam-diff-json`. Each entry carries `status`, `region_start_line`, `region_lines`, and `diff`, and quotes `old_text` and `new_text` whole for regions small enough to carry.

| Exit | Meaning |
| ---- | ------- |
| `0` | No compared region changed between the builds. |
| `1` | At least one region changed or lost its locator and needs review. |
| `2` | Nothing valid to compare, because an archive is missing or either side is a modded build. |

### Real-Tree And Compile Tests

The local integration tests use a disposable copy of a pristine archive:

```powershell
$env:MOMI_PRISTINE_ZIP = "C:\path\to\pristine-assets.zip"
dotnet test ModsOfMistriaInstallerLibTests --filter "FullyQualifiedName~ShippedCatalogLocalTest|FullyQualifiedName~GmlLayerLocalTest"
```

`ShippedCatalogLocalTest` is the definitive locator, payload-reads, and call-rewrite check. `GmlLayerLocalTest` stages the framework and a mod, requires the compile gate, writes only to a temporary game copy, and proves uninstall returns that copy to pristine.

If the mandatory compile pass cannot find `momi-gml-check`, build the checker under `tools/checker` or point `MOMI_GML_CHECKER` at the binary. A set-but-missing override also fails loudly.

## Reading Seam-Check Failures

| Problem | What it means | First response |
| ------- | ------------- | -------------- |
| `anchor` | An exact text/context anchor matched zero or multiple times. | Read the hint and numbered excerpt. Re-anchor against the new pristine code rather than loosening it until it passes. |
| `target` | The named function or token anchor was missing, duplicated, or did not own its lines. | Confirm the function's new shape. Change the target, or use a text seam when the site is no longer line-addressable. |
| `wrap` | The target function now self-calls or otherwise cannot be wrapped safely. | Use a text seam or redesign the hook around a stable in-body value. |
| `reads` | The injected code reads a variable no longer mentioned around the seam. A target seam checks the target function's span. A text seam checks the function enclosing its anchor, or the whole file for a top-level anchor. Asset-prefixed names (`spr_`, `obj_`, `rm_`) are not scope reads and get the `asset` check instead. | Confirm the surrounding code's new parameters and locals, then re-derive the ctx and the filtered value from the new engine shape. The hook's documented contract may need to change with them. |
| `asset` | The injected code references a sprite, room, or object identifier absent from the build's asset inventory. The inventory comes from the archive itself, so the check stands down for sources that cannot enumerate assets, such as unit fixtures. | Find what replaced the asset in the new build and update the payload. Existence is all this proves, so a redrawn or resized asset still needs a live look. |
| `marker` | The marker already exists in pristine or earlier staged text. | Check order and `depends_on`, then choose a more distinctive marker if it is a real collision. |
| `missing_file` | The catalog's engine path no longer exists. | Find the moved implementation and re-audit every assumption, not just the filename. |
| `decode` | A target GML file is no longer valid UTF-8. | Verify the source archive before changing the catalog. |
| `call_rewrite` | The native-call surface changed, through a member-call site, a new GML definition, arity drift, or the loss of every direct site. | Re-audit the entire callee surface. A function that gained a GML body normally wants a wrap seam instead. |
| `framework` | The framework payload calls a function this build neither defines nor uses. | Find what replaced the call in the new engine and update the framework source. Runtime-provided hooks depending on it are dead until then. |

Catalog-load errors happen before these staging records. Duplicate names, undeclared providers, kind/dispatcher mismatches, bad contention, unknown dependencies, and cycles are catalog problems, not game-update drift.

## Game Updates

A game update rewrites engine scripts, so a green test suite says nothing about whether the catalog still fits the game. Every locator has to be checked against the new pristine build, and each kind fails in its own way:

| Locator | Tolerates | Fails on |
| ------- | --------- | -------- |
| Structural target | Indentation and comment drift | The named function or the anchored token shape changing |
| Text or pristine-context anchor | Changes outside the snippet | Any textual change inside the matched snippet |
| Call rewrite | New direct call sites, which it covers automatically | The call shape itself changing |

The static checks come first, and their order matters. The [seam check](#check-against-the-real-game) gives the fail-closed verdict on every locator and payload read against the new build. The [seam diff](#triage-a-game-update) then shows every region the catalog anchors into that changed between the previous build and the new one. It must run before the reinstall, because a reinstall replaces the pristine backup the diff uses as its old side.

### Handling Broken Anchors

A failed anchor is a prompt to re-read the surrounding engine control flow, not a string to patch until it matches again. The seam's promises all have to hold on the new build, meaning its timing, its value, its context fields, the consequence of a veto or override, its zero-handler behavior, and its hot-path cost. When they do, the catalog, its reference pages, and the game-version test evidence change together.

### Handling Semantic Drift

A changed region is where semantic drift may occur. A seam can keep its locator, stage cleanly, and pass every static check while the code around it now means something else, so each changed region needs to be re-evaluated against the seam's promises and the hook's documented contract, whether or not the seam still stages. After the catalog is updated and reinstalled, [hook liveness](DEBUG.md) verifies that the catalog still holds in the updated game.

With debug enabled, a session that plays through the surfaces the changed seams cover should show every affected hook dispatching in `mmapi_hook_liveness()`. A hook that staged cleanly but never fired during a session that exercised its moment is the one failure no static check can see.

## Requesting A New Hook

If you need a new capability, create a request for one under the project's **Issues** page. If possible, please include the following with your request:

- The game moment or value you need.
- What the mod should be allowed to observe, change, block, or replace.
- Relevant information about the mod that needs it.
- The direct call, existing hooks, or polling approach you already considered.
- The engine file and function if you know them, with only the minimum source context needed.
