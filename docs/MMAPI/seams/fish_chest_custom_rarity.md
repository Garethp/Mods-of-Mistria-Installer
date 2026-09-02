# Engine Fix: fish_chest_custom_rarity

Makes an unknown chest rarity a no-op in the fishing distribution build instead of a Setup crash.

`fish_chest_custom_rarity` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [fish_chest_item_use](fish_chest_item_use.md) and [fish_chest_table_lookup](fish_chest_table_lookup.md) it carries the `fish_chest` item-data contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Fishing/Fish.gml` |
| **Locator** | text anchor: the chest-rarity switch default in `create_fish_distributions` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_fish_chest_custom_rarity` |

## The Edit

`create_fish_distributions()` runs during Setup, before the title screen. For every fish with `is_chest = true` it switches on the fish's rarity to apply the Treasure Trove and Unexpected Haul perk adjustments, and it treats any rarity outside the four vanilla chest tiers as impossible. The runtime mints `FishId` from the merged fiddle `fish` entries, so a mod adding a fishable chest with its own rarity reached this default and aborted the game at launch.

The replace turns the default into a break. A modded chest rarity gets no perk vote adjustments, and its base weight still comes from its `fishing/votes` entry, which every fish rarity must declare. The trailing Unexpected Haul condition then adds zero votes and is inert. The divespot distribution build has no such switch and already handles any chest rarity. Vanilla data enumerates all four tiers explicitly, so pristine data never reaches the default.

## See Also

- [Treasure Chests](../TREASURE_CHESTS.md) - The mod-facing contract this edit carries.
- [fish_chest_item_use](fish_chest_item_use.md) - The parse-time half of the same data contract.
- [fish_chest_table_lookup](fish_chest_table_lookup.md) - The open-time half of the same data contract.
- [fishing.should_reel](../hooks/fishing.should_reel.md) - The existing hook nearest the fishing flow this edit touches.
