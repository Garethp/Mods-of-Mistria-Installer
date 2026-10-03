# Engine Fix: recipe_items_factory

Adds a list form to the recipe component factory, an Item component that stores the first id as `item_id` and the full list as `item_ids`.

`recipe_items_factory` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_items_parse](recipe_items_parse.md), [recipe_items_fulfillment](recipe_items_fulfillment.md), and [recipe_items_payment](recipe_items_payment.md) it carries the `items` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | text anchor: the `Item` static of `RecipeComponentFactory` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_items_factory` |

## The Edit

Every recipe component is a plain struct built by one static on `RecipeComponentFactory`, and the `Item` static builds `{ type, item_id, count }`. The replace keeps that static intact and adds an `Items` static beside it. The new static takes an array of item ids and builds a struct of the same `RecipeComponentType.Item` type, with `item_id` set to the first id in the array and the whole array stored as `item_ids`.

Keeping the Item type is the whole design. The engine reads `component.item_id` in the ingot perk checks, the crafting menu's return-ingredient perks, the Living Off The Land perk, recipe pricing, dish restore values, the recipe tooltip, and the debug craft test. None of those sites change, and each sees the first listed item. Only the two cost sites that must treat the list as a union, fulfillment and payment, read `item_ids`, and both fall back to a one-element array when the member is absent, so a component built by the pristine `Item` static takes the pristine path. No vanilla recipe reaches the new static, since the parser only calls it for an `items` key.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_items_parse](recipe_items_parse.md) - The parser branch that calls the new static.
- [recipe_items_fulfillment](recipe_items_fulfillment.md) - The cost read that sums over `item_ids`.
- [recipe_items_payment](recipe_items_payment.md) - The payment that drains over `item_ids`.
