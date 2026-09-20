# Engine Fix: recipe_items_parse

Parses a recipe component's `items` list into the factory's list form, resolving names through the same lookup as `item`.

`recipe_items_parse` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_items_factory](recipe_items_factory.md), [recipe_items_fulfillment](recipe_items_fulfillment.md), and [recipe_items_payment](recipe_items_payment.md) it carries the `items` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/Items.gml` |
| **Locator** | text anchor: the `item` branch of the recipe component chain in `create_item_prototypes`, through the `else if` that opens the `tag` branch |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_items_parse` |

## The Edit

`create_item_prototypes` turns each entry of an item's `recipe` array into a component by testing the keys `item`, `tag`, `hours`, `gold`, `skill`, and `essence` in turn, and a component with none of them stops the game at Setup. The replace keeps the `item` branch as it is and inserts an `items` branch directly after it, before `tag`. The new branch walks the array, resolves every name with `string_to_item_id`, the same native the `item` branch uses, drops any id already collected, and hands the array to the factory's `Items` static with the component's `count`, defaulting to 1 as the `item` branch does.

The dedupe matters because fulfillment sums the player's stock over every listed id. A repeated id would count the same stack twice, let the craft pass its check, and then fail the payment's closing assertion. Chain order is the only precedence rule. A component that carries both `item` and `items` takes the `item` branch, exactly as a component carrying `item` and `tag` does in vanilla. An unknown name fails inside the lookup at Setup, as a misspelled `item` does today. No vanilla component carries `items`, so pristine data never enters the branch.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_items_factory](recipe_items_factory.md) - The static this branch calls.
- [fish_chest_item_use](fish_chest_item_use.md) - The catalog's other edit to the item prototype loader.
