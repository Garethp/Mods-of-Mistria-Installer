# Hook: combat.damage_resolved

Know the moment a hit lands or is blocked.

`combat.damage_resolved` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires after a hit resolves against a damage receiver: successful hits (`ReceiverStatus.Normal`, `ReceiverStatus.DamageOnAttack`, and in-air `ReceiverStatus.Aerial`) and blocks. ctx is `{ receiver, tarball, status, successful }`. `successful` is `true` for a landed hit, `false` for a block.

Hits that resolve to nothing never fire: an `UntimedInvulnerable` receiver and a grounded hit on an `Aerial` receiver drop the hit without resolution. Hits injected via `mmapi_deal_damage()` fire here exactly like engine hits, with `__mmapi_injected` still on the tarball.

| | |
| --- | --- |
| **Fires** | In `obj_damage_receiver`'s resolution switch, immediately after `succesful_hit()` for a landed hit or `blocked()` for a block. |
| **ctx** | `{ receiver, tarball, status, successful }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `receiver` - the `obj_damage_receiver` the hit resolved against.
- `tarball` - the `obj_damage_tarball` that just resolved (injected hits still carry `__mmapi_injected` with the injecting mod's name).
- `status` - the receiver's `ReceiverStatus` at resolution: `Normal` or `DamageOnAttack` for an ordinary landed hit, `Blocking` for a block, `Aerial` for an in-air hit.
- `successful` - `true` for a landed hit, `false` for a block.

## Usage

```gml
// combat.damage_resolved is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function combo_counter_combat_damage_resolved(_ctx) {
    // _ctx is { receiver, tarball, status, successful }.
    //   .receiver   - the obj_damage_receiver the hit resolved against.
    //   .tarball    - the resolved obj_damage_tarball.
    //   .status     - the ReceiverStatus: Normal / DamageOnAttack / Blocking /
    //                 Aerial (in-air hit).
    //   .successful - true for a landed hit, false for a block.
    if (_ctx.successful) {
        // a landed hit: extend the combo
    } else {
        // a block (ReceiverStatus.Blocking): reset it
    }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("combat.damage_resolved", combo_counter_combat_damage_resolved);
```

## Engine Wiring

- Seam [`combat_damage_resolved`](../seams/combat_damage_resolved.md) dispatches from `gml/objects/Combat/obj_damage_receiver.gml`, with one emit per resolving branch of the receiver-status switch (`Normal`/`DamageOnAttack`, `Blocking`, and in-air `Aerial`).

## See Also

- [combat.damage](combat.damage.md) - Filter the same hit before it resolves.
- [combat.damage_injected](combat.damage_injected.md) - Know when a mod injects a synthetic hit.
- [monster.death](monster.death.md) - This hook fires the moment a monster dies.
- [player.incoming_damage](player.incoming_damage.md) - This filter produces the player's final damage number after mitigation.
