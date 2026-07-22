# Hook: fishing.should_reel

Change whether the player reels while waiting for a fish.

`fishing.should_reel` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires every step of the player fishing FSM's `FishingState.Wait`, after bite notification handling and before the retract sound, caught/missed handling, and Reel transitions. The filtered value is whether the normal charged or repeated tool input requested a reel. ctx is `{ bite_active }`, where `bite_active` is true while `FISHING.bite_timer` is positive. Return a replacement boolean, or `undefined` to keep the current value.

The final true value runs the complete vanilla reel block. While `bite_active` is true that block catches the fish, fills the fishing FSM's celebration data, clears the bite timer, and starts the player and bobber Reel states. Without an active bite it takes the normal Missed path. A final false value remains in Wait, even when the player supplied reel input.

| | |
| --- | --- |
| **Fires** | Every step of `FishingState.Wait`, after bite notification handling and before the reel block. |
| **Value** | Whether the normal tool inputs requested a reel this frame. |
| **ctx** | `{ bite_active }` |
| **Kind contract** | The callback receives the current boolean and returns a replacement, or `undefined` to keep the current value. |

This is a hot hook while the player waits for a fish. Put the cheapest test first and avoid logging on every dispatch.

## Usage

```gml
// fishing.should_reel is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function auto_reel_fishing_should_reel(_value, _ctx) {
    // Preserve ordinary input, but request a reel as soon as a bite is active.
    if (_ctx.bite_active == true) return true;
    return undefined;
}

// Inside your latched register function (see Mod Anatomy):
mmapi_filter("fishing.should_reel", auto_reel_fishing_should_reel);
```

Returning false can suppress reeling, including a manual reel request. Return `undefined`, not false, when the handler has no opinion.

## Engine Wiring

- Seam [`fishing_should_reel`](../seams/fishing_should_reel.md) dispatches from `gml/scripts/Player/AriFsm.gml`, replacing the `FishingState.Wait` input gate with a filtered boolean before the original reel block.

## See Also

- [fsm.transition](fsm.transition.md) - Observe, redirect, or cancel the state transitions that happen after the reel decision.
- [input.check_value_id](input.check_value_id.md) - Remap an input id at the engine's input-value lookup rather than changing this fishing decision.
