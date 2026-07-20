# Seam: dungeon_treasure_chest

Emits the moment a dungeon treasure chest starts its drop chain.

`dungeon_treasure_chest` is a **template seam** (`op = "emit"`). It feeds [dungeon.treasure_chest](../hooks/dungeon.treasure_chest.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Breakables.gml` |
| **Locator** | pristine context after the chest's `drop_chain` creation (`new_chain().append(LinkId.Timer, 40)`) |
| **Op** | `emit` |
| **Feeds** | [`dungeon.treasure_chest`](../hooks/dungeon.treasure_chest.md) |
| **ctx built** | `{ node: node, object_id: node.object_id, x: node.renderer.x, y: node.renderer.y }` |
| **Marker** | `mmapi_dungeon_run_treasure_chest_callbacks` |

## The Edit

The locator is pristine context inside the dungeon breakables logic: the emit is generated right after a treasure chest's `drop_chain` is created (`new_chain().append(LinkId.Timer, 40)`, the start of its drop chain) and before the logic that follows. The ctx is assembled from the node at emit time: the `node` itself, its `object_id`, and `x`/`y` read from `node.renderer`, i.e. the chest's on-screen position.

This is an event dispatch: handlers observe the moment (a chest is opening here, dropping soon) and their return values are ignored. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [dungeon.treasure_chest](../hooks/dungeon.treasure_chest.md) - This is the hook this seam dispatches.
- [items_treasure_distribution_none](items_treasure_distribution_none.md) - This seam lives in the same file. It filters the treasure distribution when the engine found no candidate.
- [items_treasure_distribution_result](items_treasure_distribution_result.md) - This seam lives in the same file. It filters the distribution the engine did pick.
- [dungeon_side_room_chance](dungeon_side_room_chance.md) - This seam tunes how often the rooms holding these chests spawn.
