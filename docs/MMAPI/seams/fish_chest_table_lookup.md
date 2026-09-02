# Engine Fix: fish_chest_table_lookup

Resolves an opened modded chest's loot table from its prototype's `fish_chest` key, keeping the pristine crash for unknown items and missing tables.

`fish_chest_table_lookup` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [fish_chest_item_use](fish_chest_item_use.md) and [fish_chest_custom_rarity](fish_chest_custom_rarity.md) it carries the `fish_chest` item-data contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor: the `OpenChest` item-id switch in the hold-to-use completion |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_fish_chest_table_lookup` |

## The Edit

When a held chest finishes opening, the engine switches on the item id to pick a `FISH_CHEST` table and treats any other item as impossible. The replace rewrites only the default branch. It reads the prototype's `fish_chest` key, stored by [fish_chest_item_use](fish_chest_item_use.md), and looks the table up with a struct read, since `FISH_CHEST` already holds every merged `chest_tables` entry by name. The five vanilla cases are untouched.

The pristine `impossible()` survives both remaining failure shapes. An item that reaches the switch without a `fish_chest` key crashes exactly as pristine does, and a key naming no merged table crashes with the same message rather than opening into nothing. The vanilla roll-and-drop code after the switch runs unchanged, including the gold payout, so `gold` is a required table field and a chest wanting no coins declares `gold = [0, 0]`.

## See Also

- [Treasure Chests](../TREASURE_CHESTS.md) - The mod-facing contract this edit carries.
- [fish_chest_item_use](fish_chest_item_use.md) - The parse-time edit that stores the key this lookup reads.
- [fish_chest_custom_rarity](fish_chest_custom_rarity.md) - The companion edit for chests that are also fishable.
- [items.dropped](../hooks/items.dropped.md) - Observes the loot this table resolution leads to.
