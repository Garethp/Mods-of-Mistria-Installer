# Hook: store.stock

Change what a store's shelves hold.

`store.stock` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `create_store_stock(store)`, the one builder every store shelf reads. The filtered value is the `List` of categories the engine built. Each entry is `{ icon, items }`, where `icon` is the category's carousel sprite and `items` is a `List` of `LiveItem` in shelf order. ctx is `{ store, menu }`. Mutate the Lists in place, return a replacement `List`, or return `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the return of `create_store_stock()`, at the four sites listed below. |
| **Value** | The `List` of `{ icon, items }` categories. |
| **ctx** | `{ store, menu }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `store` - the `Store` id the stock was built for. `store_to_string(store)` gives its fiddle key, and `STORES[store]` is the parsed store entry, with any keys a mod's `stores.toml` added.
- `menu` - the `StoreMenu` under construction for the `StoreMenu` init fire, and `undefined` at the other three sites.

### The value

- The engine's checks run before this hook and not after it. The requirements pass, the `purchasable()` retain, and the empty-category pruning are already done when the value arrives, so nothing validates what a handler adds or changes.
- Validate your own additions. Every category you add or alter needs a real `icon` sprite asset and a non-empty `items` List of `LiveItem`, and the List you return must hold at least one category.
- The shelf shows at most 25 entries per category. Entries beyond that are silently invisible.
- The daily random seed is already restored when this hook runs.

### Where It Fires

| Site | `ctx.menu` | When |
| --- | --- | --- |
| `StoreMenu` init | The `StoreMenu` under construction. | Every store open. |
| Game setup | `undefined` | Once, before the first frame, for the Inn's `INN_STOCK`. |
| New day | `undefined` | After [game.new_day](game.new_day.md), for the Inn's `INN_STOCK`. |
| Blacksmith ledger | `undefined` | On room entry, for the tool display on the counter. |

Read the menu from `ctx.menu`. Only the init fire has one, and it is still under construction there, so `ANCHOR.get_menu(Menu.Store)` returns `undefined` at all four sites. Return `undefined` on the setup fire when your handler depends on config, a mod save, or file IO. That fire lands before the first frame, while `mmapi_io_is_ready()` is still false.

### Data First

Static and requirement-gated stock belongs in your mod's `stores.toml`, with no handler. A bare `[[<store>.categories]]` entry appends a tab, and `MOMIidentify` with `MOMIaction = "merge"` extends an existing one.

```toml
# fiddle/stores.toml
[[general.categories]]
	icon = "spr_ui_store_category_icon_seasonal"
	constant_stock = ["seed_turnip", "sapling_cherry"]

[[general.categories]]
	MOMIidentify = { icon = "spr_ui_store_category_icon_seeds" }
	MOMIaction = "merge"
	constant_stock = [
		{ item = "sapling_cherry", requirements = { repaired_general_store = true } },
	]
```

Every `requirements` key the game knows works on an entry, `random_stock` with `target_selections` gives a daily rotation, and `MOMIremove` drops vanilla entries. See [Pet Cosmetics](../PET_COSMETICS.md) for a complete `stores.toml` example. This hook is for stock the data cannot compute.

## Usage

```gml
// store.stock is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function my_mod_store_stock(_value, _ctx) {
    // _value is the List of { icon, items } categories the engine built.
    // _ctx is { store, menu }.
    //   .store - the Store id. STORES[_ctx.store] is its parsed entry.
    //   .menu  - the StoreMenu being built, or undefined when the build is
    //            for the Inn's daily stock or the blacksmith ledger.
    // Test the store first and never assume a menu:
    // if (_ctx.store != Store.General) return undefined;
    // e.g. one extra entry on the first tab:
    // var _id = try_string_to_item_id("sapling_cherry");
    // if (_id != undefined) _value.first().items.push(new LiveItem(_id));
    return undefined; // undefined = keep the (possibly mutated) List
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("store.stock", my_mod_store_stock);
```

## Interactions

- Handle a tap on an added entry through [store.item_added](store.item_added.md).
- Write state such as a sold count from [store.purchase](store.purchase.md), and read it here on the next open.
- Write state meant for the Inn's daily build from [game.new_day](game.new_day.md). It fires before that build.
- [ui.menu_opened](ui.menu_opened.md) fires after the menu's init, so the value returned here is already on the shelf by then.
- Use your mod's `stores.toml`, as [Pet Cosmetics](../PET_COSMETICS.md) shows, for stock the data can express, and this hook for stock it cannot.

## Engine Wiring

- Seam [`store_stock`](../seams/store_stock.md) dispatches from `gml/scripts/Stores.gml`, a whole-function wrap of `create_store_stock()` that filters its return value.
- Companion seam [`store_menu_ctx_pointer`](../seams/store_menu_ctx_pointer.md) provides no dispatch of its own. It holds the `StoreMenu` in a framework global for the span of the constructor's `init()` call, which is where `ctx.menu` comes from on the init fire.

## See Also

- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
- [store.purchase](store.purchase.md) - Know the moment a Buy press commits.
- [items.store_price](items.store_price.md) - Change the base price a store charges for an item.
- [Pet Cosmetics](../PET_COSMETICS.md) - Sell pet cosmetic sets from store stock in fiddle data.
