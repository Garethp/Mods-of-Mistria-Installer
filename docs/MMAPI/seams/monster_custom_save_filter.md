# Engine Fix: monster_custom_save_filter

Writes only built-in monster names to the game's `monsters_killed` save field.

`monster_custom_save_filter` is an **engine fix**, an anchored edit with no hook behind it. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Serialization/SaveGame.gml` |
| **Locator** | text anchor on the `monsters_killed` field in the player save struct |
| **Op** | text (replacement helper call) |
| **Marker** | `mmapi_monster_save_filter` |

## The Edit

The save builder calls `mmapi_monster_save_kills(ARI.monsters_killed)` instead of passing the array directly to `array_to_struct`. The helper converts the array and removes names that are not in the pristine game's monster roster.

MOMI reads that roster from the same pristine archive it uses to stage the install.

## Why

The game restores `monsters_killed` by turning every saved name back into a `MonsterId` and indexing the vanilla kill array. A custom name can disappear when its mod is removed, and a generated id has no permanent slot in that array.

With no custom monsters installed, the helper returns the same `array_to_struct` result as the original code.

## See Also

- [Custom Monsters](../CUSTOM_MONSTERS.md) - How a mod can save its own species counts.
- [monster_custom_load_tolerance](monster_custom_load_tolerance.md) - Loads saves that already contain an unavailable name.
