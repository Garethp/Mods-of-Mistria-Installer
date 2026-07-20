# Seam: crafting_max_crafts

Puts an override in front of the craft-count ceiling before the engine computes it.

`crafting_max_crafts` is a **text seam** (override-shaped: it dispatches `mmapi_run_override`). It feeds [crafting.max_crafts](../hooks/crafting.max_crafts.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | text anchor at the head of `CraftingMenu.maximum_crafts(item)` |
| **Op** | text (override dispatch) |
| **Feeds** | [`crafting.max_crafts`](../hooks/crafting.max_crafts.md) |
| **ctx built** | `{ menu: self, item: item }` |
| **Marker** | `mmapi_crafting_max_crafts_override` |

## The Edit

Two injected lines land at the head of `maximum_crafts(item)`, before the pristine `var maximum = I32_MAX;`:

```gml
var __mmapi_max_crafts = mmapi_run_override("crafting.max_crafts", { menu: self, item: item }); // mmapi_crafting_max_crafts_override
if (__mmapi_max_crafts != undefined) { return __mmapi_max_crafts; }
```

A non-`undefined` override return short-circuits the whole function: the engine's material-based computation never runs and the override's value becomes `maximum_crafts`' answer. Because `check_item_craftable` derives from `maximum_crafts`, the override also gates craftability. Returning a large number allows crafting without materials. When every handler defers (`undefined`), execution falls straight through into the pristine body. ctx carries the menu (read `ctx.menu.context` for the crafting discipline) and the item being quoted. The hook is an exclusive override: the catalog expects at most one mod to claim it.

## See Also

- [crafting.max_crafts](../hooks/crafting.max_crafts.md) - This is the hook that this seam dispatches.
- [crafting_pay_component_costs](crafting_pay_component_costs.md) - This seam is the guard on the material payment itself.
- [ui_item_node_crafting_menu](ui_item_node_crafting_menu.md) - This is the other `CraftingMenu.gml` seam, and it operates on the menu's item grid.
