# Hook: player.sword_swing_guard

Stop any swing of the player's sword combo before it starts.

`player.sword_swing_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside the player FSM's Sword state at the top of `next_swing()`, before each swing of a combo starts and before its animation, sound, or hitbox exist. It fires for all three swings of a combo, the first from the state's `start()` and the second and third from its `step()` on buffered input. ctx is `{ owner, sword, combo }`, with `owner` the player instance, `sword` the `LiveItem` being swung, and `combo` the index of the swing about to start (`0`, `1`, or `2`). Return `false` to veto the swing. The site then returns from `next_swing()` before the combo count advances, so the swing never starts, and it clears the state's input buffer and the flag that admits the next swing, so the veto ends the combo. Every other return allows.

After a vetoed swing `1` or `2` the swing in progress plays out, the state accepts no further swing and no early exit on a direction press, and it exits to the Default state when that animation completes. After a vetoed swing `0` the site queues the exit to the Default state itself, so the state leaves at the next tick with no swing having started. The state's `stop()` runs at that exit and removes the sword attachment. A handler that wants the player somewhere other than Default calls `change_state` on `owner.fsm` before it returns `false`. The site leaves that change in place, and the engine applies it at the next tick.

The engine calls `next_swing()` once more when input is still buffered after the third swing. That call starts no swing and never reaches the guard. The first swing is also subject to [items.use_guard](items.use_guard.md), which fires at the press, and a veto there keeps the Sword state from starting at all.

| | |
| --- | --- |
| **Fires** | At the top of `next_swing()` in `PlayerState.Sword`, before each swing of a combo starts. |
| **ctx** | `{ owner, sword, combo }` |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `owner` - the player instance. Its `fsm` is the player FSM, so a handler can call `owner.fsm.change_state(...)` before it returns `false` to send the player somewhere other than Default.
- `sword` - the `LiveItem` being swung, which carries `prototype.damage`, `infusion`, and `item_id`.
- `combo` - the index of the swing about to start. `0` is the first swing, `1` the second, and `2` the heavy finisher.

### When It Fires

| Case | What Happens |
| --- | --- |
| The player uses the sword. | [items.use_guard](items.use_guard.md) fires at the press. When it allows, the Sword state starts at the next tick and the guard fires from `start()` with `combo` set to `0`. |
| The player presses the attack input during a swing. | The press is buffered. Once the swing in progress has finished its action, the guard fires from `step()` with `combo` set to `1` or `2`. |
| Input is still buffered after the third swing. | The engine calls `next_swing()` once more. That call starts no swing and never reaches the guard. |

### After A Veto

| Case | What Happens |
| --- | --- |
| `combo` was `1` or `2`. | The swing in progress plays out. The state accepts no further swing and no early exit on a direction press, and it exits to `PlayerState.Default` when that animation completes. |
| `combo` was `0`. | No swing starts. The site queues the exit to `PlayerState.Default`, and the state leaves at the next tick. |
| The handler called `change_state` on `owner.fsm` before returning `false`. | The site leaves that change in place. The engine applies it at the next tick, and the Sword state's `stop()` runs at that exit. |

> [!NOTE]
> The Sword state's `step()` runs once in the frame of a vetoed swing `0`. If the animation the player was playing completes on that exact frame, the step exits to `PlayerState.Default` at once, and that exit replaces a change the handler queued. The window is one frame per animation cycle, and the engine's own queued transitions share it.

## Usage

```gml
// player.sword_swing_guard is a GUARD: return Boolean false to block the swing;
// every other return allows. Guards fail OPEN - if your handler crashes, the swing happens.
function steady_hands_player_sword_swing_guard(_ctx) {
    // _ctx is { owner, sword, combo }.
    //   .owner - the player instance; owner.fsm is the player FSM.
    //   .sword - the LiveItem being swung.
    //   .combo - 0, 1, or 2, the swing about to start.
    // The heavy finisher takes a steady hand. Veto the third swing while the
    // player is nearly out of stamina; the first two swings are never touched.
    if (_ctx.combo == 2 && ARI.get_stamina() < 10) {
        return false; // veto, the combo ends after the second swing
    }
    return undefined; // allow, the swing proceeds as vanilla
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("player.sword_swing_guard", steady_hands_player_sword_swing_guard);
```

## Interactions

- [items.use_guard](items.use_guard.md) fires first, at the press that uses the sword. A veto there keeps the Sword state from starting, so this guard never fires for that press.
- A vetoed swing builds no hitbox, so [object.chest_open_guard](object.chest_open_guard.md) does not fire from the slash path for it, and nothing else the swing would have struck is touched.
- A state change a handler makes on `owner.fsm` is applied at the next tick and passes through [fsm.transition](fsm.transition.md) like any other transition. The exit the site queues after a vetoed swing `0` is an ordinary transition from `PlayerState.Sword` to `PlayerState.Default`, and that filter sees it with those endpoints.

## Engine Wiring

- Seam [`player_sword_swing_guard`](../seams/player_sword_swing_guard.md) dispatches from `gml/scripts/Player/AriFsm.gml`, at the top of `next_swing()` inside the Sword state's create. On veto the function clears the state's input buffer and the flag that admits the next swing, queues the exit to Default for swing `0` when no other change is pending, and returns before the combo count advances.

## See Also

- [items.use_guard](items.use_guard.md) - Block an item from being used.
- [fsm.transition](fsm.transition.md) - Redirect or cancel any state transition in the game's shared FSMs.
- [object.chest_open_guard](object.chest_open_guard.md) - Stop a treasure chest from opening when the player interacts with or slashes it.
