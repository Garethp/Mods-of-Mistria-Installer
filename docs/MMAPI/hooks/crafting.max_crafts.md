# Hook: crafting.max_crafts

Take over how many of a recipe can be crafted.

`crafting.max_crafts` is an **override** hook. Register a callback with `mmapi_override`. See [Hooks](../HOOKS.md) for how registration and dispatch work. This override is **exclusive**: the catalog expects at most one mod to override it, and a second registration warns, naming both mods.

## Contract

Fires at the top of `CraftingMenu.maximum_crafts(item)`, the ceiling on how many of a recipe can be crafted (`check_item_craftable` derives from it, so this also gates craftability). ctx is `{ menu, item }`. Read `ctx.menu.context` for the crafting discipline. Return a replacement count (e.g. a large number to allow crafting without materials), or `undefined` to defer to the normal material-based computation.

| | |
| --- | --- |
| **Fires** | At the top of `CraftingMenu.maximum_crafts(item)`, before the material-based computation. |
| **ctx** | `{ menu, item }` |
| **Kind contract** | The first callback to return a non-`undefined` value replaces the engine's behavior. Return `undefined` to defer. |

### The ctx struct

- `menu` - the `CraftingMenu`, whose `ctx.menu.context` is the crafting discipline.
- `item` - the recipe item whose craft ceiling is being computed.

## Usage

```gml
// crafting.max_crafts is an OVERRIDE: return a value to replace the game's
// whole answer; return undefined to let the game (or another mod) decide.
function endless_workbench_crafting_max_crafts(_ctx) {
    // _ctx is { menu, item }.
    //   .menu - the CraftingMenu; .menu.context is the crafting discipline.
    //   .item - the recipe item whose craft ceiling is being computed.
    return 999;
}

mmapi_override("crafting.max_crafts", endless_workbench_crafting_max_crafts);
```

## Engine Wiring

- Seam [`crafting_max_crafts`](../seams/crafting_max_crafts.md) dispatches from `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml`, at the head of `maximum_crafts(item)`. A non-`undefined` override return is returned immediately, skipping the material scan.

## See Also

- [crafting.pay_component_costs](crafting.pay_component_costs.md) - Veto a recipe's material payment.
- [items.infusion_generate](items.infusion_generate.md) - Stop a recipe from rolling infusions.
- [ui.item_node](ui.item_node.md) - Mutate the crafting menu's item nodes as they are populated.
