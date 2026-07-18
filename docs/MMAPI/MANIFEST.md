# The Manifest

[← MMAPI](MMAPI.md)

Every mod ships exactly one of `manifest.json` or `manifest.toml` at its root. It identifies the mod, states the MOMI version it needs, and lists the hooks it cannot run without. The examples use JSON; TOML accepts the parsed manifest fields noted below, but not the JSON-only `description`. An unpacked folder containing both prefers JSON, but a zip or rar containing both is rejected, so package only one.

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
| `description` | Optional JSON-only summary copied best-effort into the game's generated mod manifest. It is not part of the parsed TOML model. |
| `minInstallerVersion` | The minimum MOMI version the mod requires. Any mod shipping a `gml/` folder should set this no lower than `0.14.0`. |
| `manifestVersion` | The manifest schema version. |
| `requirements` | Optional array of `{ name, author, download_url }` dependency records. MOMI uses the derived author/name id for ordering and missing-dependency handling. |
| `download_url` | Optional fallback page or download URL for this mod. |
| `update_url` | Optional update source: a GitHub repository URL, or JSON returning `{ "version", "download_url" }`. |
| `requires_hooks` | The hook names the mod cannot run without, as an array of strings. |

## The Install Namespace

MOMI derives an id from lowercased `author.name`: spaces become underscores, and characters other than letters, digits, underscores, and dots are removed. There is no separate manifest `id` field. For the GML install namespace, dots in that derived id become underscores. For example, author `you` and name `My First Mod` derive id `you.my_first_mod` and install under `scripts/you_my_first_mod/`.

Two installed mods that derive the same namespace clash, and MOMI skips one with a message naming both. The fix is to change one mod's `name` or `author`.

> [!TIP]
> The install namespace is MOMI's concern, and your code never references it. What your code does reference is the names you choose for yourself: the mod folder, the `mmapi_mod_declare` name, the `global.__<name>` struct, the function prefix, and the `mod_data/<name>/` directory. Keep that one name consistent everywhere. See [One Name Everywhere](MOD_ANATOMY.md#one-name-everywhere).

## `requires_hooks`

Before installing anything, MOMI checks each listed name against the seam catalog and **skips the whole mod** when one is missing, with a message telling the user to update MOMI. Canonical names and declared aliases both satisfy the check, though new manifests should use the canonical name. A mod built for a newer catalog then reports one clear error instead of registering for hooks that never fire.

`requires_hooks` must be an array of strings. A malformed shape becomes a manifest validation error and skips the mod; it is not treated as an empty list.

> [!TIP]
> List every shipped hook you register. An unlisted hook still works when the catalog has it, but if it is ever missing the failure moves from one install-time message to a warning buried in a log file while the mod silently does nothing. Do not list custom hooks published by another mod; they are not catalog declarations.

## Validation: One Mod, One Fate

MOMI validates the whole mod before installing any of it. If validation fails, MOMI skips the **whole mod**, so its GML, items, sprites, and everything else stay out, and the reason is shown in the installer. The GML-layer reasons you may see:

- **Missing Required Hook**: A `requires_hooks` entry is missing from the installed catalog.
- **Does Not Compile**: The staged GML failed the compile check. The message includes the compiler's complaint.
- **Namespace Clash**: Two mods derive the same install namespace.
- **Export Collision**: A top-level function duplicates an engine, framework, earlier-mod, or same-mod definition.
- **Unsafe GML Path**: A path in `gml/` would escape or alias its assigned script directory.

MOMI also runs warn-tier lints over staged GML: unnamespaced top-level functions, writes to reserved `global.__mmapi*`, replacement of another mod's global root, unknown literal hook names, kind mismatches, alias use, override contention, and calls to undefined `mmapi_*` or `__mmapi_*` names. Deep, guarded writes to fields on another mod's root remain allowed. These warn by default rather than skip.

For CI and mod development, `--strict-lints` escalates file-bearing lint findings into a skip; cross-mod contention remains a warning. `--fail-on-skip` turns any skipped GML mod into a hard failure before the rebuild. See [Preflight One Mod](TROUBLESHOOTING.md#preflight-one-mod).
