# Seam: animal_production_gate

Filters the daily production gate so unhappy or baby animals can accrue and produce.

`animal_production_gate` is a **text seam** (`anchor` + `replace`). It feeds [animal.production_gate](../hooks/animal.production_gate.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Stable.gml` |
| **Locator** | text anchor on the production gate condition inside `on_new_day()`'s animal loop, including the following `production_days` increment |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.production_gate`](../hooks/animal.production_gate.md) |
| **Value filtered** | the boolean `!animal.is_baby() && is_happy` |
| **ctx built** | `{ animal: animal, stable: self, production: production, is_happy: is_happy }` |
| **Marker** | `mmapi_stable_run_production_gate_filters` |

## The Edit

Pristine `on_new_day()` tests the gate inline at the head of each animal's production step. The replacement evaluates the pristine condition once into `__mmapi_animal_production_gate`, filters that boolean through `mmapi_apply_filters("animal.production_gate", ...)` with a ctx built entirely from locals already in scope, then uses the final value as the original `if` condition. A site-level catch leaves the captured result unchanged if ctx construction, dispatch, or assignment fails.

The seam sits after the day's happiness bookkeeping. By the time it fires, the heart penalties for hunger, missed petting, and a night outside have applied, and so have the CloseBond bonuses. A final true enters the pristine step, whose first statement is the `production_days` increment the anchor carries. A final false skips the step, exactly as a failed pristine gate would.

The paired [animal_product_ready](animal_product_ready.md) seam edits the next lines of the same function. The two anchors are disjoint and each replace preserves the other's anchor text, so the pair applies in either order with no `depends_on` edge.

With zero handlers the filter returns the captured condition unchanged, so the seam is behaviorally identical to pristine. The dispatch runs once per stalled animal per stable per day, and also when the engine's own debug and test-suite helpers skip days.

## See Also

- [animal.production_gate](../hooks/animal.production_gate.md) - This is the hook this seam dispatches.
- [animal_product_ready](animal_product_ready.md) - Filters the readiness comparison inside the step this gate releases.
- [animal_heart_points](animal_heart_points.md) - Filters the heart deltas the happiness bookkeeping above this seam applies.
