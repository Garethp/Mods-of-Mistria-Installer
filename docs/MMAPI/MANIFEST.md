# The Manifest

[← MMAPI](MMAPI.md)

Every mod ships a `manifest.json` at its root. It identifies the mod, states the MOMI version it needs, and lists the hooks it cannot run without.

```json
{
    "name": "My First Mod",
    "author": "you",
    "version": "1.0.0",
    "description": "Shows a notification when a new day starts.",
    "minInstallerVersion": "0.14.0",
    "manifestVersion": 1,
    "requires_hooks": ["game.day_started"]
}
```

## Fields

| Field | Description |
| ----- | ----------- |
| `name` | The display name shown to players. Together with `author`, it also derives the install namespace (see below). |
| `author` | The author name. |
| `version` | The mod's own version string. |
| `description` | A one-line summary shown in MOMI. |
| `minInstallerVersion` | The minimum MOMI version the mod requires. Any mod shipping a `gml/` folder should set this no lower than `0.14.0`. |
| `manifestVersion` | The manifest schema version. |
| `requires_hooks` | The hook names the mod cannot run without, as an array of strings. |

## The Install Namespace

MOMI derives an install namespace for each mod from `author` and `name`, lowercased with spaces as underscores. That namespace names the folder your `gml/` files land in inside the game's script tree (dots and dashes in it become underscores there).

Two installed mods that derive the same namespace clash, and MOMI skips one with a message naming both. The fix is to change one mod's `name` or `author`.

> [!TIP]
> The install namespace is MOMI's concern, and your code never references it. What your code does reference is the names you choose for yourself: The mod folder, the `mmapi_mod_declare` name, the `global.__<name>` struct, the function prefix, and the `mod_data/<name>/` directory. Keep that one name consistent everywhere. See [One Name Everywhere](MOD_ANATOMY.md#one-name-everywhere).

## `requires_hooks`

Before installing anything, MOMI checks each listed name against the seam catalog and **skips the mod whole** when one is missing, with a message telling the user to update MOMI. A mod built for a newer catalog then reports one clear error instead of registering for hooks that never fire.

`requires_hooks` must be an array of strings. Anything else fails the manifest load.

> [!TIP]
> List every hook you register. An unlisted hook still works when the catalog has it, but if it is ever missing the failure moves from one install-time message to a warning buried in a log file while the mod silently does nothing.

## Validation: One Mod, One Fate

MOMI validates the whole mod before installing any of it. If validation fails, the mod is skipped **whole**, so its GML, items, sprites, and everything else stay out, and the reason is shown in the installer. The GML-layer reasons you may see:

- **Missing Required Hook**: A `requires_hooks` entry is missing from the installed catalog.
- **Does Not Compile**: The staged GML failed the compile check. The message includes the compiler's complaint.
- **Namespace Clash**: Two mods derive the same install namespace.

MOMI also runs warn-tier lints over staged GML: Unnamespaced top-level functions, writes to reserved `__mmapi` or foreign global roots, unknown hook names, kind mismatches, alias use, override contention, and calls to undefined `mmapi_*` names. These warn by default rather than skip.
