# Hook: combat.damage_injected

Know when a mod injects a hit through the damage pipeline.

`combat.damage_injected` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from `mmapi_deal_damage()` right after a receiver accepts a synthetic hit: it has passed the receiver's target-mask and iframe enqueue checks but has not resolved yet. ctx is `{ receiver, tarball, mod_name }`. Observation only: the injected hit still flows through [combat.damage](combat.damage.md) and [combat.damage_resolved](combat.damage_resolved.md) like any engine hit, and the tarball carries `__mmapi_injected` with the injecting mod's name so `combat.damage` filters can tell synthetic hits from engine hits.

The injectors are `mmapi_deal_damage(target, amount, opts)` and its player convenience `mmapi_deal_damage_player(amount, opts)`. See the [API Reference](../API_REFERENCE.md) for the full opts struct (critical/heavy/instant-kill flags, knockback, status effects, popup and flinch suppression, and more). A rejected injection (target-mask miss, enqueue-time iframes, bad arguments, no receiver) never emits: `mmapi_deal_damage` destroys the tarball and returns `undefined`. On acceptance the same tarball is the function's return value, so an injector can stamp per-attack fields onto the instance your handler sees.

The hit resolves in the owner's next drain. The tarball is built with a hit count of 1, so resolution destroys it. A destruction timer (`opts.gc_frames`, default 30) collects the drop-without-destroy paths, and a hit still queued when the timer fires is skipped by the `combat.damage` drain guard the same way the engine treats any stale hit.

| | |
| --- | --- |
| **Fires** | From `mmapi_deal_damage()`, right after the receiver's own `damage()` gate accepts the synthetic hit (enqueued, not yet resolved). |
| **ctx** | `{ receiver, tarball, mod_name }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `receiver` - the `obj_damage_receiver` that accepted the hit (`obj_ari` and every `par_monster` species expose theirs as `.receiver`).
- `tarball` - the live injected `obj_damage_tarball`: a real engine tarball with its collision neutralised (`can_hurt = false`), carrying `__mmapi_injected`. The same instance `mmapi_deal_damage` returned to the injector.
- `mod_name` - the name of the mod that called `mmapi_deal_damage()`.

## Usage

```gml
// combat.damage_injected is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function damage_ledger_combat_damage_injected(_ctx) {
    // _ctx is { receiver, tarball, mod_name }.
    //   .receiver - the obj_damage_receiver that accepted the hit.
    //   .tarball  - the live injected obj_damage_tarball (still queued; it
    //               resolves in the owner's next drain).
    //   .mod_name - the mod that called mmapi_deal_damage().
    // e.g. audit which mods are injecting hits:
    // show_debug_message(_ctx.mod_name + " injected a hit");
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("combat.damage_injected", damage_ledger_combat_damage_injected);
```

## Engine Wiring

- This event is emitted by the mmapi framework itself. There is no engine seam behind it. `mmapi_deal_damage()` in `mmapi/mmapi_combat.gml` emits it after `receiver.damage(tarball)` accepts the hit, either queued for the next drain or consumed by a `DamageOnAttack` damage-back.

## See Also

- [combat.damage](combat.damage.md) - Filter the injected hit (and every engine hit) before it resolves.
- [combat.damage_resolved](combat.damage_resolved.md) - This event fires the moment the injected hit lands or is blocked.
- [player.incoming_damage](player.incoming_damage.md) - This is where the injector's `show_popup` and `flinch` opts are read for player targets.
