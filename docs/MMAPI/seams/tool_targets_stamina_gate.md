# Seam: tool_targets_stamina_gate

Filters the cells again in the charge loop, so the stamina test counts the cells the use will cover.

`tool_targets_stamina_gate` is a **text seam** (`anchor` + `replace`). It feeds [tool.targets](../hooks/tool.targets.md) as that hook's second dispatch site. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the `var stamina = abs(live_item.prototype.stamina_cost);` line in the `PlayerState.Tool` charge loop and the `var can_afford = ...` line that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`tool.targets`](../hooks/tool.targets.md) |
| **Value filtered** | a new `List` from `range_pattern_to_targets()` for the range pattern the charge just advanced to |
| **ctx built** | `{ x: self.target_pos.x, y: self.target_pos.y, range_pattern: self.range_pattern, cardinal: self.owner.cardinal, item: self.live_item }` |
| **Marker** | `mmapi_tool_run_targets_stamina_gate` |

## The Edit

Each time the charge advances to the next range pattern, the charge loop tests whether the player can afford it. Pristine code multiplies `range_pattern_to_count(self.range_pattern)` by the item's stamina cost per cell and compares the product against current stamina. A pattern that fails the test resets the charge to `RangePattern.One`. The table behind `range_pattern_to_count()` knows only the engine's own cell counts, so it cannot follow a filtered cell set.

The replacement keeps the table count as its starting number. It then builds the pattern's `List` with the same `range_pattern_to_targets()` arguments the step site uses, filters it through `mmapi_apply_filters("tool.targets", ...)` with the same ctx shape, and replaces the number with the `count()` of the final value. The `can_afford` line multiplies that number instead of the table read. A larger filtered cell set therefore needs the stamina for every cell before the pattern is allowed, and a smaller one is allowed on the stamina it needs.

The table's counts equal the lengths of the Lists that `range_pattern_to_targets()` builds for the same patterns, so with zero handlers the test compares the same number as pristine. A site-level catch leaves the table count in place when the dispatch or the `count()` read fails.

The site fires once per charge advance, which the `misc/tool_range_cycle_rate` fiddle value paces, so it is not a hot path.

## See Also

- [tool.targets](../hooks/tool.targets.md) - This is the hook this seam dispatches.
- [tool_targets](tool_targets.md) - This is the step dispatch site, whose final value the preview and the action loop read.
- [player_stamina_delta](player_stamina_delta.md) - Filters the stamina cost each cell's action spends after this test allows the pattern.
