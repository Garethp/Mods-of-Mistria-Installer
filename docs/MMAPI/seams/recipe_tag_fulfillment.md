# Engine Fix: recipe_tag_fulfillment

Counts a `tag` component's stock through the inventory's own tag helper, in the player's inventory and in each crafting chest, replacing a branch that threw.

`recipe_tag_fulfillment` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_tag_display](recipe_tag_display.md) and [recipe_tag_payment](recipe_tag_payment.md) it carries the `tag` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | text anchor: the Tag case of `maximum_fulfillment_for_component` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_tag_fulfillment` |

## The Edit

The pristine Tag case collects the player's inventory slots carrying the tag and sums `x.count()` over them. An inventory slot holds `count` as a numeric field and defines no such method, so the branch throws the first time a Tag component is costed. It also ignores `use_chests`. The engine's own `Inventory.item_tag_quantity(tag)` sums the same slots through the field correctly and is called nowhere in the pristine tree. The replace rebuilds the case in the Item case's shape on that helper. It reads the player's inventory total, then adds each storage node marked for crafting when `use_chests` is set, and returns the sum.

Because the case now honors `use_chests`, the crafting menu's two-call check, once with chests and once without, decides the storage icon for a Tag box the same way it does for an Item box. No vanilla recipe carries a `tag` component, so pristine data never reaches the case.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_tag_payment](recipe_tag_payment.md) - The payment that drains what this count promised.
- [recipe_items_fulfillment](recipe_items_fulfillment.md) - The Item case of the same function.
- [crafting_component_count](crafting_component_count.md) - The wrap on the cost function this count is compared against.
