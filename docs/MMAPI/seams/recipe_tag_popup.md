# Engine Fix: recipe_tag_popup

Lets the popup that announces a learned recipe draw a `tag` component with the first tagged prototype's icon, so a recipe with nothing but `tag` entries no longer leaves its ingredient row empty.

`recipe_tag_popup` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_tag_display](recipe_tag_display.md), [recipe_tag_fulfillment](recipe_tag_fulfillment.md), [recipe_tag_payment](recipe_tag_payment.md), and [recipe_tag_perk_return](recipe_tag_perk_return.md) it carries the `tag` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/TooltipMenu.gml` |
| **Locator** | text anchor on the Essence case and the default case of the component filter in `new_item_popup`, the two lines that close its switch |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_tag_popup` |

## The Edit

`new_item_popup` announces a learned recipe with the recipe's ingredient row under the item. It builds the row by filtering the recipe's components through a switch that maps an Item component to its item id and count, maps an Essence component to the essence placeholder item, and drops every other kind. It then asks `centered_positions` for one slot per kept component and reads the first and last slots to size the backing box. A recipe with nothing but `tag` entries keeps nothing, the slot array is empty, and the read throws. The replace adds a Tag case above the default. The case walks the item table for the first prototype carrying the tag and maps the component to that prototype's id with the component count, the same walk the crafting menu's tag box takes, so the popup and the menu show the same icon for the tag. When no prototype carries the tag the case maps the component to an undefined item, which `item_node` draws as the engine's warning sprite for an unknown item with no count.

No vanilla recipe carries a `tag` component, so pristine data never reaches the case. Every vanilla recipe carries at least one Item component, so pristine data never empties the row either. A recipe's duration entry parses to a component kind that no case of the switch keeps, which is the vanilla behavior.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_tag_display](recipe_tag_display.md) - The crafting menu box that takes the same walk.
- [recipe_tag_perk_return](recipe_tag_perk_return.md) - The other edit a recipe with no Item component needs.
