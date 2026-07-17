# Mod Anatomy

[← MMAPI](MMAPI.md)

A mod is a folder with a manifest and a `gml/` folder. This page explains the boot file skeleton every mod carries, the lifecycle its code runs in, and the engine quirks that shaped both.

## The Folder

```text
my_first_mod/
├─ manifest.json
├─ gml/
│  ├─ MyFirstMod.gml
```

`gml/` may hold any number of `.gml` files, in subfolders if you like, and the installer copies them all. One file is plenty for most mods. Large mods split into domain folders (`gml/core/`, `gml/ui/`, and so on). Everything else in the folder (sprites, item definitions, localization) is MOMI's regular content-mod territory and installs through its own pipelines. The GML layer only handles `gml/`.

## One Name Everywhere

Pick one short snake_case name for your mod and use it for every name the mod owns:

- folder: `my_mod/`
- declaration: `mmapi_mod_declare("my_mod", ...)`
- state: `global.__my_mod`
- functions: `my_mod_*` and `__my_mod_*`

Top-level functions are global across every installed mod, so the name prefix is the only thing keeping two mods' functions apart. MOMI lints for unnamespaced top-level functions and for writes to reserved or foreign global roots. Fewer names, fewer mistakes.

MOMI derives the folder your `gml/` files land in inside the game's script tree from the manifest's `author` and `name` fields (see [The Manifest](MANIFEST.md#the-install-namespace)). Your code never references that folder, so it does not need to match your chosen name. Everything your code does reference should.

## The Boot File Skeleton

```gml
// My Mod

// 1. The lazy runtime struct. All mod state in one global, created on first use.
function __my_mod_runtime() {
    if (global[$ "__my_mod"] == undefined) {
        global.__my_mod = { registered_hooks: undefined, cfg: undefined };
    }
    return global.__my_mod;
}

// 2. The latched register function. Boot can re-run, this registers exactly once.
function my_mod_register_callbacks() {
    var _rt = __my_mod_runtime();
    if (_rt.registered_hooks != undefined) return;
    _rt.registered_hooks = true;

    mmapi_on("game.day_started", my_mod_day_started);
}

// 3. Named handler functions.
function my_mod_day_started(_ctx) {
    var _rt = __my_mod_runtime();
    if (_rt.cfg == undefined) {
        _rt.cfg = mmapi_config_load("my_mod"); // lazy: file IO is safe here, not at boot
    }
    // react to the new day
}

// 4. Boot wiring. The top level runs while the game is loading and is memory-only.
mmapi_mod_declare("my_mod", "1.0.0");
my_mod_register_callbacks();
```

### The Top Level is Memory-Only

Top-level code runs while the game is still loading, before the first frame, and **file IO throws in-engine there**. So the top level only declares the mod, defines functions, and registers handlers. Config loads lazily, the first time a handler needs it.

Logging is the one exception. Log calls are safe at boot because the file sink buffers in memory until the first frame.

### The Lazy Runtime Struct

All mod state lives in one global struct, created on first access through the `global[$ ...]` accessor. One struct keeps your state out of everyone else's way, and makes the whole mod watchable in the [debug agent](DEBUG.md) as `global.__my_mod`.

### The `registered_hooks` Latch

Boot can run more than once, and registration does not fully de-duplicate across runs. The hook engine skips an exact duplicate (same hook, function, kind, and mod) with a warning, but hotkey and modsave registrations land twice and fire twice, and the warning itself is log noise. The latch makes a re-run a harmless no-op.

### Named Handler Functions

Every callback (hook handlers, install functions, hotkey callbacks) is a named top-level function. Top-level functions are hoisted, so a registration can name a function defined lower in the file. This also allows the engine's stacktrace handler to generate precise line numbers for its backtraces.

## The Lifecycle

- **Game Boot**: Every installed file's top level runs (order unspecified, memory-only).
- **Frame 1** (in `step_begin`): The first drain. IO becomes ready, buffered logs flush, and queued functions run.
- **Every Frame After**: The drain re-runs every queued function; hooks dispatch as the game plays

### Top-Level Boot

Engine boot runs the top level of every installed file, mods and framework alike, in unspecified order. All top-level functions are hoisted and global, so entry points are callable from any mod regardless of load order.

What belongs at the top level: `mmapi_mod_declare`, function definitions, hook registrations, and an `mmapi_register` call when the mod needs per-frame work or a first-safe-moment callback.

### The First Drain

The installed framework drains the `mmapi_register` queue from the game's step begin, at the top of every frame. The first drain is the boundary between boot and gameplay time. `mmapi_io_is_ready()` flips to true, and every log line buffered during boot flushes to the per-mod log files.

### Every Frame

The queue is never cleared. A function queued with `mmapi_register` runs again **every frame, forever**. That makes it two things at once: the first safe moment for file IO, and the per-frame tick. It must be idempotent, so guard one-time work with a marker:

```gml
function my_mod_tick() {
    var _rt = __my_mod_runtime();
    if (_rt.cfg == undefined) { // run once
        _rt.cfg = mmapi_config_load("my_mod");
    }
    // per-frame work goes here
}

mmapi_register(my_mod_tick);
```

The every-frame re-run is deliberate. The engine rebuilds some instances mid-game (the clock on a new day, for example), and the next drain re-wraps them. Guard the wrap with a marker on whatever you wrap, and the re-run costs about nothing once installed.

A throwing queued function warns rate-limited, attributed to its mod, and the drain continues to the next one. The framework sets the current mod around each call, so hook registrations made inside the function attribute to the mod that queued it.

## Engine Landmines

The engine quirks every mod must respect. Each one has bitten a real mod during MMAPI development.

### `mod` is a Reserved Word

`mod` is the modulo operator in this dialect.

```gml
var mod = "my_mod"; // wrong: does not parse
var mod_name = "my_mod"; // right
```

### Avoid Closures

Write named top-level functions and pass those. This is more guidance than mandatory.

```gml
mmapi_on("game.day_started", function(_ctx) { }); // wrong: no closures in this dialect
mmapi_on("game.day_started", my_mod_day_started); // right: a named top-level function
```

### `is_struct` is False for Live Instances

If a value can be a game object instance, `is_struct` alone rejects it.

```gml
if (is_struct(owner)) { }                             // wrong: false for a live instance
if (is_struct(owner) || instance_exists(owner)) { }   // right
```

### Use `global[$ "name"]`

Test lazily-created globals with the accessor.

```gml
if (variable_global_exists("__my_mod")) { } // wrong: not guaranteed on this runtime
if (global[$ "__my_mod"] != undefined) { } // right
```

### `string_split` With an Absent Delimiter returns `[]`

The shipped engine returns an empty array when the delimiter never appears, not a one-element array.

```gml
var parts = string_split(text, "."); // [] when text contains no "."
if (array_length(parts) == 0) { parts = [text]; } // right: treat the whole string as one token
```

### `save_json_file` Crashes on Non-Plain Values

A method, an instance id, or a pointer anywhere in the data crashes the game inside the engine's serializer.

```gml
save_json_file(path, { fn: my_mod_tick }); // wrong: crashes in-engine
save_json_file(path, { count: 3, name: "ok" }); // right: numbers, strings, bools, arrays, plain structs only
```

### Hot Hooks Need a Cheap First Check

Some hooks fire per instance per frame. Make your handler's first check the cheapest one and exit early. See [Hooks](HOOKS.md#hot-hooks).

