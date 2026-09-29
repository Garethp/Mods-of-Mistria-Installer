# Seam: tool_targets

Filters the cells a tool use covers where the Tool state resolves them each step.

`tool_targets` is a **text seam** (`anchor` + `replace`). It feeds [tool.targets](../hooks/tool.targets.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the `var targets = range_pattern_to_targets(...)` line in the `PlayerState.Tool` step, through the `tool_charge_released` condition that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`tool.targets`](../hooks/tool.targets.md) |
| **Value filtered** | the `List` of `Vec2` cells that `range_pattern_to_targets()` returned |
| **ctx built** | `{ x: self.target_pos.x, y: self.target_pos.y, range_pattern: self.range_pattern, cardinal: self.owner.cardinal, item: self.live_item }` |
| **Marker** | `mmapi_tool_run_targets_filters` |

## The Edit

The Tool state's step resolves the cells a use covers once per step into the local `targets`, and it reads that one `List` in two places. While the player charges, it clears the tile cursor's charging shadows and adds one per cell. On the step the use acts, it loops over the cells and calls the item's callback for each. The replacement filters `targets` between the call that builds it and the first read, so both reads see the final value.

The dispatch sits at this call site and not inside `range_pattern_to_targets()`. That function has a second caller, the Earthbreaker perk in `Pick.gml`, which asks for `RangePattern.OneByThree` and skips index 1 of the result as the rock the pick already struck. A filter inside the function would reshape that read as well.

The dispatch runs only while `self.has_done` is false. The engine sets `has_done` on the step the use acts, after this line, and never reads `targets` on a later step. An instant use sets `has_done` in the state's start, where it acts on its one cell without reading a `List`, so it never dispatches.

The replacement assigns the final value back to `targets` only when its `count()` call succeeds. An array, or any other return that is not a `List`, throws inside the site-level catch and leaves the engine's `List` in place, which keeps the step's loops from failing on their first read.

With zero handlers the filter returns the engine's `List` unchanged, so the step reads the same cells as pristine. The dispatch runs every step while a tool is in use, so handlers should keep their first test cheap.

## See Also

- [tool.targets](../hooks/tool.targets.md) - This is the hook this seam dispatches.
- [tool_targets_stamina_gate](tool_targets_stamina_gate.md) - This is the second dispatch site, which counts the filtered cells in the charge loop's stamina test.
- [pick_node_modifier](pick_node_modifier.md) - Filters the modifier the pickaxe callback passes for each cell of a charged use.
- [chop_node_modifier](chop_node_modifier.md) - Filters the modifier the axe callback passes for each cell of a charged use.
