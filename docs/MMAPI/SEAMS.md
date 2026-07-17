# Seams

[← MMAPI](MMAPI.md)

A **seam** is a small, anchored edit to one engine script that makes one hook fire. A dispatch line inserted at a precise, verified location in the game's own GML. For example, the seam behind `audio.play_guard` inserts a `mmapi_check_guards(...)` call into the engine's audio-play path, and the seam behind a filter hook threads a value through `mmapi_apply_filters(...)` before the engine uses it.

> [!IMPORTANT]
> **Mod authors never write seams in their mods.** You register handlers for the **hooks** the seams dispatch. Seams are authored, verified, and shipped with MOMI. If you need a new seam added, create a Pull Request.


## The Seam Catalog

The catalog is a single TOML file embedded in MOMI (`seams.toml`). It declares:

- **Hooks**: Every hook's `name`, `kind` (event, filter, guard, or override), a `doc` line describing when it fires and what its `ctx` contains, its `contention` class (for overrides), and any `aliases` from old names. Some hooks are marked `provider = "runtime"`, meaning the framework emits them itself with no engine edit behind them. `game.day_started`, `game.room_changed`, and `game.title_entered` are among these.
- **Seams**: The anchored engine edits that dispatch the hooks. The shipped catalog declares on the order of all hooks fed by all seams.
- A couple of **engine fixes** (hook-less edits, including the framework's own lifecycle root) and **call rewrites** (rewriting direct engine calls to a dispatching wrapper, which is how every `local_get` text lookup becomes filterable).

At install time the catalog is also rendered into `mmapi_hook_catalog.gml`, the runtime file that registration checks and the introspection functions read. See [Hooks](HOOKS.md#the-installed-catalog).

### Where Hooks are Documented

Each `[[hook]]` stanza in the catalog carries the hook's contract in its `doc` line.

That file, `ModsOfMistriaInstallerLib/Seam/Payload/seams.toml` in the MOMI source, is the authoritative hook reference. At runtime, `mmapi_hook_exists` and `mmapi_hook_kind` answer against the installed copy.

## How MOMI Applies Seams

MOMI keeps the game's assets in a compressed archive, with a pristine backup (`assets.bak.zip`) taken before any modding. On every install:

1. The GML layer stages everything **in memory against the pristine backup**: The MMAPI framework files, the seam edits, the generated hook catalog, and each mod's `gml/` tree.
2. Each seam's anchor must match its engine file **exactly once**. Any anchor that fails to match aborts the whole install, with the previous install still live and untouched.
3. Every mod's `requires_hooks` list is checked against the declared hooks. A mod needing a hook the catalog lacks is skipped whole, with a message telling the user to update MOMI.
4. The staged tree is compile-checked, then written into a whole-archive rebuild.

Because every install re-derives the modified scripts from pristine data, the operation is **idempotent and self-healing**. Running it twice produces the same result. Removing a mod is just re-running the install without it. A damaged install is repaired by the next install.

> [!NOTE]
> With zero handlers registered, every seam is designed to be behaviorally identical to the pristine game. An installed framework with no mods does nothing.

## Game Updates

A game update rewrites engine scripts, so every seam anchor must be re-checked against the new build. Two protections cover this:

- **Install-time**: Staging fails closed on the first anchor that no longer matches, so MOMI never writes against a build the catalog does not fit.
- **On demand**: The MOMI command line has a read-only check, `--verify` (or `--verify-json` for machine-readable output), that reports whether every seam still anchors cleanly against a build and writes nothing. Exit code `0` means all anchors hold.

When a game update breaks anchors, the fix is a MOMI update carrying a re-verified catalog. Your mod's code and manifest do not change unless absolutely necessary. That is the point of hooks being the supported surface.

## What This Means For Your Mod

- Register only hooks the catalog declares. Check the catalog (or `mmapi_hook_exists`) rather than guessing names.
- List every hook you register in `requires_hooks` so a missing hook becomes one clear install-time message instead of a silently dead mod. See [The Manifest](MANIFEST.md#requires_hooks).
- Prefer hooks and `mmapi_*` helpers over patterns that depend on engine internals. The catalog is what gets re-verified against each game build.
- Create a Pull Request if you need new hooks added to the catalog.
