# Seam: store_item_added

Announces every shelf tap that puts an item in the shopping basket.

`store_item_added` is a **text seam** (emit-shaped: it dispatches `mmapi_emit`). It feeds [store.item_added](../hooks/store.item_added.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | text anchor right after `self.basket.add(item);` at the shelf-tap site |
| **Op** | text (emit dispatch) |
| **Feeds** | [`store.item_added`](../hooks/store.item_added.md) |
| **ctx built** | `{ menu: self, item: item }` |
| **Marker** | `mmapi_store_run_item_added` |

## The Edit

One injected line lands in the StoreMenu's shelf-tap handling, immediately after the pristine `self.basket.add(item);`:

```gml
try { mmapi_emit("store.item_added", { menu: self, item: item }); } catch (__mmapi_store_item_added) {} // mmapi_store_run_item_added
```

It fires right after a shelf tap adds an item to the shopping basket, before the ghost and price update. ctx carries the `StoreMenu` (`menu.basket` is the `Inventory` cart with `.can_add(item)`/`.add(item)`, and `menu.update_prices()` refreshes the totals) and the `LiveItem` just added.

The anchoring is the point: the emit sits at the tap site, not inside `basket.add`. A handler that calls `menu.basket.add(...)` to add more copies does not re-fire the event, so no re-entry guard is needed. A bulk-buy handler can add extra copies freely. With zero handlers the emit early-outs on an empty registry, leaving pristine behavior.

## See Also

- [store.item_added](../hooks/store.item_added.md) - This is the hook this seam dispatches.
- [ui_menu_opened](ui_menu_opened.md) - This seam is the emit for a menu (the store included) being opened.
- [items_give](items_give.md) - This is the struct filter every item grant passes through.
