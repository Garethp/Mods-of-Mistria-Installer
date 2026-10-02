# Hook: player.jump_attack_landing

Change where the jump attack may land.

`player.jump_attack_landing` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside the Jump Attack perk's landing test, once per corner of the player's bounding box, on each frame after a jump's peak, after the player has pressed the attack input during the jump. The filtered value is whether that corner's grid node lets the jump attack land. ctx is `{ x, y, node, collideable, water }`. Return a replacement boolean, or `undefined` to keep the current value.

The attack lands only when every corner's final value is `true`.

| | |
| --- | --- |
| **Fires** | Inside the landing test of `PlayerState.Jump`, once per corner of the player's bounding box, on each frame after the jump's peak once the player has pressed the attack input during the jump. |
| **Value** | Whether the corner's grid node lets the jump attack land. |
| **ctx** | `{ x, y, node, collideable, water }` |
| **Kind contract** | The callback receives the current boolean and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `x`, `y` - the corner's room position in pixels.
- `node` - the grid node index under the corner.
- `collideable` - the node's collision flag. The engine refuses a collideable node.
- `water` - `true` when the node's terrain is water. The engine refuses a water node.

### The value

- The value starts `true` for a node that is neither collideable nor water, and `false` otherwise.
- The engine combines the four corners with `&&`, so the first `false` refuses the landing on that frame and ends that frame's corner tests.
- A final value that is not a boolean is coerced in that `&&` chain, so a string or a number other than zero counts as `true`.

### When It Fires

The test runs while the player has the Jump Attack perk set, holds a weapon, and has pressed the attack input during the jump. It runs on every frame from the jump's peak until the attack lands or the jump ends, so this is not a hot path.

| Case | What Happens |
| --- | --- |
| A monster or NPC is under the player. | The frame is refused before any corner is tested, and the hook does not fire on that frame. |
| A corner lies outside the grid. | That corner fails before dispatch, and the engine tests no further corners on that frame. |
| A corner's final value is `false`. | The engine tests no further corners on that frame, so a refused landing dispatches about once per frame for the rest of the jump. |
| Every corner's final value is `true`. | The attack enters on that frame, where the player is along the jump's arc, not at the jump's destination. With a handler that allows water, a jump from the shore lands in the first open water past the shoreline. |
| The attack ends on a water node. | The player exits into `PlayerState.Swim` instead of `PlayerState.Default`. |

## Usage

```gml
// player.jump_attack_landing is a FILTER: you receive (value, ctx) and return
// a replacement, or undefined to keep the game's value.
function splashdown_player_jump_attack_landing(_value, _ctx) {
    // _value is whether this corner's node lets the jump attack land.
    // _ctx is { x, y, node, collideable, water }.
    // Let the attack land on open water. A collideable node stays refused.
    if (_ctx.water && !_ctx.collideable) return true;
    return undefined;
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("player.jump_attack_landing", splashdown_player_jump_attack_landing);
```

With this handler the attack lands on open water, and the attack's exit enters `PlayerState.Swim` because the node under the player's position is water.

## Interactions

- An attack that ends on a water node exits into `PlayerState.Swim`.
- [fsm.transition](fsm.transition.md) fires for the attack's exit with `from` set to `PlayerState.DownSmash` and `to` set to `PlayerState.Swim` or `PlayerState.Default`, so a handler can tell where the attack landed.
- The attack's ground pound damages monsters only. Fish and fish schools carry no damage receiver, so a jump attack landing on water hits nothing in the water.
- A collideable node a handler allows is the handler's own responsibility. The engine does not test collision during or after the attack, and the player stands at the landing position when it ends.

## Engine Wiring

- Seam [`player_jump_attack_landing`](../seams/player_jump_attack_landing.md) dispatches from `gml/scripts/Player/AriFsm.gml`, filtering each corner's result inside the Jump state's landing test.
- Seam [`player_jump_attack_water_exit`](../seams/player_jump_attack_water_exit.md) edits the same file without dispatching, ending an attack that landed on a water node in `PlayerState.Swim` instead of `PlayerState.Default`.

## See Also

- [fsm.transition](fsm.transition.md) - Redirect or cancel any state transition in the game's shared FSMs.
