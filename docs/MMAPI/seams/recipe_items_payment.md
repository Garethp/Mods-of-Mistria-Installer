# Engine Fix: recipe_items_payment

Drains a list component's cost across the listed ids in order, the player's inventory first and then each crafting chest.

`recipe_items_payment` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_items_factory](recipe_items_factory.md), [recipe_items_parse](recipe_items_parse.md), and [recipe_items_fulfillment](recipe_items_fulfillment.md) it carries the `items` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | text anchor: the Item case of `pay_component_costs`, from the case label through its closing `break` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_items_payment` |

## The Edit

`pay_component_costs` runs once the craft is committed, after the [crafting.pay_component_costs](../hooks/crafting.pay_component_costs.md) guard at its head has allowed the payment. Its Item case removes the quantity-scaled count of the component's single id from the player's inventory, then from each storage node marked for crafting until the count is met, and asserts that nothing is left owing. The replace keeps that shape and turns each removal into a loop over `component.item_ids`, with the same one-element fallback the fulfillment edit uses. Within the player's inventory, and again within each chest, the listed ids drain in order and the loop stops the moment the count reaches zero, so the first listed item is spent before any later one.

The closing assertion is the pristine line. It can only fail if fulfillment and payment disagree about the same inventory, which the shared `item_ids` loop rules out.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_items_fulfillment](recipe_items_fulfillment.md) - The count this payment honors.
- [recipe_tag_payment](recipe_tag_payment.md) - The Tag case of the same function.
- [crafting.pay_component_costs](../hooks/crafting.pay_component_costs.md) - The guard that can skip this payment entirely.
