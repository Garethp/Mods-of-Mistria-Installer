# Seam: store_shelf_label_refresh

Has the StoreMenu's price pass rewrite each shelf label's price text, so a filtered price that changes while the shop is open reaches the shelf.

`store_shelf_label_refresh` is a **text seam** and a **companion edit**. It dispatches nothing itself. It exists for [items.store_price](../hooks/items.store_price.md), whose dispatch lives in [items_store_price](items_store_price.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | text anchor on the shelf loop inside `update_prices()`, the `var value = round(item.store_value() * self.price_markup);` line and the `text.set_color(...)` line that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`items.store_price`](../hooks/items.store_price.md) (no dispatch of its own) |
| **Marker** | `mmapi_store_shelf_label_refresh` |

## The Edit

This seam dispatches nothing. The `StoreMenu` writes a shelf label's price text in `select_category()`, which runs when the menu opens and when the carousel changes tabs. Its price pass, `update_prices()`, runs on every basket change and recomputes the same number for every shelf item, but uses it only to choose between the can't-afford colour and white. The replace adds `text.set_text(value);` between the two pristine lines, so the pass writes the number it already computed.

The edit matters only when a handler's answer changes while the menu is open. Without it the label keeps the old number until the player changes tabs, while the label colour, the receipt, and the Buy lock already follow the new one, so a label can read a price the basket does not charge. With it, the next price pass corrects the label, and a handler that wants the correction at once calls `menu.update_prices()`.

The expression is the one `select_category()` uses, `round(item.store_value() * self.price_markup)`, so with zero handlers the text written is the text already shown. The tooltip is outside this edit. Its price node is refreshed by the sibling edit [store_tooltip_price_refresh](store_tooltip_price_refresh.md) in the same pass.

## See Also

- [items.store_price](../hooks/items.store_price.md) - This is the hook this companion edit serves.
- [items_store_price](items_store_price.md) - This is the dispatching wrap whose filtered value this edit puts on the shelf.
- [store_basket_cost](store_basket_cost.md) - This is the sibling seam earlier in the same price pass.
- [store_tooltip_price_refresh](store_tooltip_price_refresh.md) - This is the sibling companion edit that refreshes the open tooltip's price in the same pass.
- [store_menu_ctx_pointer](store_menu_ctx_pointer.md) - This is the other companion edit in `StoreMenu.gml`.
