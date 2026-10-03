# Seam: items_store_price

Wraps `LiveItem.store_value()` so every buy-side price lookup is filterable.

`items_store_price` is a **template seam** (`op = "wrap"`). It feeds [items.store_price](../hooks/items.store_price.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/LiveItem.gml` |
| **Locator** | whole-function wrap of `store_value()` |
| **Op** | `wrap` |
| **Feeds** | [`items.store_price`](../hooks/items.store_price.md) |
| **Value filtered** | the base buy price `store_value()` returns |
| **ctx built** | `{ item: self, menu: global[$ "__mmapi_store_menu"] ?? ANCHOR.get_menu(Menu.Store) }` |
| **Marker** | `mmapi_items_store_price` |

## The Edit

`store_value()` is the one place the engine computes what a store charges for an item before the store's markup, which is the prototype's `value.store`, the star price for a recipe scroll, a cosmetic's price override, and the `DiscountTreats` perk. Its five callers are all on the buy side, the shelf price labels, the shelf can't-afford colouring, the basket sum, the store tooltip price, and the purchase stat's cost, and every one applies the store's markup on top. Wrapping the method funnels all of them to a single filtered return. The pristine `static store_value` is renamed with its body untouched, and the generated wrapper calls it and filters the returned number through `mmapi_apply_filters("items.store_price", <return>, <ctx>)` in the uniform try/catch shape. The sell price, `bin_value()`, is a separate method and is not wrapped.

`store_value()` knows the item and nothing about the store, so the ctx resolves the menu itself. The first price pass runs inside the `StoreMenu` constructor, before ANCHOR pushes the menu onto `open_menus`, and there the framework global that [store_menu_ctx_pointer](store_menu_ctx_pointer.md) sets answers. On every later pass the global is `undefined` and `ANCHOR.get_menu(Menu.Store)` answers. Both name the same `StoreMenu`.

With zero handlers a wrap is behaviorally (not byte-) equivalent to pristine. It adds one call frame and an empty-registry early-out.

## See Also

- [items.store_price](../hooks/items.store_price.md) - This is the hook this seam dispatches.
- [store_menu_ctx_pointer](store_menu_ctx_pointer.md) - The companion edit that fills `ctx.menu` during the constructor pass.
- [store_shelf_label_refresh](store_shelf_label_refresh.md) - The companion edit that puts a mid-visit price change on the shelf labels.
- [store_tooltip_price_refresh](store_tooltip_price_refresh.md) - The companion edit that puts a mid-visit price change on the open tooltip.
- [store_basket_cost](store_basket_cost.md) - The basket-level filter that sums these values.
- [ui_item_icon_live_item](ui_item_icon_live_item.md) - The `LiveItem.gml` wrap for the icon sprite.
- [item_display_description](item_display_description.md) - The `LiveItem.gml` wrap for the tooltip description.
