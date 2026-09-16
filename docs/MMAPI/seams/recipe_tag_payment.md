# Engine Fix: recipe_tag_payment

Pays a `tag` component's quantity-scaled cost through the inventory's own tag helper, the player's inventory first and then each crafting chest.

`recipe_tag_payment` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_tag_display](recipe_tag_display.md) and [recipe_tag_fulfillment](recipe_tag_fulfillment.md) it carries the `tag` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | text anchor: the Tag case of `pay_component_costs`, from the case label through its closing `break` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_tag_payment` |

## The Edit

The pristine Tag case has three defects the Item case beside it does not. It reads a slot's count as a method where the slot holds a field, so it throws on first use. It starts from `component.count`, the unscaled base, where the function has already computed the perk-modified, quantity-scaled `count` for every kind, so a craft of five would pay for one. And it never reaches a storage node, so an ingredient the fulfillment count found in a chest would go unpaid. The replace rebuilds the case in the Item case's shape on the engine's own `Inventory.remove_items_with_tag(tag, amount)`, which drains tagged slots through the field correctly and is called nowhere in the pristine tree. It removes as much of the scaled count as the player's inventory holds, then walks each storage node marked for crafting until the count reaches zero, and closes with the Item case's assertion.

The assertion mirrors the Item case and can only fail if fulfillment and payment disagree about the same inventory, which the paired helpers rule out. No vanilla recipe carries a `tag` component, so pristine data never reaches the case.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_tag_fulfillment](recipe_tag_fulfillment.md) - The count this payment honors.
- [recipe_items_payment](recipe_items_payment.md) - The Item case of the same function.
- [crafting.pay_component_costs](../hooks/crafting.pay_component_costs.md) - The guard that can skip this payment entirely.
