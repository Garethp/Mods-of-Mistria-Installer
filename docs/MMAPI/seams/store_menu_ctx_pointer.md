# Seam: store_menu_ctx_pointer

Holds the StoreMenu in a framework global for the span of its constructor's `init()` call, so store hooks that fire inside it can name the menu.

`store_menu_ctx_pointer` is a **text seam** and a **companion edit**. It dispatches nothing itself. It exists for [items.store_price](../hooks/items.store_price.md) and [store.stock](../hooks/store.stock.md), whose dispatches live in [items_store_price](items_store_price.md) and [store_stock](store_stock.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | text anchor on the `self.init();` call that ends the `StoreMenu` constructor |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`items.store_price`](../hooks/items.store_price.md) and [`store.stock`](../hooks/store.stock.md) (no dispatch of its own) |
| **Marker** | `mmapi_store_menu_ctx_pointer` |

## The Edit

This seam dispatches nothing. The `StoreMenu` constructor ends by calling `self.init()`, which builds the stock, prices the shelf, sums the basket, and spawns the first tooltip. ANCHOR pushes the menu onto `open_menus` only after the constructor returns, so `ANCHOR.get_menu(Menu.Store)` is `undefined` for that whole pass. The replace sets `global.__mmapi_store_menu = self` on the line before `self.init();` and sets it back to `undefined` on the line after.

The two wraps whose dispatch can run inside that span read the global into their ctx. [items_store_price](items_store_price.md) reads it with `ANCHOR.get_menu(Menu.Store)` as the fallback for every later pass, and [store_stock](store_stock.md) reads it alone, because its other three call sites have no menu. `self.store` is assigned at the top of the constructor, so the menu a handler receives during the span already names its store.

Both reads go through `global[$ "__mmapi_store_menu"]`, which yields `undefined` when the global was never set. With zero handlers nothing reads it, and the two added assignments are the only change to the constructor.

## See Also

- [items.store_price](../hooks/items.store_price.md) - This is a hook this companion edit serves.
- [store.stock](../hooks/store.stock.md) - This is a hook this companion edit serves.
- [items_store_price](items_store_price.md) - This is the dispatching wrap whose `ctx.menu` this pointer fills during construction.
- [store_stock](store_stock.md) - This is the dispatching wrap whose `ctx.menu` this pointer fills on the init fire.
- [store_basket_cost](store_basket_cost.md) - This is a sibling seam in `StoreMenu.gml`.
