# Seam: dungeon_ladder_spawn

Puts a veto check at the head of `spawn_ladder()`, before a floor's exit ladder appears.

`dungeon_ladder_spawn` is a **template seam** (`op = "guard"`). It feeds [dungeon.ladder_spawn](../hooks/dungeon.ladder_spawn.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml` |
| **Locator** | structural target: `spawn_ladder`, at head |
| **Op** | `guard` |
| **Feeds** | [`dungeon.ladder_spawn`](../hooks/dungeon.ladder_spawn.md) |
| **ctx built** | `{ runner: self, x: x_pos, y: y_pos }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_dungeon_run_ladder_spawn_guards` |

## The Edit

The generated guard lands at the head of `DungeonRunner.spawn_ladder()`. It calls `mmapi_check_guards("dungeon.ladder_spawn", { runner: self, x: x_pos, y: y_pos })` (`x_pos` and `y_pos` are the tile coordinates the runner is about to place the ladder at) and when any guard returns `false`, the injected line runs `return;`: no ladder spawns on this call. With zero handlers the seam is behaviorally identical to pristine. The guard check early-outs on an empty registry.

## See Also

- [dungeon.ladder_spawn](../hooks/dungeon.ladder_spawn.md) - This is the hook this seam dispatches.
- [dungeon_side_room_chance](dungeon_side_room_chance.md) - This is the other seam in `DungeonRunner.gml`.
- [interact_ladder_down_action](interact_ladder_down_action.md) - This seam guards using a ladder rather than spawning one.
