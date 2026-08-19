# Seam: player_pass_out

Emits inside `pass_out()`, right after `end_day()`.

`player_pass_out` is a **template seam** (`op = "emit"`). It feeds [player.pass_out](../hooks/player.pass_out.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/pass_out.gml` |
| **Locator** | structural target: `pass_out`, at after `end_day();` |
| **Op** | `emit` |
| **Feeds** | [`player.pass_out`](../hooks/player.pass_out.md) |
| **ctx built** | `{ faint: faint }` |
| **Marker** | `mmapi_player_pass_out` |

## The Edit

The generated emit lands in `pass_out(faint)` immediately after the `end_day();` call. It calls `mmapi_emit("player.pass_out", { faint: faint })` in the uniform try/catch shape. The placement is a deliberate middle ground. The invulnerability flag, the stamina zeroing, and the day-end commit have happened, while the held-animal put-down, the faint stat and 2 AM notification, and the faint sound are still to come.

`pass_out()` has exactly two kinds of caller in the engine: the 2 AM collapse paths (`faint = true`, from `should_faint()`'s swim and land variants) and the averted death (`faint = false`, from `should_die()`'s fade-out callback). A normal sleep ends the day without it, and a death that is not averted takes the dying scene instead. The emit's [player_died](player_died.md) sibling covers that branch. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.pass_out](../hooks/player.pass_out.md) - This is the hook this seam dispatches.
- [player_died](player_died.md) - This is the sibling emit on the final death branch.
- [new_day_complete](new_day_complete.md) - This is the emit inside the engine's new-day work that follows the day end.
