# Quick Start

[← MMAPI](MMAPI.md)

The fastest way to create an MMAPI mod is to build a small one end to end. This includes:
- A folder to house the mod.
- A manifest file to describe the mod.
- One GML file with a single hook handler.

This page explains building `my_first_mod`, which displays an in-game notification everytime a new day starts.

## Requirements

- Fields of Mistria, the game.
- MOMI version 0.13.0 or newer. That is the first version that ships the GML layer.
- A text editor. There is no compiler or build step necessary. A mod is plain GML source.

## (MOVE) Naming Conventions

Pick one name and use it everywhere: the folder, the `global.__my_first_mod` state struct, and the `my_first_mod_*` function prefix in code.

## The Folder

A mod lives inside a folder. As stated above, an MMAPI mod must contain a manifest and GML file inside of it.

Here's a directory tree view to help you visualize:
```text
my_first_mod/
├─ manifest.json
├─ gml/
│  ├─ MyFirstMod.gml
```

The full folder paths for each file should look like:
```
/my_first_mod/manifest.json
/my_first_mod/gml/MyFirstMod.gml
```


## The Manifest

`manifest.json` belongs at the root of the mod folder. The contents of it are:

```json
{
    "name": "My First Mod",
    "author": "you",
    "version": "1.0.0",
    "description": "Shows a notification when a new day starts.",
    "minInstallerVersion": "0.13.0",
    "manifestVersion": 1,
    "requires_hooks": ["game.day_started"]
}
```

Most of these manifest fields are shared in-common across all MOMI mod types, including non-MMAPI ones. The exception is `requires_hooks`.

`requires_hooks` lists every hook the mod **registers**. MOMI checks the list against the seam catalog before installing anything, and skips the mod with a clear message when a hook is missing. See [The Manifest](MANIFEST.md) for additional information about this file and its fields.

## The Boot File

`gml/MyFirstMod.gml` contains the code for this MMAPI mod. Note that it's a single GML file.

```gml
// My First Mod

// Runtime state initialization.
function __my_first_mod_runtime() {
    if (global[$ "__my_first_mod"] == undefined) {
        global.__my_first_mod = { registered_hooks: undefined };
    }
    return global.__my_first_mod;
}

// Hook callback registration.
function my_first_mod_register_callbacks() {
    var _rt = __my_first_mod_runtime();
    if (_rt.registered_hooks != undefined) return;
    _rt.registered_hooks = true;

    mmapi_on("game.day_started", my_first_mod_day_started); // EVENT hook registration
}

// Hook callback.
// game.day_started is an EVENT. MMAPI calls you after it happens.
function my_first_mod_day_started(_ctx) {
    // _ctx contains { total_days }.
    create_notification("Day " + string(_ctx.total_days) + " begins.");
    mmapi_log_info("my_first_mod", "day started: " + string(_ctx.total_days));
}

// MMAPI mod declaration and hook registration.
mmapi_mod_declare("my_first_mod", "1.0.0");
my_first_mod_register_callbacks();
```

> [!TIP]
> Every piece of this skeleton exists for an engine reason. the memory-only top level, the lazy runtime struct, the `registered_hooks` latch, the named handler function. [Mod Anatomy](MOD_ANATOMY.md) explains each one.

## Install The Mod

Install with MOMI like any other mod. Put the folder in your `/mods` directory and run the installer (GUI or command line). MOMI installs the MMAPI framework, applies the seam catalog to the game's scripts, and copies your `gml/` tree into the game's script tree.

If the mod is skipped instead of installed, MOMI shows the reason: A missing required hook, GML that does not compile, or an install-namespace clash with another mod. A skipped mod is skipped whole, so none of its content installs.

To remove the mod, remove it from the `/mods` directory and run the installer again. Every install rebuilds the game's scripts from pristine data, so removal and repair are the same operation as install.

## Run It

Start the game, load a save, and sleep. The notification appears when the new day starts.

## Check The Logs

The log file is created automatically:

```text
%LOCALAPPDATA%/FieldsOfMistria/mod_data/my_first_mod/logs/my_first_mod.log
```

The `mmapi_log_info` call in the handler proves the hook fired:

```text
[INFO ] day started: 24
```

If nothing appears, look for warnings in the same file. An unknown hook name, a wrong registration directive, and a duplicate registration each output a `WARN` log there.

## Next Steps

- Read [Hooks](HOOKS.md) for the four hook kinds and how registration and dispatch work.
- Read [Mod Anatomy](MOD_ANATOMY.md) for why the boot file looks the way it does, and for the engine quirks that will bite you if you skip it.
- Browse the [API Reference](API_REFERENCE.md) when you need a helper: config, hotkeys, per-save data, combat.
- Turn on the [debug agent](DEBUG.md) when a hook doesn't seem to fire.
