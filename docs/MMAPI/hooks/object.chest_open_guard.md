# Hook: object.chest_open_guard

Stop a treasure chest from opening when the player interacts with or slashes it.

`object.chest_open_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires just before a treasure chest opens from either of the two paths the player can trigger, the interact press and the sword or Fire Breath slash. It covers every node whose prototype carries a chest. ctx is `{ node, x, y, source }`. Return `false` to veto the open. Each site then exits exactly as its vanilla no-op path would, so the chest does not open, no animation starts, and no drop chain is created. Every other return allows.

Both sites fire only for unlocked, closed chests. The slash branch checks the lock in its own gate, and `interact()` returns at its head when `can_interact()` fails, which it does for a locked chest, so a locked chest never reaches the interact site. The veto only stops the open. Despawning the node or substituting another effect is the handler's own work. The one other caller of `breakable_open_chest()`, the Mist `chest_unlocked` command that opens the dungeon ladder's linked chest, is not routed through this guard. On the interact path, `interact()` has already flagged `ARI.used_object_today` for the chest before the guard runs, as it does for a vanilla interact on an open chest.

| | |
| --- | --- |
| **Fires** | Just before `breakable_open_chest(node)` at the interact and slash call sites. |
| **ctx** | `{ node, x, y, source }` |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `node` - the chest's grid node, which carries `node.prototype.chest` (its chest data), `node.is_open`, `node.is_locked`, `node.object_id`, and `node.renderer`.
- `x`, `y` - the chest's world position, read from `node.renderer`, which is where the engine spawns its drops.
- `source` - `"interact"` (a press at the chest) or `"slash"` (a sword swing or the player's Fire Breath).

## Usage

```gml
// object.chest_open_guard is a GUARD: return Boolean false to block the open;
// every other return allows. Guards fail OPEN - if your handler crashes, the chest opens.
function deliberate_interaction_object_chest_open_guard(_ctx) {
    // _ctx is { node, x, y, source }.
    //   .node   - the chest's grid node (.is_locked, .is_open, .object_id, ...).
    //   .x, .y  - the chest's world position (node.renderer.x/y).
    //   .source - "interact" or "slash".
    // Chests open only from a deliberate interact press. A sword swing or a
    // Fire Breath puff mid-fight must not open the chest.
    if (_ctx.source == "slash") {
        return false; // veto, the swing still lands and the chest stays closed
    }
    return undefined; // allow, the interact path proceeds as vanilla
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("object.chest_open_guard", deliberate_interaction_object_chest_open_guard);
```

## Interactions

- [object.interact](object.interact.md) runs first on the interact path. Its override sits at the head of `interact()`, so an override that handles the chest node returns before this guard is consulted.
- For a dungeon chest, a veto stops `breakable_open_chest()`, so [dungeon.treasure_chest](dungeon.treasure_chest.md) does not fire and [items.treasure_distribution](items.treasure_distribution.md) is never consulted for that chest. A chest outside the dungeon opens through its own callback, which neither of those hooks covers.
- The slash site returns `true` on a veto, the same value it returns after a vanilla open, so the swing still counts as having hit the chest and the node is not broken.

## Engine Wiring

- Seam [`object_chest_open_guard_interact`](../seams/object_chest_open_guard_interact.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Interact.gml`, inside the breakable branch of `interact()`. On veto the function returns `undefined`, which is where the pristine branch falls through to.
- Seam [`object_chest_open_guard_slash`](../seams/object_chest_open_guard_slash.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Slash.gml`, inside the chest branch of `slash_node()`. On veto the function returns `true`, as the pristine branch does.

## See Also

- [dungeon.treasure_chest](dungeon.treasure_chest.md) - Know the moment a treasure chest starts its drop chain.
- [items.treasure_distribution](items.treasure_distribution.md) - Change what the dungeon treasure roll drops.
- [object.interact](object.interact.md) - Take over any grid object's interaction.
- [dungeon.floor_built](dungeon.floor_built.md) - Know the moment a dungeon floor's room is fully built.
