# Engine Fix: recipe_tag_display

Lets the crafting menu draw a `tag` component with the first tagged prototype's icon and the shared has/needs count block.

`recipe_tag_display` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [recipe_tag_fulfillment](recipe_tag_fulfillment.md) and [recipe_tag_payment](recipe_tag_payment.md) it carries the `tag` half of the [Recipe Ingredients](../RECIPE_INGREDIENTS.md) contract. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | text anchor: the head of the Item case in the ingredient render switch of `set_to_item`, the case label and its icon line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_recipe_tag_display` |

## The Edit

`set_to_item` draws one box per recipe component. An earlier switch builds a `LiveItem` for Item and Essence components so a hover can open a tooltip, and the render switch then handles those two kinds, the Item kind with an icon plus a has/needs count block, and stops the game with `impossible(...)` on any other kind. The replace puts a Tag label above the Item label so a Tag component falls through into the Item body, and splits only the icon line. For a Tag it first sets `spr_illegal_16`, the engine's unknown-item warning sprite, then walks the item table for the first prototype carrying the tag and sets that prototype's `icon_sprite` over it. For an Item it sets the `LiveItem` icon as before. Everything below the icon line, the has count from `maximum_fulfillment_for_component`, the 99+ clamp, and the three text nodes, is shared unchanged.

No `LiveItem` is built for a Tag, which keeps the earlier switch untouched, so the box draws no tooltip on hover. Reading `icon_sprite` from the prototype rather than through `get_ui_icon` also avoids the cosmetic branches of that method, which dereference fields a bare `LiveItem` does not carry. A tag that no prototype carries keeps the warning sprite, the same icon the engine draws in an inventory slot for an item id that no longer resolves, with the count at zero. The `impossible` default still stops the game for the Gold and SkillReq kinds, which no vanilla recipe uses either.

## See Also

- [Recipe Ingredients](../RECIPE_INGREDIENTS.md) - The contract for mod authors that this edit carries.
- [recipe_tag_fulfillment](recipe_tag_fulfillment.md) - The count the shared block prints for a Tag.
- [max_crafts_zero_component](max_crafts_zero_component.md) - The catalog's other engine fix in this menu.
- [crafting_max_crafts](crafting_max_crafts.md) - The override seam at the head of the ceiling function.
