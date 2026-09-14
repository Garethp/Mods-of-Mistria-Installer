# Seam: dungeon_side_room_range

Routes the floor span of a side room through the filter chain before the uniform floor pick.

`dungeon_side_room_range` is a **template seam** (`op = "filter"`). It feeds [dungeon.side_room_range](../hooks/dungeon.side_room_range.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml` |
| **Locator** | structural target: `try_create_side_room`, at head |
| **Op** | `filter` |
| **Feeds** | [`dungeon.side_room_range`](../hooks/dungeon.side_room_range.md) |
| **Value filtered** | `range` - the floor span for the side room's uniform floor pick |
| **ctx built** | `{ impl: impl, is_ritual: impl == DungeonImpl.Ritual, start_floor: start_floor }` |
| **Marker** | `mmapi_dungeon_run_side_room_range_filters` |

## The Edit

The generated filter lands at the head of `try_create_side_room()`, before the uniform floor pick. It reassigns the function's `range` parameter through `mmapi_apply_filters("dungeon.side_room_range", range, ctx)`, so the pick that follows draws the side room's floor from `start_floor` through `start_floor` plus the filtered span. Shrink it toward 0 to pin the room near the entry floor, or widen it to spread the room deeper, where a pick at floor 100 or past it yields no side room.

The ctx literal precomputes `is_ritual` as `impl == DungeonImpl.Ritual`, so handlers can single out ritual chambers without referencing the `DungeonImpl` enum. Both `impl` and `start_floor` ride along raw. The function runs once for every side room impl the runner attempts, after the engine's perk gates and its limit of one attempt per day, so a handler may fire zero, one, or two times as a run begins. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [dungeon.side_room_range](../hooks/dungeon.side_room_range.md) - This is the hook this seam dispatches.
- [dungeon_ladder_spawn](dungeon_ladder_spawn.md) - This is the other `DungeonRunner.gml` seam.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - This seam fires when a treasure chest in one of these rooms starts its drop chain.
