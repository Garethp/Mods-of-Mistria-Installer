# Seam: player_mount_speed

Filters the mounted base speed where `get_move_speed` chose it, before the shared status-effect multipliers.

`player_mount_speed` is a **text seam**. It feeds [player.mount_speed](../hooks/player.mount_speed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Anchor** | the closing walk branch of `get_move_speed`'s base-speed selection (`spd = on_mount ? MOUNT_WALK_SPEED : HUMAN_WALK_SPEED;`) |
| **Feeds** | [`player.mount_speed`](../hooks/player.mount_speed.md) |
| **Value filtered** | `spd` - the mounted base speed |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_mount_speed` |

## The Edit

The edit appends an `if on_mount` block right after `get_move_speed`'s base-speed selection, once the walk/run branch has picked `MOUNT_WALK_SPEED`, `MOUNT_RUN_SPEED`, or `MOUNT_HORSEPOWER_RUN_SPEED` (Horsepower perk), and before the `MineTime`/`SlimeDash`/`KillHaste` status-effect multipliers that mounted and on-foot movement share. Inside the block, `spd` is reassigned through `mmapi_apply_filters("player.mount_speed", spd, { player: self })` under the uniform try/catch shape, so a filtered base speed still picks up the engine's own status multipliers and then flows through the [player_move_speed](player_move_speed.md) dispatch like any vanilla value.

The dispatch is conditional on `on_mount`, which no template op can express, so the seam takes the text form. On foot the block is skipped and the hook never fires.

## See Also

- [player.mount_speed](../hooks/player.mount_speed.md) - This is the hook this seam dispatches.
- [player_move_speed](player_move_speed.md) - This is the sibling dispatch at the end of the same computation.
- [player_swim_speed](player_swim_speed.md) - This is the swimming counterpart in the same file.
