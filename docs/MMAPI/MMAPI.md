# MMAPI

MMAPI is the hook framework for [Fields of Mistria](https://www.fieldsofmistria.com/) mods written in GML. An MMAPI mod contains a folder of plain GML source.

MOMI installs the framework, applies the **seam catalog** to the game's own scripts, and installs each mod's `gml/` folder alongside them. At runtime, everything compiles into the game as one program.

Mods talk to the game through **named hooks**, which are moments the seam catalog exposes, like `game.day_started` or `items.use_guard`. Alongside the hooks, a small runtime of `mmapi_*` helpers covers logging, config, per-save data, hotkeys, and more.

## Table of Contents

| Page | What it Covers |
| ---- | -------------- |
| [Quick Start](QUICK_START.md) | Build and install a working mod end to end in a few minutes. |
| [Hooks](HOOKS.md) | The named-hook engine. The four hook kinds, registration, ordering, dispatch, and error isolation. |
| [Seams](SEAMS.md) | How hooks come to exist. The seam catalog, how MOMI applies it to the game's scripts, and what happens on game updates. |
| [Mod Anatomy](MOD_ANATOMY.md) | The mod folder, the boot file skeleton, the lifecycle, and the engine quirks every mod must respect. |
| [The Manifest](MANIFEST.md) | Every `manifest.json` field a GML mod uses, and how MOMI validates it. |
| [API Reference](API_REFERENCE.md) | The `mmapi_*` helper areas: config, logging, per-save data, hotkeys, localization, cross-mod coordination, and calling the engine directly. |
| [Debug](DEBUG.md) | The in-game debug agent. Using and setting watches, breakpoints, pause and step, and debugger-callable functions. |

## The Big Picture

### Hooks

Every hook has exactly one **kind**, and each kind has its own registration **directive**:

| Kind | Directive | The Callback's Contract |
| ---- | --------- | ----------------------- |
| event | `mmapi_on` | Observes after the fact. Receives `ctx`. The return value is ignored. |
| filter | `mmapi_filter` | Receives `(value, ctx)`. Returns a replacement value, or `undefined` to keep the current one. |
| guard | `mmapi_guard` | Receives `ctx`. Returns `false` to veto the action, or `undefined` or `true` to allow it. |
| override | `mmapi_override` | Receives `ctx`. Returns a value to replace the engine's whole answer, or `undefined` to defer. The first non-`undefined` result wins. |

Registering with the wrong directive is the classic mistake: the handler never runs, and the only clue is a warning in the log. See [Hooks](HOOKS.md).

### The Lifecycle

A mod's code runs in three phases:
1. Top-level boot runs every installed file while the game is still loading. The order is unspecified, and boot is **memory-only** because file IO throws in-engine there. The only exception is **logging**, which MMAPI explicitly handles by buffering the output until the first in-game frame.
2. On the first frame, the game's step begin drains the `mmapi_register` queue, which is the first moment file IO works.
3. The drain then repeats every frame, so registered functions double as per-frame ticks and must be idempotent. See [Mod Anatomy](MOD_ANATOMY.md).

### Error Isolation

Every call into mod code is guarded. A throwing handler logs a rate-limited warning attributed to its mod, and the chain continues.

A throwing **filter** keeps the current value, a throwing **guard** counts as allow, and a throwing **event** or **override** handler is skipped. One broken mod does not take down another.

### Logging

> [!NOTE]
> The Fields of Mistria runtime limits where mods are capable of writing files to. The `%LOCALAPPDATA%/FieldsOfMistria` directory used by the game is the only confirmed write-approved location. To account for this, MMAPI automatically scopes all writes to the `/mod_data` sub-directory it creates.

Each mod automatically gets its own log file at `%LOCALAPPDATA%/FieldsOfMistria/mod_data/<mod>/logs/<mod>.log` adjacent to the game's save directory, plus a colored console line output.

Logging is safe at boot. Boot-time lines buffer in memory and land on disk on the first frame.

### No Unregistration

There is no unregistration API. A registered handler stays for the session. To turn behavior off at runtime, gate the handler with a flag in your mod's runtime struct and return early.

## A Note on Style

Prefer the catalog hooks and the `mmapi_*` helpers over reaching into engine internals. The seam catalog is verified against each game build, so the hooks are the surface that survives updates.

That said, most game behavior needs no hook at all. The engine's own scripts are plain GML and can be called directly from any handler. See [Calling the Engine Directly](API_REFERENCE.md#calling-the-engine-directly).
