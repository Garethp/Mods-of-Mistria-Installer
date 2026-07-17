# Hook: combat.tarball_grid

Change what a swing can pick, chop, or destroy.

`combat.tarball_grid` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `obj_damage_tarball`'s step, inside the `if self.can_hurt` block and before the grid pick/chop/destroy blocks (pick at `obj_damage_tarball.gml:168`, chop at `:209`, destroy at `:231`) and the receiver-collision enqueue (`:253`). The filtered value is the tarball instance. A handler mutates it in place, setting `can_pick_grid_objects` / `can_chop_grid_objects` / `can_destroy_grid_objects` so the swing forages / chops / destroys grid nodes that same frame. These flags are what the pick/chop/destroy blocks read.

Guard on the tarball's `target` (`CombatTarget.Player` is an incoming hit on the player) and its provenance (e.g. `parent_id`, the attack's source instance) before acting. Tarballs injected via `mmapi_deal_damage()` never reach this hook: they are built with `can_hurt = false`, so they skip the whole block.

| | |
| --- | --- |
| **Fires** | At the top of `obj_damage_tarball`'s step, inside the `can_hurt` block, before the grid pick/chop/destroy blocks and the receiver-collision enqueue. |
| **Value** | The `obj_damage_tarball` instance, mutated in place. |
| **ctx** | `undefined`. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value

- `can_pick_grid_objects` - set `true` and the swing forages grid nodes this frame (the pick block reads it).
- `can_chop_grid_objects` - set `true` and the swing chops grid nodes this frame (the chop block reads it).
- `can_destroy_grid_objects` - set `true` and the swing destroys grid nodes this frame (the destroy block reads it).
- `target` - the `CombatTarget` mask. `CombatTarget.Player` is an incoming hit on the player, not one of the player's swings. Guard on it before acting.
- `parent_id` - the attack's source instance (its provenance). Check it before granting a swing new powers.

> [!IMPORTANT]
> This is an in-place filter: the return value is discarded. Mutate the value you are given. Never build a replacement.

> [!IMPORTANT]
> Hot path. This filter runs for every active `can_hurt` tarball, every unpaused frame. Make the callback's first check its cheapest early-exit.

## Usage

```gml
// combat.tarball_grid is an IN-PLACE filter: the return value is DISCARDED.
// Change the struct/instance you are given; never build a replacement.
function harvest_sweep_combat_tarball_grid(_value, _ctx) {
    // _value is the obj_damage_tarball instance for this swing.
    //   .target                   - CombatTarget mask; CombatTarget.Player is
    //                               an incoming hit on the player - skip it.
    //   .parent_id                - the attack's source instance (provenance).
    //   .can_pick_grid_objects    - set true: the swing forages grid nodes.
    //   .can_chop_grid_objects    - set true: the swing chops grid nodes.
    //   .can_destroy_grid_objects - set true: the swing destroys grid nodes.
    // _ctx is undefined.
    // HOT PATH: every active can_hurt tarball, every unpaused frame.
    if (_value.target == CombatTarget.Player) return; // incoming hit, not ours
    _value.can_pick_grid_objects = true; // weapon swings now forage
    // no return statement - the game keeps reading the same instance
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("combat.tarball_grid", harvest_sweep_combat_tarball_grid);
```

## Engine Wiring

- Seam [`combat_tarball_grid`](../seams/combat_tarball_grid.md) dispatches from `gml/objects/Combat/obj_damage_tarball.gml` as a `filter_call` (dispatch only, return discarded, the `ui.item_node` shape), at the head of the `if self.can_hurt` block in the tarball's step.

## See Also

- [combat.damage](combat.damage.md) - Filter the same tarball when it resolves against a receiver.
- [resource.node_modifier](resource.node_modifier.md) - Adjust the tool modifier once a pick or chop actually hits a node.
- [combat.damage_injected](combat.damage_injected.md) - Injected tarballs never enter the grid blocks (`can_hurt = false`).
