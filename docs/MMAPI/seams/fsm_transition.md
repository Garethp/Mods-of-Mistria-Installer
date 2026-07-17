# Seam: fsm_transition

Filters every executed shared-FSM state transition through one funnel.

`fsm_transition` is a **text seam** (`anchor` + `replace`). It feeds [fsm.transition](../hooks/fsm.transition.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Fsm/StateMachine.gml` |
| **Locator** | text anchor: the head of `__StateMachine.execute_state_change()`, before `if self.state.has_stop() {` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`fsm.transition`](../hooks/fsm.transition.md) |
| **Value filtered** | `{ fsm: self, owner: self.state.owner, from: self.state.state_id, to: self.next_state.state_id, cancel: false }` |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_fsm_transition` |

## The Edit

`execute_state_change()` is the single funnel every executed shared-FSM transition routes through: the deferred path (`new_tick`) and the instant path (`change_state_instant`) both land here. Only the initial state entered at spawn bypasses it, via `begin_new_state`, which is why spawn-time state entry never dispatches. One edit at its head therefore covers every machine built on the shared FSM: monsters, the player and the fishing sub-machine, NPCs, animals, birds, fish and fish schools, the camera, cameos, the bobber.

The injected block builds `__mmapi_fsm_tx = { fsm: self, owner: self.state.owner, from: self.state.state_id, to: self.next_state.state_id, cancel: false }` and runs it through `mmapi_apply_filters("fsm.transition", ..., undefined)` in a try/catch. Dispatch happens before the outgoing state's `stop()` runs, so stop handlers that read `fsm.next_state` see the post-filter destination. The result is only honored if it is a struct, and its fields are read defensively:

- **Cancel.** `cancel == true` clears `self.next_state = undefined;` and returns early. That means no `stop()`, no `start()`, the machine keeps its state, and `state_frame` stalls for that tick. The early return is safe because `new_tick`'s boolean return is consumed nowhere in pristine code.
- **Redirect.** The `to` field is bounds- and integer-checked before it is applied: it must be a real, `floor(to) == to`, different from the current `next_state.state_id`, and inside `[0, self.states.count())`. The checks exist because `List.get()` asserts out of range, and every id in `[0, count)` is a defined state at spawn, so anything that passes is a valid `self.states.get(to)`. Non-numeric, fractional, or out-of-range values are silently ignored.

`execute_state_change` is a zero-argument function, so the injected try/catch is reliable under the engine dialect. With zero handlers the block builds one struct per transition and falls straight through to the pristine `if self.state.has_stop() {`.

## See Also

- [fsm.transition](../hooks/fsm.transition.md) - This is the hook this seam dispatches.
- [monster_spawn](monster_spawn.md) - This is the other cancel-switch filter seam, with the same struct-and-`cancel` shape.
