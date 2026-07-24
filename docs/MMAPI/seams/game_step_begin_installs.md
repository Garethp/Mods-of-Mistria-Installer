# Engine Fix: game_step_begin_installs

Installs the MMAPI per-frame drain at the top of the game's `step_begin`.

`game_step_begin_installs` is an **engine fix**, an anchored edit with no hook behind it. There is nothing to register for here. The edit itself is what makes registration work. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Game.gml` |
| **Locator** | text anchor at the head of `Game`'s `step_begin`, before `TICK++` |
| **Feeds** | - (no hook) |
| **Marker** | `mmapi_run_installs();` |

## The Edit

This is the MMAPI lifecycle root. The replace inserts one line, `mmapi_run_installs();`, as the first statement of `Game`'s `step_begin`, ahead of `TICK++`, ahead of everything the game does each frame.

That call drains the `mmapi_register` queue at the top of every frame. The first drain, on frame 1, is the boundary between boot and gameplay time: `mmapi_io_is_ready()` flips to true, accepted log lines buffered during boot flush to the per-mod log files, and every queued function runs. The queue is never cleared, so every registered function runs again every frame after (the first safe moment for file IO and the per-frame tick, one mechanism). See [Mod Anatomy](../MOD_ANATOMY.md) for the lifecycle this line anchors.

Every runtime-provided hook and every per-frame mod tick ultimately runs because this edit exists.

## See Also

- [Mod Anatomy](../MOD_ANATOMY.md) - This page describes the boot / first-drain / every-frame lifecycle this edit roots.
- [camera_culls_processed](camera_culls_processed.md) - This is a seam whose timing story is defined relative to this begin_step tick.
- [shroom_puddle_mask](shroom_puddle_mask.md) - This is another of the catalog's engine fixes, a beta-wiring correction.
- [statue_hp_death_sweep](statue_hp_death_sweep.md) - This is another of the catalog's engine fixes, the griffin statue's missing death check.
