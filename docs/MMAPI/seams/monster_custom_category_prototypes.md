# Engine Fix: monster_custom_category_prototypes

Adds the prototypes for custom monster categories after the game finishes its own monster prototypes.

`monster_custom_category_prototypes` is an **engine fix**, an anchored edit with no hook behind it. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Combat/Monsters.gml` |
| **Locator** | text anchor on the return at the end of `create_monster_prototypes` |
| **Op** | text (call before return) |
| **Marker** | `mmapi_monster_custom_category_prototypes` |

## The Edit

The edit passes the completed `monster_prototypes` array to `__mmapi_monster_build_custom_prototypes()` before returning it. MOMI fills that helper's descriptor table from the installed `momi/monster_categories/*.toml` files.

For each category, the helper reads its Fiddle table and follows the same steps as the native builder: apply `[default]`, build the sprite and audio catalogues, resolve the GML object and hitboxes, and parse the drop bundle. It writes the result into the slot assigned to the monster's generated `MonsterId`.

## Why

The game's builder visits each built-in category through a hardcoded `MonsterCategory` branch. A monster in a new category gains a `MonsterId`, but no native branch visits that category's Fiddle file.

MOMI keeps custom categories outside the save-facing `MonsterCategory` enum. Their runtime numbers begin after `MonsterCategory.LEN` and only select the state and sprite layout while the game is running. The stable name is the monster's Fiddle key.

With no custom categories installed, the descriptor table is empty and the completed native array is returned unchanged.

## See Also

- [Custom Monsters](../CUSTOM_MONSTERS.md) - The package format this edit supports.
- [monster_custom_save_filter](monster_custom_save_filter.md) - Keeps install-dependent names out of the game's saved kill table.
