# Hook: interact.elevator_action

Block the dungeon elevator before its menu opens.

`interact.elevator_action` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when the player actually activates the dungeon elevator, the interaction action, not the per-frame interactability poll. ctx is `{ subject, key: "dungeon_elevator" }`. Return `false` to veto opening the elevator menu (`Menu.Elevator` never spawns). `undefined` or `true` allows.

This is an action guard by design. A registered interaction carries an action (the third `register_interaction` argument) and an interactability condition (the fourth), and the condition is polled every frame for every in-range interactable (`par_interactable`'s `has_potential_interactions`). A side effect on the condition side fired on any Interact press near the elevator, for example using the ladder at the mines entrance. On the action side, the engine's facing and selection have already routed the press to the faced interactable, so your callback runs only on a real elevator press.

| | |
| --- | --- |
| **Fires** | Inside the dungeon elevator's interaction action, just before the elevator menu spawns. |
| **ctx** | `{ subject, key: "dungeon_elevator" }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `subject` - the `obj_dungeon_elevator` instance the player pressed.
- `key` - Always `"dungeon_elevator"`. Use it to share one handler across the interact-action guards.

## Usage

```gml
// interact.elevator_action is a GUARD: return false to block it, undefined
// (or true) to allow. Guards fail OPEN - if your handler crashes, the action happens.
function lift_keeper_interact_elevator_action(_ctx) {
    // _ctx is { subject, key }.
    //   .subject - the obj_dungeon_elevator instance the player pressed.
    //   .key     - always "dungeon_elevator".
    // Fires only on a real elevator press - never on the per-frame
    // interactability poll.
    // if (<your condition>) {
    //     return false; // veto - Menu.Elevator never opens
    // }
    return undefined; // allow the elevator menu
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("interact.elevator_action", lift_keeper_interact_elevator_action);
```

## Engine Wiring

- Seam [`interact_elevator_action`](../seams/interact_elevator_action.md) dispatches from `gml/objects/dungeon/obj_dungeon_elevator.gml`, inside the elevator's interaction action, just before `ANCHOR.spawn_menu(Menu.Elevator)`. On veto the engine runs `return;`.

## See Also

- [interact.ladder_down_action](interact.ladder_down_action.md) - This hook has the same action-guard shape for the dungeon ladder.
- [input.take_press](input.take_press.md) - Veto any registered interaction press generically.
- [ui.menu_opened](ui.menu_opened.md) - Know when the elevator menu (or any menu) opens.
