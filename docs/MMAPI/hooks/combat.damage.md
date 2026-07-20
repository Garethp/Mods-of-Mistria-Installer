# Hook: combat.damage

Change any hit before it resolves.

`combat.damage` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the `obj_damage_receiver` drain loop, after the iframe and rockstack checks and before the hit resolves. The filtered value is the damage tarball instance. ctx is the receiver. Return the replacement tarball, or `undefined` to keep the current one.

The seam skips the hit if the tarball comes back destroyed. Calling `instance_destroy(_value)` from a handler is the sanctioned way to drop a hit here. Fields set on the tarball in this filter are read later by the `player.incoming_damage` seam: setting `__mmapi_player_show_damage_popup` and `__mmapi_player_should_flinch` to `false` suppresses the player's damage popup and flinch, and with both `false` and non-negative final damage the hit is skipped entirely (see [player.incoming_damage](player.incoming_damage.md)).

Hits injected via `mmapi_deal_damage()` carry `__mmapi_injected` with the injecting mod's name. Engine hits do not. Exact-match it to tell synthetic hits from engine hits.

| | |
| --- | --- |
| **Fires** | In the `obj_damage_receiver` drain loop, after the iframe and rockstack checks and before the hit resolves. |
| **Value** | The damage tarball instance (`obj_damage_tarball`). |
| **ctx** | The `obj_damage_receiver` instance draining the hit. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- The `obj_damage_receiver` the hit is resolving against. The player and every monster drain damage through this one loop, so guard on the tarball's `target` (`CombatTarget.Player` is an incoming hit on the player) before acting.

## Usage

```gml
// combat.damage is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function stoic_farmer_combat_damage(_value, _ctx) {
    // _value is the damage tarball instance (obj_damage_tarball).
    //   .target           - CombatTarget mask; CombatTarget.Player is an
    //                       incoming hit on the player.
    //   .__mmapi_injected - the injecting mod's name on synthetic hits from
    //                       mmapi_deal_damage(); absent on engine hits.
    // _ctx is the obj_damage_receiver draining the hit.
    if (!instance_exists(_value)) return undefined; // an earlier mod dropped the hit
    if (_value.target == CombatTarget.Player) {
        // fields set here are read later by the player.incoming_damage seam:
        _value.__mmapi_player_show_damage_popup = false; // no damage popup
        _value.__mmapi_player_should_flinch = false;     // no flinch
    }
    // instance_destroy(_value); // destroying the tarball skips the hit
    return undefined; // keep the (mutated) tarball
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("combat.damage", stoic_farmer_combat_damage);
```

## Engine Wiring

- Seam [`combat_damage_pre`](../seams/combat_damage_pre.md) dispatches from `gml/objects/Combat/obj_damage_receiver.gml`, in the damage drain loop between the engine's iframe/rockstack rejections and the resolution switch. After the filter chain it re-checks `instance_exists(tarball)` and `continue`s past a destroyed or `undefined` tarball, skipping the hit.

## See Also

- [combat.damage_resolved](combat.damage_resolved.md) - This event fires the moment the same hit lands or is blocked.
- [combat.damage_injected](combat.damage_injected.md) - Know when a mod injects a synthetic hit.
- [player.incoming_damage](player.incoming_damage.md) - This filter produces the player's final damage number, and it reads fields set here.
- [combat.tarball_grid](combat.tarball_grid.md) - This hook exposes the same tarball's grid pick/chop/destroy flags.
