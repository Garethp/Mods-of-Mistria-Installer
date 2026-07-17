# Hook: object.interact

Take over any grid object's interaction.

`object.interact` is an **override** hook. Register a callback with `mmapi_override`. See [Hooks](../HOOKS.md) for how registration and dispatch work. This override is **claim-scoped**: many mods may register, but return `undefined` for targets you do not own. Any non-`undefined` return claims the whole interaction.

## Contract

Fires at the top of `interact(node)` (`Interact.gml`, the shared grid-object interaction dispatcher), before `can_interact`. ctx is the grid node being interacted with (read `node.object_id`, `node.prototype`, `node.top_left_x/y`, `node.renderer`). Return a non-`undefined` value to replace the interaction entirely: the engine's interact for this node is skipped and your value becomes `interact`'s return (return `true` for a handled interaction). Return `undefined` to defer to the engine's normal handling.

This gives a mod's placed furniture custom interact behavior, since the engine dispatches every furniture interaction through this one function.

| | |
| --- | --- |
| **Fires** | At the top of `interact(node)`, the shared grid-object interaction dispatcher, before `can_interact`. |
| **ctx** | The grid node being interacted with. |
| **Kind contract** | The first callback to return a non-`undefined` value replaces the engine's behavior. Return `undefined` to defer. |

### The ctx parameter

ctx is the grid node under interaction. Read:

- `node.object_id` - Which object this node is. Exact-match the ids your mod owns and defer on everything else.
- `node.prototype` - the node's prototype.
- `node.top_left_x` / `node.top_left_y` - the node's grid position.
- `node.renderer` - the node's renderer instance.

## Usage

```gml
// object.interact is an OVERRIDE: return a value to replace the game's whole
// answer; return undefined to let the game (or another mod) decide.
function living_furniture_object_interact(_ctx) {
    // _ctx is the grid node being interacted with.
    //   .object_id  - which object this is; exact-match the ids you own.
    //   .prototype  - the node's prototype.
    //   .top_left_x / .top_left_y - the node's grid position.
    //   .renderer   - the node's renderer instance.
    if (_ctx.object_id != "living_furniture/dancing_chair") return undefined; // defer: not ours
    // ... your interaction here ...
    return true; // handled - the engine's interact for this node never runs
}

mmapi_override("object.interact", living_furniture_object_interact);
```

## Engine Wiring

- Seam [`object_interact`](../seams/object_interact.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Interact.gml`, at the head of `interact(node)`, before `can_interact`.

## See Also

- [furniture.place_guard](furniture.place_guard.md) - Veto placing the furniture in the first place.
- [input.take_press](input.take_press.md) - Veto any registered interaction press generically.
- [object.node_sprite](object.node_sprite.md) - Swap the sprite of any world node.
