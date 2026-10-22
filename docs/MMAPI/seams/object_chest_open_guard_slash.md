# Seam: object_chest_open_guard_slash

Puts a veto check on the slash path's chest open, just before the chest opens.

`object_chest_open_guard_slash` is a **template seam** (`op = "guard"`). It feeds [object.chest_open_guard](../hooks/object.chest_open_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Slash.gml` |
| **Locator** | pristine context inside the chest branch of `slash_node()`, between the vanilla gate and `breakable_open_chest(node)` |
| **Op** | `guard` |
| **Feeds** | [`object.chest_open_guard`](../hooks/object.chest_open_guard.md) |
| **ctx built** | `{ node: node, x: node.renderer.x, y: node.renderer.y, source: "slash" }` |
| **On veto** | `return true;` |
| **Marker** | `mmapi_object_run_chest_open_guards_slash` |

## The Edit

The locator is pristine context inside the chest branch of `slash_node()`. The vanilla gate has already passed, which means the node carries a chest prototype, `is_locked` and `is_open` are both false, and the swing came from `obj_ari` or the player's `obj_fire_breath`. The check is generated right before `breakable_open_chest(node)` starts the opening animation. On a veto the function returns `true`, which is where the pristine code lands after the branch, because `return true;` follows it unconditionally. A vetoed slash still reads as a consumed hit and opens nothing.

Because the vanilla gate runs first, this site never fires for a locked chest, a chest that is already open, or a monster's swing. With zero handlers the guard check returns true and the seam is behaviorally identical to pristine. The dispatch sits inside its own try/catch, so a failure in ctx construction or dispatch counts as allow.

## See Also

- [object.chest_open_guard](../hooks/object.chest_open_guard.md) - This is the hook this seam dispatches.
- [object_chest_open_guard_interact](object_chest_open_guard_interact.md) - The same guard's other site, the interact path.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - Emits the moment a dungeon treasure chest starts its drop chain.
