# Hooks

[← MMAPI](MMAPI.md)

The named-hook engine. One generic registry backs four hook **kinds**. Mods register handlers against hook names, and the engine seams installed by MOMI dispatch them. Every hook name, its kind, and its context shape come from the seam catalog. See [Seams](SEAMS.md).

## The Four Kinds

| Kind | Register with | Handler | Semantics |
| ---- | ------------- | ------- | --------- |
| event | `mmapi_on` | `fn(ctx)` | Observe-only fan out. Every handler runs, and return values are ignored. |
| filter | `mmapi_filter` | `fn(value, ctx)` | Chained value transform. Each handler returns a replacement value, or `undefined` to keep the current value. |
| guard | `mmapi_guard` | `fn(ctx)` | Veto check. Any handler returning `false` vetoes. `undefined` or `true` allows. |
| override | `mmapi_override` | `fn(ctx)` | Replacement. The first non-`undefined` result wins. `undefined` defers to the next handler, then the engine. |

One hook name corresponds to one kind. Each dispatcher only invokes handlers of its own kind, so a registration made with the wrong directive never runs. MMAPI warns once per hook and mod, naming the correct directive to use instead.

All four directives (how mods register their hooks) share the same shape:

```gml
// Note: opts is an optional argument
mmapi_on(hook_name, handler, opts);
mmapi_filter(hook_name, handler, opts);
mmapi_guard(hook_name, handler, opts);
mmapi_override(hook_name, handler, opts);
```

> [!IMPORTANT]
> `undefined` always means **decline**, which keeps the current value (filter), allows (guard), or defers (override).

### Examples

An `EVENT` observer:

```gml
function my_mod_day_started(_ctx) {
    mmapi_log_info("my_mod", "day " + string(_ctx.total_days) + " started"); // log on event
}

mmapi_on("game.day_started", my_mod_day_started);
```

A `FILTER` that blocks damage but lets heals through.

```gml
function my_mod_health_delta_filter(_value, _ctx) {
    if (is_real(_value) && _value < 0) {
        return 0; // prevent damage
    }
    return undefined; // heals and zero pass through unchanged
}

mmapi_filter("player.health_delta", my_mod_health_delta_filter);
```

A `GUARD` that vetoes:

```gml
// Prevents the game from playing specific audio.
function my_mod_audio_guard(_ctx) {
    // _ctx is { asset_name }.
    if (my_mod_is_muted(_ctx.asset_name)) { return false; } // veto
    return undefined; // allow
}

mmapi_guard("audio.play_guard", my_mod_audio_guard);
```

An `OVERRIDE` that claims only its own targets:

```gml
function my_mod_interact_override(_ctx) {
    if (!my_mod_owns(_ctx)) { return undefined; } // defer: not ours
    // ... handle the interaction ...
    return true; // claim it
}
mmapi_override("object.interact", my_mod_interact_override);
```

## Override Contention

Only one override answer wins, so every override hook is declared with a **contention** class in the catalog:

- **exclusive**. The hook expects at most one mod to override it. A second mod overriding an already-overridden hook warns, naming both mods.
- **claim-scoped**. Many mods may register, but each handler must return `undefined` for targets it does not own. Any non-`undefined` return claims the whole interaction, as in the `object.interact` example above.

## Registration Options

The optional `opts` struct on every directive: `{ priority, mod_name, before, after }`.

- `priority`: Lower runs first. The default is `0`, and equal priority keeps registration order (stable).
- `before` / `after`: Ordering edges that name **mods**, not functions. They are safe when the named mod is absent. An edge naming a mod that never registers has no effect, and an edge naming a mod that registers later still lands, because the handler list is re-sorted on every registration.
- `mod_name`: [**DEPRECATED**] Attribution override. Defaults to the current mod. 

```gml
// Run after spawn_tuner's filters, whether or not that mod is installed.
mmapi_filter("monster.spawn", my_mod_monster_spawn, { after: "spawn_tuner" });
```

## Registration Checks

Registration validates against the installed catalog and the registry:

- An **unknown hook name** warns once per name. It is a typo, or a seam that was never installed. The registration lands but nothing ever dispatches it. Listing the hook in your manifest's `requires_hooks` turns this silent failure into a clear install-time skip. See [The Manifest](MANIFEST.md#requires_hooks).
- An **old (alias) name** warns once per alias and mod and lands on the canonical name, so a hook rename never breaks a mod silently.
- A **duplicate registration** (same hook, function, kind, and mod) is skipped with a rate-limited warning. Installers rerun every frame, and without this an unguarded registration would pile up handlers.
- A **kind mismatch** warns once per hook and mod, naming the correct directive to use in the log.

> [!IMPORTANT]
> There is no unregistration API. To disable a handler at runtime, gate its body with a flag and leave it registered.

## Error Isolation

A handler that throws never breaks the game or the other handlers. MMAPI warps every call into mod code in a try/catch and contains the failure per kind:

| Kind | On handler error |
| ---- | ---------------- |
| event | The handler is skipped. The remaining handlers still run. |
| filter | The current value is kept. The chain continues. |
| guard | Counts as allow (guards fail open). |
| override | The handler is skipped. The next override may still answer. |

Each failure increments the owning mod's error count (visible in `mmapi_hook_stats`) and logs a rate-limited warning. The first occurrence immediately, then one log per 60 occurrences.

## Hot Hooks

Some hooks fire per instance per frame. `monster.step_begin` and `monster.draw` are among them. Make your handler's first check the cheapest one and short circuit early when your mod has nothing to do:

```gml
function my_mod_monster_step(_ctx) {
    if (!__my_mod_runtime().enabled) return;  // short circuit test
    // real work only past this line
}
```

## The Installed Catalog

The installed catalog file, `mmapi_hook_catalog.gml`, is generated from the seam catalog at install time and ships alongside the patched scripts. It maps every hook name to its kind, every alias to its canonical name, and every override hook to its contention class. Registration checks read it, and the introspection functions expose it:

| Function | What it answers |
| -------- | --------------- |
| `mmapi_hook_exists(name)` | Is this hook declared in the installed catalog? |
| `mmapi_hook_kind(name)` | Which kind, and therefore which directive to use. |
| `mmapi_hook_info(name)` | The hook's registered handlers, in dispatch order. |
| `mmapi_hook_stats()` | Registry-wide snapshot: hooks, handler wiring, per-mod error counts. |

The authoritative list of hook names and their context shapes is the seam catalog itself. Each hook declaration carries a `doc` line saying when it fires and what `ctx` contains. See [Seams](SEAMS.md#where-hooks-are-documented).

## Defining Your Own Hooks

The dispatch functions are ordinary GML functions. A mod may call them with its own hook names to publish extension points to other mods:

| Function | Kind it dispatches |
| -------- | ------------------ |
| `mmapi_emit(name, ctx)` | event |
| `mmapi_apply_filters(name, value, ctx)` | filter |
| `mmapi_check_guards(name, ctx)` | guard |
| `mmapi_run_override(name, ctx)` | override |

See [Cross-Mod Coordination](API_REFERENCE.md#cross-mod-coordination) for the rest of the cross-mod story.
