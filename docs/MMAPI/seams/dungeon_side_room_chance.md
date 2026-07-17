# Seam: dungeon_side_room_chance

Routes the side-room spawn chance through the filter chain before the per-floor roll.

`dungeon_side_room_chance` is a **template seam** (`op = "filter"`). It feeds [dungeon.side_room_chance](../hooks/dungeon.side_room_chance.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml` |
| **Locator** | structural target: `try_create_side_room`, at head |
| **Op** | `filter` |
| **Feeds** | [`dungeon.side_room_chance`](../hooks/dungeon.side_room_chance.md) |
| **Value filtered** | `chance_val` - the side-room spawn chance (0-100) |
| **ctx built** | `{ impl: impl, is_ritual: impl == DungeonImpl.Ritual, max_flr: max_flr }` |
| **Marker** | `mmapi_dungeon_run_side_room_chance_filters` |

## The Edit

The generated filter lands at the head of `try_create_side_room()`, before the per-floor roll. It reassigns the function's `chance_val` through `mmapi_apply_filters("dungeon.side_room_chance", chance_val, ctx)`, so the roll that follows uses the filtered chance. Raise it toward 100 to force treasure or ritual side rooms, lower it toward 0 to suppress them.

The ctx literal precomputes `is_ritual` as `impl == DungeonImpl.Ritual`, so handlers can special-case ritual rooms without referencing the `DungeonImpl` enum. Both `impl` and `max_flr` ride along raw. The function runs once for every side-room impl the runner attempts, so a handler fires multiple times per floor. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [dungeon.side_room_chance](../hooks/dungeon.side_room_chance.md) - This is the hook this seam dispatches.
- [dungeon_ladder_spawn](dungeon_ladder_spawn.md) - This is the other `DungeonRunner.gml` seam.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - This seam fires when a treasure chest in one of these rooms starts its drop chain.
