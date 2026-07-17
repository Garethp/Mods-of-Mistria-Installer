# Hook: input.take_press

Block an interactable's press before the interaction runs.

`input.take_press` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `par_interactable` when a registered interaction's input reads as pressed, before the interaction runs. ctx is `{ subject, input_id, local_key, interaction }`. Return `false` to veto the press (a `force_press` still goes through). `undefined` or `true` allows.

| | |
| --- | --- |
| **Fires** | In `par_interactable`, when a registered interaction's input reads as pressed. |
| **ctx** | `{ subject, input_id, local_key, interaction }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `subject` - the `par_interactable` instance whose interaction is firing.
- `input_id` - the input id the interaction is registered on (`interaction.input_id`).
- `local_key` - the interaction's registered `local_key`, identifying which interaction this is.
- `interaction` - the registered interaction struct itself.

## Usage

```gml
// input.take_press is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function careful_hands_input_take_press(_ctx) {
    // _ctx is { subject, input_id, local_key, interaction }.
    //   .subject     - the par_interactable instance whose interaction fired.
    //   .input_id    - the input id the interaction is registered on.
    //   .local_key   - the interaction's registered local_key.
    //   .interaction - the registered interaction struct itself.
    // if (<your condition>) {
    //     return false; // veto - the press is dropped (a force_press still goes through)
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("input.take_press", careful_hands_input_take_press);
```

## Engine Wiring

- Seam [`input_take_press`](../seams/input_take_press.md) dispatches from `gml/objects/system/parents/par_interactable.gml`, between the `take_press`/`pressed` input read and the `if pressed || force_press` gate. On veto it sets `pressed = false`, so only a `force_press` still gets through.

## See Also

- [input.check_value_id](input.check_value_id.md) - Swap the input id `Input.check_value()` looks up.
- [interact.elevator_action](interact.elevator_action.md) - Block activating the dungeon elevator.
- [interact.ladder_down_action](interact.ladder_down_action.md) - Block descending a dungeon ladder.
- [object.interact](object.interact.md) - Replace a grid object's interaction entirely.
