# Engine Fix: recipe_items_fulfillment

Sums a list component's stock across every listed id, in the player's inventory and in each crafting chest.

`recipe_items_fulfillment` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_items_factory](recipe_items_factory.md), [recipe_items_parse](recipe_items_parse.md), and [recipe_items_payment](recipe_items_payment.md) it carries the `items` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | text anchor: the Item case of `maximum_fulfillment_for_component` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_items_fulfillment` |

## The Edit

`maximum_fulfillment_for_component` answers how much of one component the player can pay. Its Item case reads `item_id_quantity` for the component's single id from the player's inventory, then from every storage node marked for crafting when `use_chests` is set. The replace keeps that shape and wraps each read in a loop over `component.item_ids`, falling back to a one-element array holding `component.item_id` when the member is absent. A single-item component therefore performs the same reads as before, and a list component sums its stock across every listed id.

Every craft decision flows through this count. `can_fulfill_component` compares it to the perk-modified, quantity-scaled cost, `maximum_crafts` divides it for the craft ceiling, and the crafting menu prints it as the has side of has/needs and decides whether to show the storage icon by calling it with and without chests. All of those see the union without change.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_items_payment](recipe_items_payment.md) - The payment that drains what this count promised.
- [recipe_tag_fulfillment](recipe_tag_fulfillment.md) - The Tag case of the same function.
- [crafting.max_crafts](../hooks/crafting.max_crafts.md) - The override on the ceiling this count feeds.
