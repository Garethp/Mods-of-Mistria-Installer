# Seam: store_tooltip_price_refresh

Has the StoreMenu's price pass rewrite the open tooltip's price, so a filtered price that changes while the shop is open reaches the tooltip without a re-spawn.

`store_tooltip_price_refresh` is a **text seam** and a **companion edit**. It dispatches nothing itself. It exists for [items.store_price](../hooks/items.store_price.md), whose dispatch lives in [items_store_price](items_store_price.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | text anchor on the close of the shelf loop inside `update_prices()` and the `self.buy_button.set_unlocked(...)` line that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`items.store_price`](../hooks/items.store_price.md) (no dispatch of its own) |
| **Marker** | `mmapi_store_tooltip_price_refresh` |

## The Edit

This seam dispatches nothing. `create_tooltip()` writes the tooltip's price once, when the tooltip spawns, into a text node it keeps as `gold_text`, and the `StoreMenu` keeps the open tooltip as `self.tooltip` along with the item it describes. The replace adds a guarded block between the shelf loop and the Buy gate. When a tooltip is open and carries a price node, the block writes `round(self.tooltip.item.store_value() * self.price_markup)` into that node.

That is the expression the tooltip used at spawn, with the store's buy-price branch and markup, so with zero handlers the text written is the text already shown. The block never spawns, closes, or moves the tooltip, so the vanilla hover behavior is untouched. It adds one `store_value()` call per price pass, which is one more [items.store_price](../hooks/items.store_price.md) fire per basket change while a tooltip is open.

The tooltip's title, description, stars, and icon are built once at spawn and are outside this edit. Only the price node is refreshed.

## See Also

- [items.store_price](../hooks/items.store_price.md) - This is the hook this companion edit serves.
- [items_store_price](items_store_price.md) - This is the dispatching wrap whose filtered value this edit puts on the tooltip.
- [store_shelf_label_refresh](store_shelf_label_refresh.md) - This is the sibling companion edit that puts the same value on the shelf labels.
- [store_basket_cost](store_basket_cost.md) - This is the sibling seam earlier in the same price pass.
