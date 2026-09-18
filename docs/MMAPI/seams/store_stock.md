# Seam: store_stock

Wraps `create_store_stock()`, the one builder every store shelf reads.

`store_stock` is a **template seam** (`op = "wrap"`). It feeds [store.stock](../hooks/store.stock.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Stores.gml` |
| **Locator** | whole-function wrap of `create_store_stock(store)` |
| **Op** | `wrap` |
| **Feeds** | [`store.stock`](../hooks/store.stock.md) |
| **Value filtered** | the `List` of `{ icon, items }` categories the builder returns |
| **ctx built** | `{ store: store, menu: global[$ "__mmapi_store_menu"] }` |
| **Marker** | `mmapi_store_stock` |

## The Edit

`create_store_stock(store)` reads the store's fiddle categories, applies the requirements and `purchasable()` gates, draws the daily random picks, adds the carpenter's kitchen upgrades and the festival stalls' tiers, fills the recipe tab, prunes empty categories, restores the random seed, and returns the `List` the shelf is built from. Wrapping the function funnels every consumer to a single filtered return. The pristine definition is renamed with its body untouched, and the generated wrapper calls it and filters the returned `List` through `mmapi_apply_filters("store.stock", <return>, <ctx>)` in the uniform try/catch shape.

The wrap covers all four callers, which a seam inside the store menu could not: `StoreMenu.init()` on every open, the Inn's `INN_STOCK` build at game setup and on each new day, and the blacksmith ledger's tool display. That breadth is the point. The counter display and the shelf are built from the same filtered value.

ctx carries the `Store` id the builder was called with. Its `menu` field reads the framework global that [store_menu_ctx_pointer](store_menu_ctx_pointer.md) sets for the span of the `StoreMenu` constructor's `init()` call, so it is the menu under construction on the init fire and `undefined` at the three sites that have no menu.

With zero handlers a wrap is behaviorally (not byte-) equivalent to pristine. It adds one call frame and an empty-registry early-out.

## See Also

- [store.stock](../hooks/store.stock.md) - This is the hook this seam dispatches.
- [store_menu_ctx_pointer](store_menu_ctx_pointer.md) - The companion edit that fills `ctx.menu` on the init fire.
- [store_item_added](store_item_added.md) - The emit for a shelf tap on any entry, added ones included.
- [store_purchase](store_purchase.md) - The emit where sold-out state gets written before the next build.
- [store_pet_cosmetic_entry](store_pet_cosmetic_entry.md) - The engine fix that widened the stock parser this builder feeds from.
