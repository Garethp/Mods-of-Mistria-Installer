# Hook: resource.node_chopped

Know the moment a chop lands on a tree or stump.

`resource.node_chopped` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `chop_node()` when a chop lands on a tree or stump, the damaging hit and the destroying hit alike, right before the function returns `true`. It never fires for inadequate-quality bounces, burn i-frame absorbs, grass, or non-choppable targets, and never for `slash_node` destroys. ctx is `{ grid, x, y, item, node, destroyed, burn }`. The engine grants no essence or XP inside `chop_node()` itself. The per-swing rewards live in the Tool FSM closures, so a handler rewarding non-FSM chops (damage-tarball swings) does not double anything the engine does at this site.

Fires for every caller of `chop_node()`: the Tool FSM's axe closure, damage tarballs whose `can_chop_grid_objects` flag is set, and fire spread. Check `ctx.burn` to skip the chops no player swing caused.

| | |
| --- | --- |
| **Fires** | Inside `chop_node()`, the moment a chop lands on a tree or stump, just before the function returns `true`. Both the damaging hit and the destroying hit fire. |
| **ctx** | `{ grid, x, y, item, node, destroyed, burn }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `grid` - the grid the node lives in.
- `x`, `y` - the chopped cell (the `x_pos`/`y_pos` arguments). For trees this is the trunk-center cell the engine accepted, not necessarily the node's top-left.
- `item` - the acting item prototype. For a Tool FSM swing this is the held axe; for a damage tarball it is the tarball's fixed chop prototype, so read the player's held item yourself if the reward should scale with it.
- `node` - the node struct. On `destroyed = true` it is already erased from the grid, but its fields (`prototype`, `stage`, `top_left_x`/`top_left_y`, `hitpoints`) stay readable.
- `destroyed` - `true` when this hit brought the node to zero hitpoints (a felled tree or a broken stump/branch), `false` on a damaging chip.
- `burn` - the `is_burn` argument: `true` for fire-spread chops, which no player swing caused.

## Usage

```gml
// resource.node_chopped is an EVENT: the return value is ignored.
function wood_tally_resource_node_chopped(_ctx) {
    // _ctx is { grid, x, y, item, node, destroyed, burn }.
    //   .node      - the chopped node (already erased when .destroyed).
    //   .destroyed - true on the felling/breaking hit, false on a chip.
    //   .burn      - true for fire-spread chops; usually skip those.
    if (_ctx.burn) { return; }
    // your code here - e.g. tally landed chops, reward non-FSM swings
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("resource.node_chopped", wood_tally_resource_node_chopped);
```

## Engine Wiring

- Seam [`chop_node_chopped_tree`](../seams/chop_node_chopped_tree.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Chop.gml`, where a landed chop on a tree returns `true`.
- Seam [`chop_node_chopped_stump`](../seams/chop_node_chopped_stump.md) dispatches from the same file, where a landed chop on a stump or branch returns `true`.

## See Also

- [resource.node_picked](resource.node_picked.md) - Know the moment a pick lands on a rock or dig site.
- [resource.node_modifier](resource.node_modifier.md) - Change the charged-tool modifier on picks and chops.
- [combat.tarball_grid](combat.tarball_grid.md) - Let a swing pick/chop/destroy grid nodes in the first place.
