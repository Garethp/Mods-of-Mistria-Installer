# Seam: fish_chest_table_lookup

Resolves an opened chest's loot table, modded chests included, and announces the open before the drops.

`fish_chest_table_lookup` is a **text seam**. It feeds [items.chest_opened](../hooks/items.chest_opened.md) and carries the open-time half of the [`fish_chest`](../TREASURE_CHESTS.md) item-data contract. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor: the `OpenChest` item-id switch in the hold-to-use completion |
| **Feeds** | [`items.chest_opened`](../hooks/items.chest_opened.md) |
| **Marker** | `mmapi_fish_chest_table_lookup` |

## The Edit

When a held chest finishes opening, the engine switches on the item id to pick a `FISH_CHEST` table and treats any other item as impossible. The replace rewrites the default branch and adds one dispatch after the switch.

The default branch reads the prototype's `fish_chest` key, stored by [fish_chest_item_use](fish_chest_item_use.md), and looks the table up with a struct read, since `FISH_CHEST` already holds every merged `chest_tables` entry by name. The five vanilla cases are untouched. The pristine `impossible()` survives both remaining failure shapes. An item that reaches the switch without a `fish_chest` key crashes exactly as pristine does, and a key naming no merged table crashes with the same message rather than opening into nothing.

After the switch, the seam emits `items.chest_opened` with `{ live_item, table }` in the uniform try/catch shape (catch var `__mmapi_items_chest_opened`). The emit lands only on a successful resolution, for vanilla and modded chests alike, before the loot roll and gold payout run. With zero handlers the emit early-outs on an empty registry, and with no modded data the rewritten default branch is unreachable, so pristine behavior is preserved on both axes.

The vanilla roll-and-drop code after the dispatch runs unchanged, including the gold payout, so `gold` is a required table field and a chest wanting no coins declares `gold = [0, 0]`.

## See Also

- [items.chest_opened](../hooks/items.chest_opened.md) - This is the hook this seam dispatches.
- [Treasure Chests](../TREASURE_CHESTS.md) - The mod-facing contract this seam carries.
- [fish_chest_item_use](fish_chest_item_use.md) - The parse-time engine fix that stores the key this lookup reads.
- [fish_chest_custom_rarity](fish_chest_custom_rarity.md) - The companion engine fix for chests that are also fishable.
