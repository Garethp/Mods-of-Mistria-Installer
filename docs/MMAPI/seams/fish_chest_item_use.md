# Engine Fix: fish_chest_item_use

Lets a fiddle item declaring `fish_chest` take `ItemUse.OpenChest`, carrying its loot-table key on the prototype.

`fish_chest_item_use` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with the [fish_chest_table_lookup](fish_chest_table_lookup.md) seam and the [fish_chest_custom_rarity](fish_chest_custom_rarity.md) fix it carries the `fish_chest` item-data contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/Items.gml` |
| **Locator** | text anchor: the OpenChest branch of the use-assignment chain in `create_item_prototypes` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_fish_chest_item_use` |

## The Edit

The engine assigns almost every `ItemUse` from data fields on the item's fiddle entry. The OpenChest branch is an exception. It matches five hardcoded item ids, so no modded item can become an openable chest. The replace widens the branch's condition so an item whose fiddle entry declares `fish_chest` also takes `ItemUse.OpenChest`, and copies the declared key onto the prototype for [fish_chest_table_lookup](fish_chest_table_lookup.md) to read at open time.

The field's value names an entry under fiddle `fishing/chest_tables`, the same registry `create_fish_chests()` already walks generically during Setup. Everything downstream of the use assignment keys on the `ItemUse` rather than the item id, so a modded chest inherits the hold-to-use flow, the open glyph, the mimic-disguise exclusion, the non-giftable rule, and the single-stack rule unchanged. No vanilla item carries the field, so pristine data never reaches the widened condition.

## See Also

- [Treasure Chests](../TREASURE_CHESTS.md) - The mod-facing contract this edit carries.
- [fish_chest_table_lookup](fish_chest_table_lookup.md) - The open-time lookup that consumes the key this edit stores.
- [fish_chest_custom_rarity](fish_chest_custom_rarity.md) - The companion edit for chests that are also fishable.
- [items.use_guard](../hooks/items.use_guard.md) - A guard over using any item, modded chests included.
