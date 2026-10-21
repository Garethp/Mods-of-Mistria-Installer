# Seam: player_sword_swing_guard

Puts a veto check at the top of the Sword state's next_swing(), before each swing of a combo starts.

`player_sword_swing_guard` is a **text seam** (`anchor` + `replace`). It feeds [player.sword_swing_guard](../hooks/player.sword_swing_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the head of `next_swing()` inside the `PlayerState.Sword` create, through the four lines that advance the combo count and clear the input buffers |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.sword_swing_guard`](../hooks/player.sword_swing_guard.md) |
| **ctx built** | `{ owner: self.owner, sword: self.sword, combo: self.combo_state + 1 }` |
| **On veto** | `self.input_buffer = false; self.can_go_to_next_combo = false; if (self.combo_state < 0 && self.fsm.next_state == undefined) { self.fsm.change_state(PlayerState.Default); } return;` |
| **Marker** | `mmapi_player_run_sword_swing_guards` |

## The Edit

`next_swing()` is a function the Sword state declares in its create. The state's `start()` calls it for the first swing, and its `step()` calls it for the second and third when the attack input is buffered and the swing in progress has finished its action. The function first advances `combo_state` and clears the input buffers, then chooses the swing's sound and animation by the new count, and returns without a swing once the count has passed the third.

The replacement inserts the dispatch as the function's first statement, inside a gate on `combo_state` that admits only the three real swings. It checks `mmapi_check_guards("player.sword_swing_guard", ...)` with the player instance, the `LiveItem` being swung, and the index of the swing about to start. On a veto it returns before the count advances, after three writes. It clears the state's own `input_buffer`, because `step()` calls `next_swing()` on every frame the buffer is set and the next swing is admitted, and a bare return would reach the guard once per frame for one press. It sets `can_go_to_next_combo` to `false`. The create never initializes that flag, and vanilla relies on `next_swing()` writing it before the first step reads it. A `false` there also keeps the step's direction exit from replacing a transition the handler queued in the same frame. For the first swing, where the count is still `-1`, it queues the exit to `PlayerState.Default` when the machine has no other change pending, so the state leaves at the next tick instead of idling until the player's animation completes. The pristine statements follow the gate unchanged.

The gate keeps the dispatch away from the call `step()` makes when input is still buffered after the third swing. That call advances the count past the third swing and returns from the pristine switch without a swing, so there is nothing for a handler to veto there.

With zero handlers the check returns true and the function runs as pristine. A catch at the site counts a failure in context construction or dispatch as allow.

## See Also

- [player.sword_swing_guard](../hooks/player.sword_swing_guard.md) - This is the hook this seam dispatches.
- [items_use_guard](items_use_guard.md) - Puts a veto check in front of every item use, right after the LiveItem coercion.
- [fsm_transition](fsm_transition.md) - Filters every executed shared-FSM state transition through one funnel.
