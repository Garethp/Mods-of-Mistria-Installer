# API Reference

[← MMAPI](MMAPI.md)

The `mmapi_*` helper areas, with the house pattern for each. For the hook engine itself, see [Hooks](HOOKS.md). For the debug agent, see [Debug](DEBUG.md).

## Core

Mod identity and lifecycle.

| Function | What it does |
| -------- | ------------ |
| `mmapi_mod_declare(mod_name, version)` | Call once from your boot file's top level. Every registration made afterwards attributes to that mod: log lines, error counts, hook ordering edges. |
| `mmapi_register(install_fn)` | Queue a function the framework calls **every frame** from the game's step begin, starting with the first frame. The first call is the first safe moment for file IO, and every later call is a per-frame tick. Must be idempotent. |
| `mmapi_current_mod()` | The mod the framework currently attributes work to. |
| `mmapi_io_is_ready()` | `false` during boot, `true` from the first frame's drain on. |

See [Mod Anatomy](MOD_ANATOMY.md#the-lifecycle) for how these fit the lifecycle.

## Config

JSON configuration helpers. All mod data lives under the game's `%LOCALAPPDATA%` directory. Due to engine constraints, that is the only location GML can write to.

Each mod gets `%LOCALAPPDATA%/FieldsOfMistria/mod_data/<mod>/` for its config, logs, and per-save data, and its config file is `mod_data/<mod>/<mod>.json`.

| Function | What it does |
| -------- | ------------ |
| `mmapi_config_load(mod_name)` | Load the mod's JSON config as a struct (empty struct when absent). |
| `mmapi_config_save(mod_name, cfg)` | Write the struct back. |
| `mmapi_config_get(cfg, key, default)` | Read one key with a default. |
| `mmapi_config_get_range(cfg, key, default, min, max)` | Read a number, clamped and validated. |
| `mmapi_config_path(mod_name)` / `mmapi_config_dir(mod_name)` / `mmapi_mod_data_dir(mod_name)` | The file and directory paths. |

### The House Pattern
Load lazily (never at top-level boot), validate every key, then write the normalized struct back so players see every key with its current value.

```gml
function my_mod_config() {
    if (global[$ "__my_mod_cfg"] != undefined) {
        return global.__my_mod_cfg;
    }
    var _cfg = mmapi_config_load("my_mod");
    var _normalized = {
        bite_multiplier: mmapi_config_get_range(_cfg, "bite_multiplier", 1.0, 0.1, 10.0),
        show_hud: mmapi_config_get(_cfg, "show_hud", true),
    };
    mmapi_config_save("my_mod", _normalized);
    global.__my_mod_cfg = _normalized;
    return _normalized;
}
```

## Log

Leveled logging to the console and a per-mod log file at `mod_data/<mod>/logs/<mod>.log`.

- Levels: `MmapiLogLevel` Trace, Debug, Info, Warn, Error. One call per level: `mmapi_log_trace/debug/info/warn/error(mod_name, message)`, plus `mmapi_log(level, mod_name, message)`.
- Logging is **safe at boot**. Boot-time lines buffer in memory and flush on the first frame. Lines otherwise flush in batches, and immediately at Warn and above.
- Level control: `mmapi_log_set_level` / `mmapi_log_get_level` / `mmapi_log_level_from_string`, or the `log_level` key in the mod's config file. The default is Info.
- `mmapi_warn_rate_limited(mod_name, key, message)`: the first occurrence logs immediately, then one per 60. The framework uses this for handler errors, and it is available to mods too.
- `mmapi_log_flush()` forces the file sink, and `mmapi_log_set_sinks` selects console/file.

## Mod Save Files

Per-save mod data, as a JSON sidecar keyed by the save slot, driven by the game's own save and load.

```gml
mmapi_modsave_register("my_mod", my_mod_save_collect, my_mod_save_apply);
```

`collect` returns a plain struct to persist (numbers, strings, bools, arrays, and plain structs only, per [save_json_file](MOD_ANATOMY.md#save_json_file-crashes-on-non-plain-values)). `apply` receives it back after a save loads. Version your schema with a field in the struct so an old sidecar can be migrated or discarded.

> [!WARNING]
> Register once, inside your latch. Mod-save registrations are not de-duplicated.

## Hotkey

Keyboard hotkeys through a shared registry, so mods do not fight over raw input polling.

```gml
var _vk = mmapi_hotkey_vk_from_name(my_mod_config().activation_button); // e.g. "HOME", "F7"
if (_vk != undefined) {
    mmapi_hotkey_register(_vk, my_mod_on_hotkey);
}
```

`mmapi_hotkey_vk_from_name` resolves `F1–F12`, `0–9`, `A–Z`, numpad keys, and named specials. Unknown names return `undefined`, so hotkeys can safely come from config. Two mods on the same key both stay registered, with a warning.

## Localization

Every engine text lookup routes through MMAPI (a call rewrite installed by the seam layer), which makes all game text filterable:

- `mmapi_local_get(key, ...)`: The lookup itself, callable directly.
- Hook `local.get` (filter): Rewrite any text the engine asks for.
- Hook `local.missing` (filter): Supply text for keys the tables lack.

## Derived Events

Some hooks are emitted by the framework itself, polled once per frame, rather than by an engine seam. `game.day_started`, `game.room_changed`, and `game.title_entered` are among them. They register and behave exactly like any other hook. The distinction only matters if you go looking for their seam.

## Cross-Mod Coordination

How mods talk to each other without a cross-mod call API.

### Never Call Another Mod's Functions

Mod GML compiles into the game as **one program**. A call to a function that does not exist is a *compile error*, not a runtime error. If your mod names another mod's function and that mod is absent, your mod fails to compile and never loads. No `if` guard helps, because the name fails to resolve before any code runs.

Coordination happens exclusively through **guarded global reads**. Globals are addressed by string through the accessor (`global[$ "name"]`), which resolves at runtime and yields `undefined` when the other mod is absent.

### Reading Another Mod's State

Gate every step of the path with `is_struct()`. The other mod may be absent, an older version without the field, or not yet booted:

```gml
function my_mod_read_other_wave() {
    var _other = global[$ "__spawn_tuner"];
    if (!is_struct(_other)) { return undefined; }
    var _state = _other[$ "state"];
    if (!is_struct(_state)) { return undefined; }
    return _state[$ "wave"];
}
```

Read from a handler or your tick, never at top-level boot. Mods boot in unspecified order.

### Leaving a Flag For Another Mod

The receiver owns the flag. It declares the struct, documents the field, and reads it on its own schedule. The writer only checks that the receiver exists and writes the agreed field. Write into a foreign namespace's fields, and never replace another mod's root struct.

```gml
// Writer (a different mod): set the flag only when the receiver is present.
var _arena = global[$ "__arena_mod"];
if (is_struct(_arena)) {
    _arena.disable_spawns = true;
}
```

### Ordering Handlers Across Mods

Use the `before` / `after` registration options. They name mods, not functions, and are safe when the named mod is absent. See [Hooks](HOOKS.md#registration-options).

### Publishing Hooks For Your Mod

Call the dispatchers (`mmapi_emit`, `mmapi_apply_filters`, `mmapi_check_guards`, `mmapi_run_override`) with your own hook names to give other mods extension points. See [Hooks](HOOKS.md#defining-your-own-hooks).

## Calling the Engine Directly

Most mod needs are direct engine calls, not hooks. Since mod GML compiles into the game as one program, every engine function, method, and global is directly callable. Hooks are for changing what the engine does. To make the engine do something, call it.

> [!IMPORTANT]
> Call the engine from a hook handler or a registered tick, never at top-level boot. Top-level code runs before the first frame: there is no player, no room, and file IO throws.

A sampler of the common ones:

```gml
// Give the player an item (check the lookup, undefined for unknown names).
var _id = try_string_to_item_id("wild_berry");
if (_id != undefined) { ARI.give_item(_id, 1); }

// Give (or take) gold.
ARI.modify_gold(500);

// Show the toast notification popup. Once per real event, not every frame.
create_notification("Something happened");

// Teleport the player to a named location, at pixel coordinates in that room.
if (instance_exists(obj_ari)) {
    ari_teleport_to_room("town", 1097, 1323);
}

// Play an engine sound by name (missing names are silent, so check first).
var _name = "SoundEffects/Objects/Explosion";
if (TANGO.name_exists(_name)) { TANGO.play(_name); }

// The player (ari) instance. It does not exist on the title screen, so gate every access.
if (instance_exists(obj_ari)) {
    var _ari = instance_find(obj_ari, 0);
    // read _ari.x, _ari.y, ...
}
```

Direct calls still cooperate with the hook system where the engine path is seamed. Every `TANGO.play` runs the `audio.play_guard` hook, every monster spawn runs the `monster.spawn` filter, and so on.

> [!IMPORTANT]
> Keep in mind that another mod can legitimately veto or reshape what you asked for. Write defensive code.

## Hooks

When you need to change or observe what the engine does *on its own*, register a hook instead. Start at [Hooks](HOOKS.md).
