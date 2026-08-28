# Seam: animal_product_drops

Filters the product lists after the Eggstra roll, before stats and grid drops.

`animal_product_drops` is a **text seam** (`anchor` + `replace`). It feeds [animal.product_drops](../hooks/animal.product_drops.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Stable.gml` |
| **Locator** | text anchor on the `trellis_point` position line that opens the drop landing code in `on_new_day()` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.product_drops`](../hooks/animal.product_drops.md) |
| **Value filtered** | `{ normal_products, golden_products }`, the two rolled Lists |
| **ctx built** | `{ animal: animal, stable: self, production: production, production_tier: my_tier }` |
| **Marker** | `mmapi_stable_run_product_drops_filters` |

## The Edit

Pristine `on_new_day()` rolls both product lists, lets the Eggstra perk add its extra product, and then records stats and pushes the drops into the stable grid. The replacement inserts the dispatch between those halves. It wraps both Lists in a value struct, filters it, and re-reads the two fields defensively in the style of [request_board_fetch_pool](request_board_fetch_pool.md). A non-struct return keeps both engine lists, a partial struct keeps the missing field's list, and a site-level catch keeps everything if dispatch fails.

The stats records and the `is_empty()` tests run on the final lists, so a handler that empties both lists suppresses the drops and their records together, and injected items are recorded like rolled ones. The bead roll and the bonus gather item roll sit below the anchor and run regardless.

With zero handlers the filter returns the struct unchanged and both fields re-read to the engine lists, so the seam is behaviorally identical to pristine. The dispatch runs once per delivery, only for animals whose [animal_product_ready](animal_product_ready.md) decision passed.

## See Also

- [animal.product_drops](../hooks/animal.product_drops.md) - This is the hook this seam dispatches.
- [animal_product_ready](animal_product_ready.md) - Filters the readiness decision that releases this block.
- [request_board_fetch_pool](request_board_fetch_pool.md) - The defensive struct handling this seam follows.
