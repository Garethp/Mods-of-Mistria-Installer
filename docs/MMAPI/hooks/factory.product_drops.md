# Hook: factory.product_drops

Change what an apiary or terrarium hands over for its requested item.

`factory.product_drops` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the factory's give interaction, the `InputId.Throw` interaction `create_furniture_renderer()` registers on an apiary or terrarium that is waiting for its requested item. By the time it fires the requested item has left the player's hand and the engine has rolled the tier reward and, for an apiary, the honeycomb bonus. Nothing has dropped yet. The filtered value is the array of products about to drop. ctx is `{ node, production_request, production_tier }`.

Return a replacement array, mutate the array in place, or return `undefined` to keep the current list. A return that is not an array is dropped and the rolled list stands. The bounce animation, reward sound, and request reset run whatever the array holds, so an empty array consumes the request and drops nothing.

| | |
| --- | --- |
| **Fires** | In the give interaction, after the hand pop and both rolls and before the first drop, once per collection. |
| **Value** | `[reward]`, or `[reward, ItemId.Honeycomb]` when the apiary bonus rolled. |
| **ctx** | `{ node, production_request, production_tier }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value array

Each element is an item id or a `LiveItem`, whatever `drop_item()` accepts. Do not nest arrays.

- `[0]` - the tier reward, one random entry of `rewards_map[production_tier]`.
- `[1]` - `ItemId.Honeycomb`, present only when the factory is an apiary and its 15 percent bonus rolled.

### The ctx struct

- `node` - the factory's grid node. `node.object_id` tells an apiary (`ObjectId.Apiary`) from a terrarium (`ObjectId.Terrarium`), `node.prototype.factory.rewards_map` is the array of reward arrays indexed by `FactoryTier`, `node.inventory` holds the bees or bugs, and `node.renderer` is the instance the drops land at.
- `production_request` - the item id the factory asked for and the player just handed over.
- `production_tier` - the `FactoryTier` the roll used, an index into `rewards_map`. It was resolved from the rarity of the bees or bugs inside when the request was made, and `FactoryTier.Mixed` is the tier for mixed rarities.

## Usage

```gml
// factory.product_drops is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function busy_hives_factory_product_drops(_value, _ctx) {
    // _value is the array of products about to drop, item ids or LiveItems.
    // _ctx is { node, production_request, production_tier }.
    // e.g. a second roll from the same tier:
    // var _pool = _ctx.node.prototype.factory.rewards_map[_ctx.production_tier];
    // array_push(_value, _pool[irandom(array_length(_pool) - 1)]);
    return undefined; // undefined = keep the (possibly mutated) array
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("factory.product_drops", busy_hives_factory_product_drops);
```

## Interactions

- Each element drops through its own `drop_item()` call at the factory's position, so [items.dropped](items.dropped.md) fires once per element, after this hook.
- [input.take_press](input.take_press.md) sees the give press first and can veto it, in which case this hook never fires.
- The request itself is rolled in `new_day_grid()` once the factory has been full for `days_to_produce` days, before [game.new_day](game.new_day.md) fires, so a handler there can rewrite `production_request` and `production_tier` on the nodes in `FACTORIES`. This hook covers only the moment the item changes hands.
- [object.interact](object.interact.md) covers interactions that reach `interact(node)`. The give interaction is a separate registration on the renderer, so it never reaches that override.

## Engine Wiring

- Seam [`factory_product_drops`](../seams/factory_product_drops.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml`, inside the give interaction callback, after gathering the two rolls into one array and before the drop loop.

## See Also

- [animal.product_drops](animal.product_drops.md) - The ranching counterpart. It filters a stable animal's daily products before they land.
- [items.dropped](items.dropped.md) - Know what is about to drop into the world.
- [items.treasure_distribution](items.treasure_distribution.md) - The dungeon counterpart. It filters a rolled drop before it lands.
- [game.new_day](game.new_day.md) - React after the daily request roll.
