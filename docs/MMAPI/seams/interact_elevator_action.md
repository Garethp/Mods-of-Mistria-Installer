# Seam: interact_elevator_action

Puts a veto check on the elevator's interaction action, the press that opens the lift menu.

`interact_elevator_action` is a **template seam** (`op = "guard"`). It feeds [interact.elevator_action](../hooks/interact.elevator_action.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/dungeon/obj_dungeon_elevator.gml` |
| **Locator** | pristine context: the head of the interaction action closure that spawns `Menu.Elevator` |
| **Op** | `guard` |
| **Feeds** | [`interact.elevator_action`](../hooks/interact.elevator_action.md) |
| **ctx built** | `{ subject: self, key: "dungeon_elevator" }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_interact_run_elevator_action` |

## The Edit

The locator is pristine context: the head of the `function() { ... ANCHOR.spawn_menu(Menu.Elevator); }` closure, the interaction *action* the elevator registers. The generated check runs `mmapi_check_guards("interact.elevator_action", { subject: self, key: "dungeon_elevator" })`. When any guard returns `false`, the injected line runs `return;` and `Menu.Elevator` never opens. The seam sets `try_catch = false`, so the check is injected bare. Guard dispatch already fails open when a handler throws.

The catalog is explicit about why the guard sits on the action, not the condition. `register_interaction` takes the action as its 3rd argument and the interactability condition as its 4th, and the condition is polled every frame for every in-range interactable (`par_interactable`'s `has_potential_interactions`), so a side-effect placed there fired the "lift locked" conversation on any Interact press near the elevator, for example using the ladder at the mines entrance. On the action, the engine's facing and selection have already routed the press to the faced interactable, so the guard fires only on a real elevator press.

## See Also

- [interact.elevator_action](../hooks/interact.elevator_action.md) - This is the hook this seam dispatches.
- [interact_ladder_down_action](interact_ladder_down_action.md) - This is the other interaction-action guard, on the dungeon ladder.
