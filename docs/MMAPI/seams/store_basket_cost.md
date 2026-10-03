# Seam: store_basket_cost

Filters the basket total inside the store's price pass, guarded so only a number replaces it.

`store_basket_cost` is a **text seam** (`anchor` + `replace`). It feeds [store.basket_cost](../hooks/store.basket_cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | text anchor on the close of the basket sum in `update_prices()` and the `self.total_count` line that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`store.basket_cost`](../hooks/store.basket_cost.md) |
| **Value filtered** | `self.basket_total` - the summed basket total, markup applied |
| **ctx built** | `{ menu: self }` |
| **Marker** | `mmapi_store_run_basket_cost_filters` |

## The Edit

`update_prices()` sums the basket into `self.basket_total`, then reads that one field for the receipt label, its can't-afford color, the remaining-gold figure the shelf coloring compares against, and the Buy gate. The Buy callback later charges the same field without recomputing it. The replacement lands between the sum and the first read. It runs `mmapi_apply_filters("store.basket_cost", self.basket_total, { menu: self })` in a try/catch and adopts the result only when it is numeric (`is_real` or `is_int64`).

A text seam rather than a template one, because the dispatch carries its own type guard. The total is compared against the player's gold and subtracted from it right after the site's catch closes, so a non-numeric handler value would survive the catch and throw at the receipt's affordability compare, outside any isolation. The guard only binds on handler output vanilla never produces. With zero handlers the pristine lines around the added block are byte-identical and the total is untouched.

## See Also

- [store.basket_cost](../hooks/store.basket_cost.md) - This is the hook this seam dispatches.
- [items_store_price](items_store_price.md) - The per-item price wrap whose values this sum is built from.
- [store_purchase](store_purchase.md) - The emit that reports this total as it is charged.
- [player_essence_delta](player_essence_delta.md) - The delta filter this guard shape comes from.
