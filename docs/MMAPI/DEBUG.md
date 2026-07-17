# Debug

[← MMAPI](MMAPI.md)

MMAPI ships an in-game **debug agent**. It stages watches, breakpoints, and callable functions, can pause and single-step the game, and speaks a two-file JSON protocol to an external debugger client. It is off by default and effectively no-ops while disabled.

## Enabling

The agent is gated by one config key. To enable it, set `debug_enabled` in the framework's own config file, `%LOCALAPPDATA%/FieldsOfMistria/mod_data/mmapi/mmapi.json`:

```json
{ "debug_enabled": true }
```

There is no installer toggle for this. It is a deliberate one-key hand edit, since the agent reads the key itself through the normal config store.

The flag is checked lazily on the first frame, never at top-level boot, and the verdict is cached for the session. Restart the game to toggle, or call `mmapi_debug_set_enabled(true)` from mod code as the runtime escape hatch.

While disabled, the per-frame cost is one struct lookup and a bool test, and the agent never reads or writes a file. Debug calls can therefore stay in shipped mod code.

## Hotkeys

Registered through the hotkey registry only once the agent is enabled, so a disabled debugger never claims the keys.

| Key | Action |
| --- | ------ |
| F9 | Pause / resume |
| F10 | Step one frame (while paused) |
| F8 | Toggle the state snapshot emit |

The pause is cooperative. It sets the engine's own pause flag, so all pause-gated game logic freezes while draw events still run and audio keeps playing.

## The Two-File Protocol

The agent exchanges JSON with the debugger client under the framework's mod-data directory (`mod_data/mmapi/`):

- **`control.json`** (client → agent): watch paths, breakpoints, and commands (`pause`, `resume`, `step`, `set`, `call`).
- **`state.json`** (agent → client): the watched values, pause state, break reports, and the catalog of callable functions.

While running, the agent polls `control.json` at most every 10th frame and writes `state.json` about 10 times a second. While paused, both run every frame. Any tool that can read and write JSON files can be a client.

## Watches and Paths

Watches, breakpoints, `set`, and `call` arguments all address live game state through **dotted paths**, resolved by the same resolver as `mmapi_debug_resolve`:

- `global.__my_mod.state.x` walks from a named global.
- A bare head tries curated instance roots first (`obj_ari`, the live player), then a global, so `__my_mod.x` works without the `global.` prefix.
- Numeric segments index arrays.

Anything the resolver can reach is watchable from the client with no registration. This is why the house pattern keeps all mod state in one `global.__<name>` struct.

### Watching hook wiring

`global.__mmapi_debug_stats` carries a fresh `mmapi_hook_stats()` snapshot, republished on every control poll. Watch it (or its `.hooks`, `.errors`, and `.wiring` children) to observe hook wiring live. Hook names contain dots, so the `wiring` table swaps dots for underscores: `.wiring.spells_cost` lists every handler on `spells.cost` in dispatch order, with its mod, kind, and priority. This is the fastest way to answer "did my handler actually register, and in what order?"

## The Mod-Facing API

All of these are inert while the agent is disabled. They return immediately and never touch config or files, so they are safe on hot paths and in shipped code.

### mmapi_debug_break(label, cond)

Pauses the game when `cond` becomes true, **edge-triggered per label**: after firing, the label must be observed with `cond` false before it can fire again. Place it somewhere that runs every frame (or at least on both sides of the edge).

```gml
// Pause on the frame the wave counter first reaches 3.
function arena_mod_tick() {
    mmapi_debug_break("wave_three", global.__arena_mod.wave >= 3);
}
```

Firing pauses the game the same way F9 does, records the break as `{ kind, label, game_frame }` in the state snapshot, and logs an Info line. Resume with F9 or from the client.

### mmapi_debug_break_each(label)

Pauses every time the call is reached, with no edge and no condition. It means "stop each time this event happens".

```gml
function arena_mod_on_monster_death(_ctx) {
    mmapi_debug_break_each("monster_death");
}
```

> [!WARNING]
> Do not place it on a line that runs every frame. It would re-pause every frame after resume.

### `mmapi_debug_register_fn(name, fn, opts)`

Publishes a function to the debugger's call surface, so the client can invoke it by name. `opts` is `{ mod_name, description, args }`, where `args` is an array of `{ name, type, default }` descriptors so the client can render a typed argument form.

```gml
function warp_kit_debug_warp(_location) {
    if (instance_exists(obj_ari)) {
        ari_teleport_to_room(_location, 0, 0);
    }
}

mmapi_debug_register_fn("warp", warp_kit_debug_warp, {
    description: "Warp the player to a named location",
    args: [{ name: "location", type: "string", default: "town" }],
});
```

The call is guarded. A throwing function is reported back to the client as an error and cannot take the agent down. An argument written as `{"$ref": "path"}` is resolved through the path resolver to the live value first. Registration works while the agent is disabled, but the catalog is only published once it is enabled.

> [!TIP]
> A common pattern in real mods: register `*_debug_*` driver functions **only when the agent is enabled**. Check `mmapi_config_get(mmapi_config_load("mmapi"), "debug_enabled", false)` in your installer. A normal build then registers nothing and claims no keys, while a debug session can drive the mod's real code paths on demand.

### `mmapi_debug_resolve(path)` / `mmapi_debug_is_unresolved(value)`

Resolve a dotted path to its live value from mod code. Failure returns a distinct **unresolved sentinel** and never a throw. Test with `mmapi_debug_is_unresolved`, not against `undefined`. An existing field whose value is `undefined` resolves to `undefined`, which is different from "path not found".

```gml
var _wave = mmapi_debug_resolve("global.__arena_mod.wave");
if (!mmapi_debug_is_unresolved(_wave)) {
    mmapi_log_info("arena_mod", "wave: " + string(_wave));
}
```

### `mmapi_debug_set_enabled(on)`

Enables or disables the agent at runtime, bypassing (and thereafter shadowing) the config gate for this session. Enabling takes effect next frame: the hotkeys install and the file protocol comes alive.

> [!CAUTION]
> Resume before disabling. A disabled agent no longer drives the engine's pause flag, so a pause left set stays set.
