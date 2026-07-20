# Hook: player.incoming_damage

Change the final damage a hit deals the player.

`player.incoming_damage` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the player's damage drain in `obj_ari`, after mitigation is applied. The filtered value is the final signed damage (the engine computes it as `min(-1, defense - tarball.damage)`, so it arrives at most `-1`), and health only changes while it is negative: the seam calls `ARI.modify_health` only for a negative result. ctx is `{ player, receiver, tarball }`. Return the replacement damage, or `undefined` to keep the current value.

A [combat.damage](combat.damage.md) filter can set the ctx.tarball fields `__mmapi_player_show_damage_popup` and `__mmapi_player_should_flinch` to `false` to suppress the damage popup and flinch. The seam reads both defensively, so a tarball without them counts as `true` for each. With both `false` and non-negative damage the hit is skipped entirely (the drain `continue`s: no popup, no flinch, no health change). `mmapi_deal_damage()`'s `show_popup` and `flinch` opts ride the same two fields.

| | |
| --- | --- |
| **Fires** | In the player's damage drain in `obj_ari`, after mitigation is applied and before the health change. |
| **Value** | The final signed damage. Health only changes while it is negative. |
| **ctx** | `{ player, receiver, tarball }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `ARI` struct about to take the hit.
- `receiver` - the player's `obj_damage_receiver` draining the hit.
- `tarball` - the damage tarball instance for this hit. Carries `__mmapi_player_show_damage_popup` / `__mmapi_player_should_flinch` when a `combat.damage` filter or `mmapi_deal_damage()` set them, and `__mmapi_injected` (the injecting mod's name) on synthetic hits.

> [!NOTE]
> The flinch rides `took_damage`: the seam sets it only while `__mmapi_player_should_flinch` holds, so a `false` there keeps the player steady even when the damage lands.

## Usage

```gml
// player.incoming_damage is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function stone_skin_player_incoming_damage(_value, _ctx) {
    // _value is the final signed damage after mitigation. The engine
    // computes min(-1, defense - tarball.damage), so it arrives <= -1.
    // Health only changes while it stays negative.
    // _ctx is { player, receiver, tarball }.
    //   .player   - the ARI struct about to take the hit.
    //   .receiver - the player's obj_damage_receiver draining the hit.
    //   .tarball  - the damage tarball instance. __mmapi_player_show_damage_popup /
    //               __mmapi_player_should_flinch ride here (set by combat.damage
    //               filters or mmapi_deal_damage); __mmapi_injected marks synthetic hits.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value < 0) {
        return min(-1, ceil(_value / 2)); // halve the hit, keep it a hit
    }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("player.incoming_damage", stone_skin_player_incoming_damage);
```

## Engine Wiring

- Seam [`player_incoming_damage`](../seams/player_incoming_damage.md) dispatches from `gml/objects/characters/obj_ari.gml`, rewriting the drain's mitigation block: it filters `final_dmg`, reads the two presentation fields off the tarball, `continue`s past the hit when both are `false` and the damage is non-negative, sets `took_damage` only when flinching, and calls `ARI.modify_health` only for negative damage.

## See Also

- [combat.damage](combat.damage.md) - Filter the tarball earlier, where the popup/flinch fields are set.
- [combat.damage_resolved](combat.damage_resolved.md) - Observe the hit after it resolves.
- [combat.damage_injected](combat.damage_injected.md) - Observe synthetic hits from `mmapi_deal_damage()`.
- [player.health_delta](player.health_delta.md) - This is the last filter the damage passes through, inside `modify_health`.
