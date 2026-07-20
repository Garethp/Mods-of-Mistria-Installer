# Hooks

[← MMAPI](MMAPI.md)

Hooks let your mod respond to moments in the game without editing the same engine code as every other mod. Register a handler for a named hook, and MMAPI calls it when that moment occurs.

Each hook has one of four kinds, which determines whether a handler observes an event, transforms a value, vetoes an action, or supplies replacement behavior. The [Catalog](CATALOG.md) lists every shipped hook and its contract. To learn how MOMI wires those hooks into the game, see [Seams](SEAMS.md).

Most shipped hooks are dispatched from engine seams. A few are emitted directly by the MMAPI runtime. Mods consume both in exactly the same way.

> [!NOTE]
> A mod uses the game hooks MOMI already ships. It never packages its own seams. Mods can also publish custom hooks as cross-mod extension points, covered in [Publishing Custom Hooks](#publishing-custom-hooks).

## Using A Shipped Hook

Follow the same path for every hook:

1. Find the capability in the [Catalog](CATALOG.md).
2. Read the hook's page. It states when the hook fires, its kind, the value and `ctx` it supplies, what a return value means, and any important frequency or edge cases.
3. List every shipped hook your mod depends on in the manifest's [`requires_hooks`](MANIFEST.md#requires_hooks). A MOMI version with an older seam catalog can then skip the mod with a clear reason instead of leaving a registration that never fires.
4. Write a named top-level handler and register it with the function for that hook's kind.
5. Install the mod, trigger the moment in game, and [confirm the handler fired](#confirming-a-handler-fired).

For example, `game.day_started` is an event hook. Its handler receives `{ total_days }`:

```gml
function my_mod_day_started(_ctx) {
    var _cfg = my_mod_config();
    if (!_cfg.enabled) return;

    // React to the new day. The event's return value is ignored.
    mmapi_log_info("my_mod", "day " + string(_ctx.total_days) + " started");
}

// Inside your latched register function:
mmapi_on("game.day_started", my_mod_day_started);
```

Put registrations behind the latch shown in [Mod Anatomy](MOD_ANATOMY.md#the-registered_hooks-latch). Registration is memory-only and safe during boot. Reading config, touching live engine state, and performing file IO are not; do that later from the handler or a queued tick.

> [!IMPORTANT]
> Use the registration function named by the hook's kind. A mismatched registration still lands, but the shipped dispatcher skips it, so the handler never runs.

## The Four Kinds

Start from what the handler is allowed to do:

| Intent | Kind | Register with | Handler | Return contract |
| ------ | ---- | ------------- | ------- | --------------- |
| React to a moment | `event` | `mmapi_on` | `fn(ctx)` | Every handler runs. Return values are ignored. |
| Transform a value | `filter` | `mmapi_filter` | `fn(value, ctx)` | Return a replacement value, or `undefined` to keep the current one. Later filters receive the current result. |
| Block an action | `guard` | `mmapi_guard` | `fn(ctx)` | The first Boolean `false` vetoes. Every other value allows. |
| Supply replacement behavior | `override` | `mmapi_override` | `fn(ctx)` | The first non-`undefined` result wins. Return `undefined` to defer to the next handler, then the engine. |

The kind describes how handlers participate, not when the hook fires. An event may run before, during, or after the engine action. Read the individual hook page for its exact timing and whether the engine reads any mutations made to `ctx`.

> [!IMPORTANT]
> `undefined` means **decline**: a filter keeps the current value, a guard allows, and an override defers. Events ignore every return value. For a guard, only the Boolean value `false` vetoes; numeric zero still allows.

Most filters replace a value by returning one. Two shipped hooks, `combat.tarball_grid` and `ui.item_node`, are declared **in place** instead. Their handlers mutate the first `value` argument because the dispatch site deliberately discards the return; both receive `undefined` as `ctx`. Their hook pages call this out prominently.

### Filter Example

This filter replaces a negative health delta with zero while leaving heals unchanged:

```gml
function my_mod_health_delta_filter(_value, _ctx) {
    if (is_real(_value) && _value < 0) {
        return 0; // replace damage with no change
    }
    return undefined; // keep heals and zero unchanged
}

// Inside your latched register function:
mmapi_filter("player.health_delta", my_mod_health_delta_filter);
```

`false` is also a real filter value. Returning it does not mean "keep the current value"; return `undefined` to decline.

### Guard Example

This guard vetoes selected sound effects:

```gml
function my_mod_audio_guard(_ctx) {
    // _ctx is { asset_name }.
    if (my_mod_is_muted(_ctx.asset_name)) {
        return false; // veto
    }
    return undefined; // allow
}

// Inside your latched register function:
mmapi_guard("audio.play_guard", my_mod_audio_guard);
```

Guard dispatch stops at the first Boolean `false`. A handler that allows does not prevent a later handler from vetoing.

### Override Example

This override claims only interactions owned by the mod:

```gml
function my_mod_interact_override(_ctx) {
    if (!my_mod_owns(_ctx)) {
        return undefined; // not ours: let another handler or the engine answer
    }

    // Handle the interaction.
    return true; // claim it
}

// Inside your latched register function:
mmapi_override("object.interact", my_mod_interact_override);
```

Any non-`undefined` result claims the override chain, including `false`, zero, and an empty string. Later override handlers do not run. The individual hook contract determines what the engine does with the winning value; it does not always mean "the engine action happened."

## Override Contention

Only one override answer wins, so every shipped override hook declares a **contention** class:

- **exclusive**: The hook expects at most one mod to override it. A rival registration logs a warning naming both mods. Both handlers remain registered, and dispatch order decides which one gets the first opportunity to answer.
- **claim-scoped**: Multiple mods are expected to coexist. Each handler must return `undefined` for targets it does not own. A rival registration logs an informational message explaining that contract.

`object.interact` is claim-scoped: each mod recognizes its own objects and defers everywhere else. An exclusive override normally represents one indivisible engine decision where rival answers genuinely conflict.

## Registration Options And Dispatch Order

All four registration functions accept the same optional `opts` struct:

```gml
mmapi_on(hook_name, handler, opts);
mmapi_filter(hook_name, handler, opts);
mmapi_guard(hook_name, handler, opts);
mmapi_override(hook_name, handler, opts);
```

| Field | Default | Meaning |
| ----- | ------- | ------- |
| `priority` | `0` | Lower values run first. Equal priorities keep registration order. |
| `before` | absent | One mod name or an array of mod names whose handlers should run after this one. |
| `after` | absent | One mod name or an array of mod names whose handlers should run before this one. |
| `mod_name` | current mod | Legacy attribution override. Normal mod code should rely on MMAPI's captured current-mod attribution instead. |

Priority establishes the base order. `before` and `after` then add relationships between **mods**, not individual functions. They may move a handler across priority groups, and naming a mod that is absent is safe. Handler lists are re-sorted after every registration, so an edge still takes effect when the named mod registers later.

If the relationships form a cycle, MMAPI warns and keeps the priority-and-registration base order for that hook.

```gml
// Run after spawn_tuner's filters if that mod is present.
mmapi_filter("monster.spawn", my_mod_monster_spawn, {
    after: "spawn_tuner",
});
```

> [!IMPORTANT]
> There is no unregistration API. To disable a handler at runtime, gate its body with a flag and return early.

## What MMAPI Checks At Registration

The installed catalog lets MMAPI catch mistakes when a handler is registered:

| Condition | What MMAPI does | Can the shipped dispatcher call it? |
| --------- | --------------- | ----------------------------------- |
| Known name and matching kind | Registers the handler in dispatch order. | Yes. |
| Old alias | Warns once per alias and mod, resolves it to the canonical name, and registers there. | Yes. |
| Unknown name | Warns once for the name but keeps the registration so a mod-published custom hook can dispatch it. | No MOMI-provided dispatcher exists for an unknown name. |
| Kind mismatch | Warns once per hook and mod, naming the correct registration function, but keeps the record under the requested kind. | No. Each shipped dispatcher invokes only records of its own kind. |
| Mixed kinds under one custom name | Warns rate-limited but keeps both kinds of record. | A custom dispatcher invokes only records matching its own kind. |
| Exact duplicate | Skips the new record with a rate-limited warning. The existing registration remains. | The existing record still runs. |

An exact duplicate means the same canonical hook, handler function, kind, and mod. Changing `priority`, `before`, or `after` does not make a second registration distinct. Duplicate suppression matters because queued install functions can rerun every frame, but a registration latch is still preferable: it avoids warning noise and also protects APIs that do not de-duplicate.

Alias resolution happens during registration only. Dispatch and introspection functions do not translate an old name, so use the canonical name everywhere. Unknown-name warnings are also once per name globally: the mod that registers the name first receives the warning, while later consumers may not see it in their own logs.

### Declaring Hook Compatibility

Registration warnings happen at game boot. The manifest's [`requires_hooks`](MANIFEST.md#requires_hooks) catches a missing shipped hook during installation instead:

```toml
requires_hooks = ["game.day_started", "player.health_delta"]
```

MOMI checks those names against the installed seam catalog. If any are absent, it skips that mod with a clear reason while continuing with compatible mods. List the shipped hooks your mod needs to function.

Custom mod-published hooks do not belong in `requires_hooks`; they are not part of MOMI's catalog. See [Publishing Custom Hooks](#publishing-custom-hooks).

## Handler Runtime Behavior

### Error Isolation

MMAPI catches exceptions around each call into a hook handler. An exception does not propagate into the engine or stop later handlers, although MMAPI cannot undo state the handler changed before it threw.

| Kind | On handler error |
| ---- | ---------------- |
| event | Skips the failed handler. Remaining event handlers still run. |
| filter | Keeps the current value and continues the chain. |
| guard | Counts the failure as allow and continues. A crash never becomes an accidental veto. |
| override | Treats the failure as a decline. The next override may still answer. |

Each failure increments the owning mod's error count, visible through `mmapi_hook_stats()`. The warning is rate-limited per hook and mod: the first occurrence logs immediately, then one warning every 60 occurrences.

### Hot Hooks

Some hooks fire per instance per frame. `monster.step_begin` and `monster.draw` are among them. Put the cheapest test first and return before allocating structs, scanning collections, logging, or doing other work when the mod has nothing to do:

```gml
function my_mod_monster_step(_ctx) {
    if (!__my_mod_runtime().enabled) return;

    // Real work only past this line.
}
```

The individual hook page documents frequency when it matters. Treat a log-and-flush probe as temporary on a hot hook; flushing every dispatch turns a useful test into repeated file IO.

## Testing And Inspecting Hooks

### Confirming A Handler Fired

For a temporary proof, add one log line to the handler and flush it:

```gml
mmapi_log_info("my_mod", "game.day_started fired");
mmapi_log_flush("my_mod");
```

Install the mod, launch the game, and trigger the documented moment. Then read:

```text
%LOCALAPPDATA%/FieldsOfMistria/mod_data/<mod>/logs/<mod>.log
```

If the proof line is absent, read every `WARN` in the same file. The common causes are an unknown hook name, a kind mismatch, an exact duplicate, or an exception inside the handler. Unknown and mismatched registrations remain in the registry; exact duplicates are skipped.

Also confirm that the mod was not skipped at install and that the hook's timing conditions really occurred. See [Troubleshooting](TROUBLESHOOTING.md#the-handler-never-runs) for the full preflight, or [Debug](DEBUG.md) for live watches.

### The Installed Catalog

At install time MOMI renders the hook declarations from `seams.toml` into `mmapi_hook_catalog.gml`. The generated file maps every shipped hook to its kind, every old alias to its canonical name, and every override hook to its contention class. Registration checks and introspection read those tables.

| Function | What it answers |
| -------- | --------------- |
| `mmapi_hook_exists(name)` | Whether the name is declared in the installed catalog. Returns `undefined` when the catalog table is unavailable, such as in a bare test runtime. |
| `mmapi_hook_kind(name)` | The declared kind, and therefore the registration function to use. |
| `mmapi_hook_info(name)` | The declared kind and registered handlers for one hook, in dispatch order. |
| `mmapi_hook_stats()` | A registry-wide snapshot of handler counts, dispatch wiring, and per-mod error counts. |

The installed GML catalog supports runtime checks. The browsable [Catalog](CATALOG.md) and its hook pages are the public reference for timing, values, context shapes, and edge cases. The source declarations live in the seam catalog; see [Declaring A Hook](SEAMS.md#3-declare-the-hook).

## Before Shipping

- The hook appears in your manifest's `requires_hooks` if the mod depends on it.
- The registration function matches the kind on the hook page.
- The handler is a named top-level function registered behind your mod's latch.
- The handler follows the documented timing, `ctx`, mutation, and return contract.
- A filter or override returns `undefined` when it does not want to answer; a guard returns Boolean `false` only when it intends to veto.
- A hot handler exits through its cheapest condition first.
- A real in-game trigger produces the expected result without warnings in the mod log.

## Publishing Custom Hooks

The dispatchers are ordinary GML functions, so a mod may publish its own extension points for other mods:

| Function | Kind it dispatches | With zero handlers |
| -------- | ------------------ | ------------------ |
| `mmapi_emit(name, ctx)` | event | Returns `undefined`. |
| `mmapi_apply_filters(name, value, ctx)` | filter | Returns the original value. |
| `mmapi_check_guards(name, ctx)` | guard | Returns `true` to allow. |
| `mmapi_run_override(name, ctx)` | override | Returns `undefined` so the publisher can fall back. |

Namespace the hook to the publishing mod, such as `my_mod.item_charged`, and document the same contract as a shipped hook: exact kind, timing, value, `ctx`, return behavior, and frequency. Consumers must use the matching registration function.

Custom names are not added to MOMI's installed catalog. The first registration therefore produces the expected once-per-name unknown-hook warning, and `mmapi_hook_kind()` cannot supply their kind. MOMI's literal-hook lint also reports them as unknown, so `--strict-lints` is not compatible with a custom literal registration.

Do not put custom names in `requires_hooks`; that field proves a MOMI catalog hook exists, not that another mod is installed. When the publisher is optional, an absent publisher simply means the handler never fires. If the publisher is required, coordinate that dependency outside the hook catalog.

See [Cross-Mod Coordination](API_REFERENCE.md#cross-mod-coordination) for the rest of the cross-mod contract.
