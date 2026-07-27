# Engine Fix: max_crafts_zero_component

Skips zero-cost components in the craft-ceiling loop, mirroring the zero guard the duration branch already has.

`max_crafts_zero_component` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. The added guard line is the whole feature. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | text anchor: the Item-component loop inside `maximum_crafts(item)` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_max_crafts_zero_component` |

## The Edit

`maximum_crafts` computes the craft ceiling as the minimum over components of `available div per-craft-cost`. Its Duration branch guards that division (`if denominator != 0`) because the engine's own time perks can legitimately reduce a duration to zero. The Item-component loop a few lines below runs the same division unguarded, so a zero item count is a division-by-zero crash. While the game itself never produces one, a modded recipe via a [crafting.component_count](../hooks/crafting.component_count.md) handler returning 0, or a future 1-ingredient perk-discountable recipe would crash the crafting menu.

The replace inserts the same guard the Duration branch uses. A zero-cost component is skipped (`continue`), meaning it imposes no ceiling, which is exactly the Duration branch's established semantics. If every component is zero-cost the ceiling stays `I32_MAX`, which downstream handles safely. `check_item_craftable` is a comparison and the MAX button routes through `modify_quantity`, whose `clamp(..., 1, 999)` bounds any craft run at 999 (the engine's own quantity ceiling). `can_fulfill_component` (multiplication) and `pay_component_costs` (removes zero, `assert_eq(0, 0)` holds) were already zero-safe, so this loop was the single unguarded site.

## See Also

- [crafting.component_count](../hooks/crafting.component_count.md) - The filter whose zero-count returns this fix makes safe.
- [tarball_chop_burn_flag](tarball_chop_burn_flag.md) - This is another of the catalog's engine fixes, the same guarded-sibling-unguarded-twin pattern.
- [crafting_max_crafts](crafting_max_crafts.md) - The override seam at the head of the same function.
