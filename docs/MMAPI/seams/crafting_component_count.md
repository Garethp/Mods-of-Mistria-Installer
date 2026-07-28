# Seam: crafting_component_count

Filters every crafting cost read by wrapping the component-count resolver.

`crafting_component_count` is a **template seam** (`op = "wrap"`). It feeds [crafting.component_count](../hooks/crafting.component_count.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | structural target: the `get_modified_component_count(component, context, item_id)` function |
| **Op** | `wrap` |
| **Feeds** | [`crafting.component_count`](../hooks/crafting.component_count.md) |
| **ctx built** | `{ component: component, context: context, item_id: item_id }` |
| **Marker** | `mmapi_crafting_component_count` |

## The Edit

The wrap renames the pristine `get_modified_component_count` and emits a wrapper in its place. The wrapper calls the original, threads its return through `mmapi_apply_filters("crafting.component_count", result, { component, context, item_id })` under a try/catch (a throwing handler keeps the engine's count, fail-open), and returns the filtered value.

`get_modified_component_count` is the engine's own cost-modification point. The crafting perks (ingot discounts, the per-discipline time-reduction perks) apply inside it, and every cost consumer reads through it: `maximum_crafts`' ceiling computation, `can_fulfill_component`, and `pay_component_costs`' actual deduction (where a `Duration` component's count becomes a `CLOCK.jump`). Filtering the wrapper therefore adjusts a component's effective cost everywhere at once, consistently.

With zero handlers the filter dispatch early-outs on an empty registry, leaving pristine behavior.

## See Also

- [crafting.component_count](../hooks/crafting.component_count.md) - This is the hook this seam dispatches (including the zero-item-count warning).
- [crafting_max_crafts](crafting_max_crafts.md) - The craft-ceiling override in the crafting menu.
- [crafting_pay_component_costs](crafting_pay_component_costs.md) - The material-payment guard in the same engine file.
