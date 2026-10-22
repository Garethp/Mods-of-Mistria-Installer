# Seam: object_chest_open_guard_interact

Puts a veto check on the chest interact press, just before the chest opens.

`object_chest_open_guard_interact` is a **template seam** (`op = "guard"`). It feeds [object.chest_open_guard](../hooks/object.chest_open_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Interact.gml` |
| **Locator** | pristine context inside the `ObjectCategory.Breakable` branch of `interact()`, between the gate that requires a closed chest and `breakable_open_chest(node)` |
| **Op** | `guard` |
| **Feeds** | [`object.chest_open_guard`](../hooks/object.chest_open_guard.md) |
| **ctx built** | `{ node: node, x: node.renderer.x, y: node.renderer.y, source: "interact" }` |
| **On veto** | `return undefined;` |
| **Marker** | `mmapi_object_run_chest_open_guards_interact` |

## The Edit

The locator is pristine context inside the breakable branch of `interact()`. The vanilla gate, `node.prototype.chest != undefined && node.is_open == false`, has already passed, and the check is generated right before `breakable_open_chest(node)` starts the opening animation. On a veto the function returns `undefined`. That is what the pristine code produces after this branch, because its `break` leaves the switch with no statement after it and the function ends, so a vetoed interact exits exactly like an interact with a chest that is already open.

A locked chest never reaches this branch. `interact()` returns `false` at its head when `can_interact(node)` fails, and the breakable case of `can_interact()` requires `node.is_locked == false`, so the check runs only for unlocked, closed chests. With zero handlers the guard check returns true and the seam is behaviorally identical to pristine. The dispatch sits inside its own try/catch, so a failure in ctx construction or dispatch counts as allow.

## See Also

- [object.chest_open_guard](../hooks/object.chest_open_guard.md) - This is the hook this seam dispatches.
- [object_chest_open_guard_slash](object_chest_open_guard_slash.md) - The same guard's other site, the sword or Fire Breath slash path.
- [object_interact](object_interact.md) - Puts a claim-scoped override in front of every grid-object interaction.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - Emits the moment a dungeon treasure chest starts its drop chain.
