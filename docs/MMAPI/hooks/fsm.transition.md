# Hook: fsm.transition

Redirect or cancel any state transition in the game's shared FSMs.

`fsm.transition` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `__StateMachine.execute_state_change()`, once per executed state transition of every shared-FSM machine in the game (monsters, the player and the fishing sub-machine, NPCs, animals, birds, fish and fish schools, the camera, cameos, the bobber) before the outgoing state's `stop()` runs. The filtered value is the struct `{ fsm, owner, from, to, cancel: false }`. Return the replacement struct, or mutate it in place and return `undefined`. The seam re-reads fields defensively.

Set `cancel` to `true` to skip the transition entirely: no `stop()` or `start()` runs, the machine keeps its state, and `state_frame` stalls for that tick. Set `to` to a different valid state id of the same machine to redirect. Non-numeric, fractional, or out-of-range values are ignored. The seam integer- and bounds-checks the redirect because `List.get()` asserts out of range. Every id in `[0, count)` is a defined state at spawn, so an in-range integer is always safe. Redirect via the struct, never by calling `change_state` from inside a handler.

This is the single funnel every executed transition routes through: the deferred path (`new_tick`) and the instant path (`change_state_instant`) both land in `execute_state_change()`. What does not dispatch: the initial state entered at spawn (it bypasses `execute_state_change` via `begin_new_state`), and deferred requests overwritten before `new_tick`. A cancelled transition that the owner re-requests every step re-dispatches every tick for that instance while the cancel holds. Transitions are edge events, not per-frame. Because dispatch happens before the outgoing state's `stop()`, stop handlers that read `fsm.next_state` see the post-filter destination.

| | |
| --- | --- |
| **Fires** | In `__StateMachine.execute_state_change()`, once per executed transition of every shared-FSM machine, before the outgoing state's `stop()` runs. |
| **Value** | The struct `{ fsm, owner, from, to, cancel }`. |
| **ctx** | `undefined`. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `fsm` - the `__StateMachine` executing the change.
- `owner` - whatever `spawn()` received: an instance for monsters, NPCs, and the player, or a **struct** for the camera and the fishing sub-machine. Check `is_struct` before touching instance fields.
- `from` - the outgoing state's id, in the machine's per-species enum (`CatState.*`, `TomeState.*`, `MushroomState.*`, `PlayerState.*`, and so on, resolvable by name in mod GML).
- `to` - the requested destination state id. Overwrite it with a different valid state id of the **same machine** to redirect. Non-numeric, fractional, or out-of-range values are ignored.
- `cancel` - starts `false`. Set `true` to skip the transition entirely: no `stop()` or `start()` runs, the machine keeps its state, and `state_frame` stalls for that tick.

> [!IMPORTANT]
> To cancel, set `_value.cancel = true` on the value struct and return it. Returning `false` does not cancel. A filter's return replaces the value.

## Usage

```gml
// fsm.transition is a FILTER whose value struct carries a 'cancel' switch:
// set _value.cancel = true to stop it. Never return false - a filter's
// return REPLACES the value, it does not veto.
function puppet_master_fsm_transition(_value, _ctx) {
    // _value is { fsm, owner, from, to, cancel }.
    //   .fsm    - the state machine executing the change.
    //   .owner  - whatever spawn() received: an instance for monsters/NPCs/
    //             the player, a struct for the camera and the fishing
    //             sub-machine - check is_struct(_value.owner) first.
    //   .from   - the outgoing state id (per-species enum, e.g. CatState.*).
    //   .to     - the requested state id; overwrite with another valid id of
    //             the same machine to redirect.
    //   .cancel - set true to skip the transition entirely.
    // _ctx is undefined.
    // Never call change_state from in here - redirect via the struct.
    if (is_struct(_value.owner)) return undefined; // camera/fishing: not ours
    // if (<your condition>) {
    //     _value.cancel = true; // no stop/start; state_frame stalls this tick
    //     return _value;
    // }
    // _value.to = <another state id of this machine>; return _value; // redirect
    return undefined; // keep the transition
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("fsm.transition", puppet_master_fsm_transition);
```

## Engine Wiring

- Seam [`fsm_transition`](../seams/fsm_transition.md) dispatches from `gml/scripts/GameplaySystems/Fsm/StateMachine.gml`, at the head of `execute_state_change()`. On cancel it clears `next_state` and returns before `stop()` runs (`new_tick`'s boolean return is consumed nowhere in pristine, so the early return is safe). On redirect it re-points `next_state` after the integer and bounds checks.

## See Also

- [monster.spawn](monster.spawn.md) - This is the other cancel-switch filter. Use it to move, replace, or cancel a spawn.
- [monster.step_begin](monster.step_begin.md) - This hook provides per-frame monster observation, for when you need every tick rather than transition edges.
