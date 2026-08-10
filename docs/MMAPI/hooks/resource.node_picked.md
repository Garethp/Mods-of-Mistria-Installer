# Hook: resource.node_picked

Know the moment a pick lands on a rock or dig site.

`resource.node_picked` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `pick_node()` when a pick lands. For a rock it fires right after the damage is applied and the outcome is decided, before any drops, perk procs, or teardown run, so it covers both the damaging chip and the destroying break from one site. For a dig site it fires on a successful dig, after `dig_site_attempt_dig()` has already run. Furniture and rug picks, inadequate-quality bounces, and empty swings do not fire. ctx is `{ grid, x, y, item, node, result, destroyed, burn, effect_override }`.

Fires for every caller of `pick_node()`: the Tool FSM's pickaxe closure, damage tarballs whose `can_pick_grid_objects` flag is set, and the engine's internal Earthbreaker chain picks. Check `ctx.effect_override` and `ctx.burn` to skip the picks no player swing caused.

| | |
| --- | --- |
| **Fires** | Inside `pick_node()`, the moment a pick lands: on a rock right after its damage is applied (the chip and the break alike), and on a dig site when the dig succeeds. |
| **ctx** | `{ grid, x, y, item, node, result, destroyed, burn, effect_override }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `grid` - the grid the node lives in.
- `x`, `y` - the swung cell (the `x_pos`/`y_pos` arguments). `pick_node()` scans a 2x2 box from there, so the node itself may sit one cell off.
- `item` - the acting item prototype. For a Tool FSM swing this is the held pickaxe; for a damage tarball it is the tarball's fixed pick prototype, so read the player's held item yourself if the reward should scale with it.
- `node` - the node struct. For `result = "rock"` the fields stay readable on the break. For `result = "dig_site"` it is read after the dig, so it may already reflect the dug or absent state.
- `result` - `"rock"` or `"dig_site"`, naming which arm fired.
- `destroyed` - for a rock, `true` when this hit broke it and `false` on a chip. For a dig site, whether the dig consumed the site.
- `burn` - the `is_burn` argument.
- `effect_override` - non-`undefined` exactly for the engine's internal chained picks (the Earthbreaker perk). Skip those to react only to picks a swing caused.

## Usage

```gml
// resource.node_picked is an EVENT: the return value is ignored.
function ore_tally_resource_node_picked(_ctx) {
    // _ctx is { grid, x, y, item, node, result, destroyed, burn, effect_override }.
    //   .result          - "rock" or "dig_site".
    //   .destroyed       - rock: break vs chip; dig site: site consumed.
    //   .effect_override - non-undefined for Earthbreaker chain picks; usually skip.
    if (_ctx.burn || _ctx.effect_override != undefined) { return; }
    // your code here - e.g. tally landed picks, reward non-FSM swings
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("resource.node_picked", ore_tally_resource_node_picked);
```

## Engine Wiring

- Seam [`pick_node_picked_rock`](../seams/pick_node_picked_rock.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Pick.gml`, right after a landed pick's rock damage is applied.
- Seam [`pick_node_picked_dig_site`](../seams/pick_node_picked_dig_site.md) dispatches from the same file, inside the dig-site success branch.

## See Also

- [resource.node_chopped](resource.node_chopped.md) - Know the moment a chop lands on a tree or stump.
- [resource.node_modifier](resource.node_modifier.md) - Change the charged-tool modifier on picks and chops.
- [combat.tarball_grid](combat.tarball_grid.md) - Let a swing pick/chop/destroy grid nodes in the first place.
- [items.dig_artifact](items.dig_artifact.md) - Swap the artifact a dig spot yields.
