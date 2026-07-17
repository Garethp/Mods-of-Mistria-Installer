# Seam: interact_ladder_down_action

Puts a veto check on the ladder's descend action, before the sound and the floor change.

`interact_ladder_down_action` is a **text seam** (`anchor` + `replace`). It feeds [interact.ladder_down_action](../hooks/interact.ladder_down_action.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/dungeon/par_ladder.gml` |
| **Locator** | text anchor: the head of the interaction action closure that plays the descend sound |
| **Feeds** | [`interact.ladder_down_action`](../hooks/interact.ladder_down_action.md) |
| **ctx built** | `{ subject: self, key: "dungeon_ladder_down" }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_interact_run_action_guards` |

## The Edit

The anchor is the ladder's interaction action closure, the `function()` literal whose first pristine statement is `TANGO.play("SoundEffects/Entrances/LadderDescend")`. The replace injects the guard as the closure's new first statement:

```gml
if (mmapi_check_guards("interact.ladder_down_action", { subject: self, key: "dungeon_ladder_down" }) == false) {
    return;
}
```

On veto the closure returns before the descend sound plays and before the floor-change logic that follows (the `if room() == rm_mines_entry` branch), so the player simply does not descend. Like the elevator guard, this fires on the actual press. The engine's facing and selection have already routed the interaction to this ladder. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [interact.ladder_down_action](../hooks/interact.ladder_down_action.md) - This is the hook this seam dispatches.
- [interact_elevator_action](interact_elevator_action.md) - This seam is the other interaction-action guard, on the dungeon elevator.
- [dungeon_ladder_spawn](dungeon_ladder_spawn.md) - This seam guards the ladder ever appearing, rather than its use.
