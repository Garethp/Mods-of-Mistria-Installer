# Engine Fix: monster_custom_load_tolerance

Drops an unavailable monster name from `monsters_killed` instead of failing the save load.

`monster_custom_load_tolerance` is an **engine fix**, an anchored edit with no hook behind it. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `apply_struct_to_array` call for `monsters_killed` |
| **Op** | text (replacement helper call) |
| **Marker** | `mmapi_monster_load_kills_guard` |

## The Edit

The loader calls `mmapi_monster_load_kills()` for the saved `monsters_killed` struct. The helper restores every available built-in name. If a name no longer resolves, it skips that entry and writes a warning containing the name.

When no custom monsters are installed and every name resolves, the helper calls the game's original `apply_struct_to_array` path.

## Why

The original loader assumes every saved name still belongs to the current `MonsterId` roster. That assumption stops holding when a save was written with a monster mod that is no longer installed. The failed lookup otherwise reaches an invalid array index and can trip the game's assertion path.

After the unknown entry is skipped, the next save writes the cleaned table through [monster_custom_save_filter](monster_custom_save_filter.md). Custom per-species counts are not retained by the game; mods that need them can use [MMAPI mod save data](../API_REFERENCE.md#mod-save-files).

## See Also

- [Custom Monsters](../CUSTOM_MONSTERS.md) - The package and removal behavior for custom monsters.
- [monster_custom_save_filter](monster_custom_save_filter.md) - Prevents new custom names from entering the vanilla table.
