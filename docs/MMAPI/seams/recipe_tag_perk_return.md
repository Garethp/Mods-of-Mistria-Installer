# Engine Fix: recipe_tag_perk_return

Skips the crafting menu's ingredient return when a recipe has no Item component to return, instead of building an item from an empty list.

`recipe_tag_perk_return` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_tag_display](recipe_tag_display.md), [recipe_tag_fulfillment](recipe_tag_fulfillment.md), [recipe_tag_payment](recipe_tag_payment.md), and [recipe_tag_popup](recipe_tag_popup.md) it carries the `tag` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | text anchor on the line in `on_close` that pushes the randomly chosen ingredient onto the yield list |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_tag_perk_return` |

## The Edit

When the crafting menu closes, `on_close` walks the recipes crafted in that session and rolls the return perk of the menu's crafting context. On a successful roll it collects the recipe's Item components into a list, picks one at random, and pushes a new `LiveItem` of it onto the list of items to hand back. A recipe with no Item component leaves that list empty. The list's `choose_random` answers undefined for an empty list, and the `LiveItem` constructor indexes the item table with that answer. The replace wraps the push in a check that the list is not empty. The perk's trigger count and the secondary perk's count below the push are not touched, so a successful roll on such a recipe still counts as a trigger and returns nothing.

Every vanilla recipe carries at least one Item component, so pristine data never empties the list. A component parsed from an `items` list keeps the Item kind, so a recipe built from `items` entries alone is unaffected. Only a recipe built without `item` or `items` entries, such as one with nothing but `tag` entries, reaches the guard.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_tag_popup](recipe_tag_popup.md) - The other edit a recipe with no Item component needs.
- [max_crafts_zero_component](max_crafts_zero_component.md) - The catalog's other guard in this menu.
- [recipe_tag_display](recipe_tag_display.md) - The crafting menu's tag box.
