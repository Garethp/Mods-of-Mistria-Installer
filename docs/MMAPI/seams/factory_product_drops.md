# Seam: factory_product_drops

Gathers a factory's rolled products into one array and filters it before the first drop.

`factory_product_drops` is a **text seam** (`anchor` + `replace`). It feeds [factory.product_drops](../hooks/factory.product_drops.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml` |
| **Locator** | text anchor on the give interaction callback in `create_furniture_renderer()`, from the tier reward read through the reward sound |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`factory.product_drops`](../hooks/factory.product_drops.md) |
| **Value filtered** | the array of products about to drop, `[reward]` or `[reward, ItemId.Honeycomb]` |
| **ctx built** | `{ node: self, production_request: self.production_request, production_tier: self.production_tier }` |
| **Marker** | `mmapi_factory_run_product_drops_filters` |

## The Edit

Pristine pops the requested item from the player's hand, drops one random entry of the tier's reward array, rolls a 15 percent honeycomb bonus for an apiary and drops that, and then plays the reward sound and resets the request. A filter over the whole product list needs both rolls settled before the first drop, so the replacement gathers them first. It rolls the tier reward into an array of one element, pushes the honeycomb after the same apiary test and the same `chance_percent(15)` roll, threads the array through `mmapi_apply_filters` under a site-level catch, and then drops each element through its own `drop_item()` call at the renderer position. A return that is not an array keeps the rolled array. The hand pop stays above the dispatch, and the sound and reset stay below the loop.

Dropping per element rather than passing the array to one `drop_item()` call keeps the drop sound and the [items_dropped](items_dropped.md) emit matched to pristine call by call, where each product was its own call. The honeycomb roll now happens before the first drop instead of between the two drops. The two rolls keep their pristine order relative to each other, and only the drop scatter's random draws move behind them, so with zero handlers the same rolls decide every outcome and the seam is behaviorally, not byte, equivalent to pristine on that one point.

The interaction is registered only while the node carries a `production_request`, and `self` inside its callback is the factory node, which is where the ctx reads come from.

## See Also

- [factory.product_drops](../hooks/factory.product_drops.md) - This is the hook this seam dispatches.
- [animal_product_drops](animal_product_drops.md) - The ranching counterpart, a struct filter between the rolls and the grid drops.
- [items_dropped](items_dropped.md) - The emit each element of the final array passes through.
- [input_take_press](input_take_press.md) - The veto on the give press, ahead of this callback.
