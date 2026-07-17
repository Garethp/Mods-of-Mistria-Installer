# Hook: item.display_description

Reword the description an item's tooltip renders.

`item.display_description` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `LiveItem.get_display_description()`, the string the tooltip body actually renders. The filtered value is the description string. ctx is `{ item }`. Return the replacement string, or `undefined` to keep the current value.

`TooltipMenu` (and `CraftingMenu`) build the tooltip body from `get_display_description()`, so this is the faithful chokepoint for that string. A replacement here lands on exactly what the player reads. The UI item nodes only carry the icon, whose `.description` field is never drawn.

| | |
| --- | --- |
| **Fires** | At the return of `LiveItem.get_display_description()`. |
| **Value** | The description string the tooltip body renders. |
| **ctx** | `{ item }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `item` - the `LiveItem` whose description is being resolved (`self` inside `get_display_description()`).

## Usage

```gml
// item.display_description is a FILTER: you receive (value, ctx) and return
// a replacement, or undefined to keep the game's value.
function lore_keeper_item_display_description(_value, _ctx) {
    // _value is the description string the tooltip body renders.
    // _ctx is { item }.
    //   .item - the LiveItem whose description is being resolved.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // your change here, e.g. append a line for items you own:
    // if (<_ctx.item is yours>) return _value + "\nAn heirloom of the old kingdom.";
    return undefined; // undefined = keep the game's value
}

mmapi_filter("item.display_description", lore_keeper_item_display_description);
```

## Engine Wiring

- Seam [`item_display_description`](../seams/item_display_description.md) dispatches from `gml/scripts/GameplaySystems/Items/LiveItem.gml`, a whole-function wrap of `get_display_description()` that filters its return value.

## See Also

- [ui.item_node](ui.item_node.md) - Mutate a UI item node as it is populated (its icon-node `.description` is never drawn).
- [ui.item_icon](ui.item_icon.md) - Swap an item's icon sprite wherever it is resolved.
- [local.get](local.get.md) - Filter every engine localization lookup.
