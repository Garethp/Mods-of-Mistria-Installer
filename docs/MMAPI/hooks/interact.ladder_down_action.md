# Hook: interact.ladder_down_action

Stop a dungeon ladder descent before it starts.

`interact.ladder_down_action` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when the player activates a dungeon ladder to descend, before the descend sound and floor change. ctx is `{ subject, key: "dungeon_ladder_down" }`. Return `false` to veto the descent. `undefined` or `true` allows.

Like [interact.elevator_action](interact.elevator_action.md), this is an action guard, not a per-frame interactability poll: the check sits inside the ladder's interaction action, after the engine's facing and selection have routed the press to this ladder, so it fires only on a real descend press. The guard runs above the `TANGO.play("SoundEffects/Entrances/LadderDescend")` call. A veto returns before the sound, so nothing about the descent happens.

| | |
| --- | --- |
| **Fires** | Inside the dungeon ladder's interaction action, before the descend sound and floor change. |
| **ctx** | `{ subject, key: "dungeon_ladder_down" }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `subject` - the ladder instance (`par_ladder`) the player pressed.
- `key` - always `"dungeon_ladder_down"`. Use it to share one handler across the interact-action guards.

## Usage

```gml
// interact.ladder_down_action is a GUARD: return false to block it, undefined
// (or true) to allow. Guards fail OPEN - if your handler crashes, the action happens.
function safe_depths_interact_ladder_down_action(_ctx) {
    // _ctx is { subject, key }.
    //   .subject - the par_ladder instance the player pressed.
    //   .key     - always "dungeon_ladder_down".
    // Fires only on a real descend press - never on the per-frame
    // interactability poll. A veto also silences the descend sound.
    // if (<your condition>) {
    //     return false; // veto - no sound, no floor change
    // }
    return undefined; // allow the descent
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("interact.ladder_down_action", safe_depths_interact_ladder_down_action);
```

## Engine Wiring

- Seam [`interact_ladder_down_action`](../seams/interact_ladder_down_action.md) dispatches from `gml/objects/dungeon/par_ladder.gml`, at the top of the ladder's interaction action, above the descend sound. On veto the engine runs `return;`.

## See Also

- [interact.elevator_action](interact.elevator_action.md) - This hook has the same action-guard shape for the dungeon elevator.
- [dungeon.ladder_spawn](dungeon.ladder_spawn.md) - Veto the ladder from spawning at all.
- [input.take_press](input.take_press.md) - Veto any registered interaction press generically.
