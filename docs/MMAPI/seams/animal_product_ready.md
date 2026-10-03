# Seam: animal_product_ready

Filters the readiness comparison after the counter increments, before the drop block.

`animal_product_ready` is a **text seam** (`anchor` + `replace`). It feeds [animal.product_ready](../hooks/animal.product_ready.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Stable.gml` |
| **Locator** | text anchor on the readiness comparison inside `on_new_day()`'s production step, including the following counter reset |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.product_ready`](../hooks/animal.product_ready.md) |
| **Value filtered** | the boolean `animal.production_days >= production.days_to_produce` |
| **ctx built** | `{ animal: animal, stable: self, production: production, is_happy: is_happy }` |
| **Marker** | `mmapi_stable_run_product_ready_filters` |

## The Edit

Pristine `on_new_day()` compares the freshly incremented `production_days` against the prototype's `days_to_produce` and, when the counter reaches it, resets the counter and runs the drop block. The replacement evaluates the pristine comparison once into `__mmapi_animal_product_ready`, filters that boolean through `mmapi_apply_filters("animal.product_ready", ...)` with a ctx built entirely from locals already in scope, then uses the final value as the original `if` condition. A site-level catch leaves the captured result unchanged if ctx construction, dispatch, or assignment fails.

A final true enters the pristine block without replacing any of it. The counter reset, the tier selection from heart level, the BarnyardBounty and Eggstra perk rolls, the `GAME_STATS` records, the grid drops, the CurrencyOfCareTwo bead roll, and the bonus gather item spawn remain engine-owned. A final false skips the block and leaves the incremented counter in place, so the counter keeps climbing until a later day passes the comparison.

The paired [animal_production_gate](animal_production_gate.md) seam edits the preceding lines of the same function and decides whether this site is reached at all. The two anchors are disjoint and each replace preserves the other's anchor text, so the pair applies in either order with no `depends_on` edge.

With zero handlers the filter returns the captured comparison unchanged, so the seam is behaviorally identical to pristine. The dispatch runs once per day for each animal the gate lets through, and also when the engine's own debug and test-suite helpers skip days.

## See Also

- [animal.product_ready](../hooks/animal.product_ready.md) - This is the hook this seam dispatches.
- [animal_production_gate](animal_production_gate.md) - Filters the outer gate that decides whether this comparison runs.
- [new_day_complete](new_day_complete.md) - Emits the day-boundary event after all stables have processed.
