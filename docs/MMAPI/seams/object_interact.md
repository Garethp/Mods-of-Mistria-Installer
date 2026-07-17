# Seam: object_interact

Puts a claim-scoped override in front of every grid-object interaction.

`object_interact` is a **text seam** (override-shaped: it dispatches `mmapi_run_override`). It feeds [object.interact](../hooks/object.interact.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Interact.gml` |
| **Locator** | text anchor at the head of `interact(node)` |
| **Op** | text (override dispatch) |
| **Feeds** | [`object.interact`](../hooks/object.interact.md) |
| **ctx built** | `node` (the grid node being interacted with) |
| **Marker** | `mmapi_object_interact_override` |

## The Edit

Two injected lines land at the head of `interact(node)`, before `can_interact` runs:

```gml
var __mmapi_object_interact = mmapi_run_override("object.interact", node); // mmapi_object_interact_override
if (__mmapi_object_interact != undefined) { return __mmapi_object_interact; }
```

A non-`undefined` return short-circuits the function: the engine's interact for this node is skipped entirely and the override's value becomes `interact`'s return (return `true` for a handled interaction). When every handler defers (`undefined`), execution falls straight through into the engine's normal handling.

`interact()` is the shared grid-object interaction dispatcher (the engine routes every furniture interaction through this one function), which is what lets a mod's placed furniture carry custom interact behavior. ctx is the grid node itself: read `node.object_id`, `node.prototype`, `node.top_left_x/y`, `node.renderer`. The hook is claim-scoped: many mods register, each returning `undefined` for nodes it does not own.

## See Also

- [object.interact](../hooks/object.interact.md) - This is the hook this seam dispatches.
- [furniture_place_guard](furniture_place_guard.md) - This is the guard on placing furniture. This seam covers interacting with it once placed.
- [input_take_press](input_take_press.md) - This is the guard on interactable input presses upstream.
