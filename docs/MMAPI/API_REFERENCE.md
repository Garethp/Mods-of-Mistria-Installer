# API Reference

[← MMAPI](MMAPI.md)

The public `mmapi_*` helper areas and their working contracts. For the hook engine itself, see [Hooks](HOOKS.md). For the debug agent, see [Debug](DEBUG.md).

## Core

Mod identity and lifecycle.

| Function | What it does |
| -------- | ------------ |
| `mmapi_mod_declare(mod_name, version)` | Declare identity/version and set current attribution for later hook, hotkey, and `mmapi_register` registrations. MMAPI restores that attribution around queued callbacks. Logging and mod-save calls take an explicit mod name. |
| `mmapi_register(install_fn)` | Queue a function the framework calls **every frame** from the game's step begin, starting with the first frame. The first call is the first safe moment for file IO, and every later call is a per-frame tick. The function must be idempotent, and the queue call itself must be latched because registrations are not de-duplicated. |
| `mmapi_current_mod()` | The mod the framework currently attributes work to. |
| `mmapi_io_is_ready()` | `false` during boot, `true` from the first frame's drain on. |

See [Mod Anatomy](MOD_ANATOMY.md#the-lifecycle) for how these fit the lifecycle.

## Config

JSON configuration helpers. All mod data lives under the game's `%LOCALAPPDATA%` directory. Due to engine constraints, that is the only location GML can write to.

Each mod gets `%LOCALAPPDATA%/FieldsOfMistria/mod_data/<mod>/` for its config, logs, and per-save data, and its config file is `mod_data/<mod>/<mod>.json`.

| Function | What it does |
| -------- | ------------ |
| `mmapi_config_load(mod_name)` | Low-level load of the parsed JSON value. A missing file yields `{}`; a corrupt primary first tries its last-good `.bak`. It does not shape-check, so prefer `mmapi_config_read_valid` for the standard struct contract. |
| `mmapi_config_save(mod_name, cfg)` | Preserve the old valid file as `<mod>.json.bak`, then write the struct. |
| `mmapi_config_get(cfg, key, default)` | Read one key with a default. |
| `mmapi_config_get_range(cfg, key, default, min, max)` | Return the value when it compares inside the inclusive range, otherwise the default. This legacy helper neither clamps nor checks the value's type. |
| `mmapi_config_read_valid(mod_name, version)` | Load only a struct with the matching numeric `__config_version`; otherwise return `{}` so fields take their defaults. |
| `mmapi_config_version_ok(cfg, version)` | Test that version contract directly. |
| `mmapi_config_bool(cfg, key, default)` | Read a genuine Boolean, otherwise the default. |
| `mmapi_config_number(cfg, key, default, min, max)` | Read a number in the inclusive range, normalized with `real()`, otherwise the default. It does not clamp. |
| `mmapi_config_write(mod_name, version, cfg)` | Stamp `__config_version`, preserve the last-good copy, and write the normalized struct. |
| `mmapi_config_path(mod_name)` / `mmapi_config_dir(mod_name)` / `mmapi_mod_data_dir(mod_name)` | The file and directory paths. |

### The House Pattern

Load lazily (never at top-level boot), reject an old schema as a unit, validate every key, then write the normalized struct back so players see every key with its current value. Increment the version when an old file cannot safely map to the new schema.

```gml
#macro MY_MOD_CONFIG_VERSION 1

function my_mod_config() {
    if (global[$ "__my_mod_cfg"] != undefined) {
        return global.__my_mod_cfg;
    }
    var _source = mmapi_config_read_valid("my_mod", MY_MOD_CONFIG_VERSION);
    var _cfg = {
        bite_multiplier: mmapi_config_number(_source, "bite_multiplier", 1.0, 0.1, 10.0),
        show_hud: mmapi_config_bool(_source, "show_hud", true),
    };
    mmapi_config_write("my_mod", MY_MOD_CONFIG_VERSION, _cfg);
    global.__my_mod_cfg = _cfg;
    return _cfg;
}
```

An absent primary is a fresh config and does not resurrect a stray backup. A primary that exists but no longer parses does try `<mod>.json.bak` and logs the recovery. Keep enum allow-lists, arrays, and other mod-specific validation explicit beside the Boolean and number helpers.

## Log

Leveled logging to the console and a per-mod log file at `mod_data/<mod>/logs/<mod>.log`.

- Levels in `MmapiLogLevel`: `Trace`, `Debug`, `Info`, `Warn`, and `Error`. One call per level: `mmapi_log_trace/debug/info/warn/error(mod_name, message)`, plus `mmapi_log(level, mod_name, message)`.
- Logging is **safe at boot**. Info and above buffer in memory and flush on the first frame. The configured Debug/Trace threshold is read only after IO becomes ready, so boot-time Debug and Trace are filtered unless code first calls `mmapi_log_set_level`. Lines otherwise flush in batches, and immediately at Warn and above.
- Level control: `mmapi_log_set_level` / `mmapi_log_get_level` / `mmapi_log_level_from_string`. The shared default comes from `log_level` in `mod_data/mmapi/mmapi.json`, not from each mod's config, and defaults to Info. Trace is file-only.
- `mmapi_warn_rate_limited(key, mod_name, message)`: the first occurrence logs immediately, then one per 60. Keys share one framework-wide counter, so prefix yours with the mod name.
- `mmapi_log_flush(mod_name)` forces that mod's file sink after IO is ready, and `mmapi_log_set_sinks(console_on, file_on)` selects the shared sinks. Do not flush at top-level boot; use it from a handler or tick.

Trace, Debug, and Info normally flush after 20 pending lines; Warn and Error flush immediately. Each flush rewrites the accumulated log for the current session. When proving that a one-off handler fired, log at Warn or call `mmapi_log_flush("my_mod")` after the Info line from gameplay time.

## Mod Save Files

Per-save mod data, as a JSON sidecar driven by the game's own save and load. MMAPI extracts the prefix between `game-` and the next dash in the save path, then writes `mod_data/<mod>/saves/<prefix>.json`.

```gml
mmapi_modsave_register("my_mod", my_mod_save_collect, my_mod_save_apply);
```

`collect` takes no arguments and returns a plain struct to persist (numbers, strings, bools, arrays, and plain structs only, per [save_json_file](MOD_ANATOMY.md#save_json_file-crashes-on-non-plain-values)). `apply(data)` receives it after a save loads. A fresh save passes `undefined`. Version your schema with a field in the struct so an old sidecar can be migrated or discarded.

Returning `undefined` from `collect`, or throwing before it returns, skips that write and preserves the previous sidecar. Callback failures are contained and rate-limited so the remaining registrations still run. Before overwriting a valid sidecar, MMAPI preserves `<prefix>.json.bak`; if the primary later exists but cannot parse, load tries that last-good copy. A merely missing primary is treated as fresh and does not revive the backup.

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

`mmapi_hotkey_vk_from_name` is case-sensitive. It accepts `F1` through `F12`, `NUMPAD_0` through `NUMPAD_9`, single digits, uppercase `A` through `Z`, and `INSERT`, `DELETE`, `HOME`, `PAGE_UP`, `PAGE_DOWN`, `SHIFT`, `ALT`, `CONTROL`, `PAUSE_BREAK`, `CAPS_LOCK`, `NUM_LOCK`, or `SCROLL_LOCK`. Lowercase and `GAMEPAD_*` names return `undefined`.

The callback takes no arguments. Callback failures are isolated and rate-limited, and polling continues. Two mods on the same key both fire with a warning. Registrations are not de-duplicated, even when the mod, key, and callback are identical, so register once inside your latch.

## Combat

Inject a hit through the engine's own damage pipeline, rather than writing to health directly. The synthetic hit runs the full pipeline, so other mods' `combat.damage` filters still see it.

| Function | What it does |
| -------- | ------------ |
| `mmapi_deal_damage(target, amount, opts)` | Deal `amount` damage to a receiver instance, or to an instance that owns one (a monster, or the player). Returns the tarball, or `undefined` when the hit is rejected or the target is invalid. |
| `mmapi_deal_damage_player(amount, opts)` | The same, aimed at the player. Resolves the live `obj_ari` instance and returns `undefined` when there is no player, such as on the title screen. |

`amount` is positive, pre-mitigation damage unless `instant_kill` is set. Player damage goes through the engine's mitigation and one-damage floor; monster damage is raw. A queued hit normally resolves in the owner's next damage drain; a `DamageOnAttack` receiver can consume it during `receiver.damage(...)` before the helper returns.

`opts` is optional. Its full surface is:

| Field | Meaning |
| ----- | ------- |
| `critical`, `heavy` | Set the matching popup flags. |
| `instant_kill` | Use the engine's 999-damage critical behavior. It is not guaranteed to kill a target above 999 health. |
| `flags` | Raw `CombatFlag` bits OR'd onto the tarball. |
| `pierce_iframes` | Add `CombatFlag.Acid`, bypassing the drain-time iframe check. Enqueue-time iframes can still reject the hit. |
| `electrocute_kind` | Add the Electric flag and set the engine's electrocute kind. |
| `venomous`, `frozen`, `fire_oil` | Set the matching monster-side status effects. |
| `source` | Instance credited as the tarball parent. Required for `knockback`. |
| `knockback` | `{ force_min, force_max, radius }`; omitted with a warning when `source` is absent. |
| `provenance`, `stats_entry` | Mines-run accounting fields passed to the builder. |
| `show_popup`, `flinch` | Player-only presentation controls. Set either to `false` to suppress it. |
| `target_mask` | Override the tarball target mask. Defaults to the receiver's mask. |
| `gc_frames` | Fallback lifetime for unresolved or dropped tarballs. Defaults to 30 frames. |

> [!NOTE]
> The injected hit flows through `combat.damage` and `combat.damage_resolved` like any engine hit, so a filter can still change or cancel it. The tarball carries `__mmapi_injected` with the injecting mod's name, so a `combat.damage` filter can tell synthetic hits from engine hits. See [combat.damage_injected](hooks/combat.damage_injected.md).

## Localization

Every direct engine-GML `local_get(...)` call routes through MMAPI (a call rewrite installed by the seam layer), which makes that game-text surface filterable. Mod files are added after the rewrite; call `mmapi_local_get(key)` explicitly when mod code should use the same hook path:

- `mmapi_local_get(key)`: The lookup itself, callable directly.
- Hook `local.get` (filter): Rewrite any text the engine asks for.
- Hook `local.missing` (filter): Supply text for keys the tables lack.

## Derived Events

Some hooks are emitted by the framework itself rather than by an engine seam. `game.day_started`, `game.room_changed`, and `game.title_entered` come from the per-frame state poll; `combat.damage_injected` fires directly from `mmapi_deal_damage`. They register and behave exactly like any other hook. The distinction only matters if you go looking for their seam.

## Cross-Mod Coordination

How mods talk to each other without a cross-mod call API.

### Never Call Another Mod's Functions

Mod GML compiles into the game as **one program**. A call to a function that does not exist is a *compile error*, not a runtime error. If your mod names another mod's function and that mod is absent, your mod fails to compile and never loads. No `if` guard helps, because the name fails to resolve before any code runs.

Direct cross-mod state access uses **guarded global reads**. Globals are addressed by string through the accessor (`global[$ "name"]`), which resolves at runtime and yields `undefined` when the other mod is absent. Published custom hooks are the other coordination surface; see [Publishing Hooks For Your Mod](#publishing-hooks-for-your-mod).

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

Most mod needs are direct engine calls, not hooks. Since mod GML compiles into the game as one program, every engine function, method, and global is directly callable. Hooks are for observing or changing what the engine does on its own. To ask the engine to do something, call it.

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
// The argument is a LOCALIZATION KEY, resolved engine-side through local_get
// - inside the local_get_dispatch rewrite, so local.get filters apply (pass
// the key, never pre-localized text). Register your own keys via fiddle
// strings + l10n fiddle_renames: see Mod Anatomy's "User-Facing Text".
// Optional second arg suppresses repeats of the same key, in frames.
create_notification("misc_local/known_recipe");
create_notification("mods/my_mod/notifications/something_happened", 60 * 5);
// Prototypes only (untranslatable, bypasses filters):
create_notification(ANCHOR.wrap_for_local("Something happened"));

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
