# Hook: crafting.pay_component_costs

Veto a recipe's material payment, craft for free.

`crafting.pay_component_costs` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `pay_component_costs(components, item_id, quantity, context)`, before a recipe's materials are deducted. ctx is `{ item_id, quantity, context }` (`context` is the crafting discipline). Return `false` to skip the payment (craft for free). `undefined` or `true` pays normally.

| | |
| --- | --- |
| **Fires** | At the top of `pay_component_costs()`, before a recipe's materials are deducted. |
| **ctx** | `{ item_id, quantity, context }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `item_id` - the id of the item being crafted.
- `quantity` - how many are being crafted.
- `context` - the crafting discipline.

## Usage

```gml
// crafting.pay_component_costs is a GUARD: return false to block it, undefined
// (or true) to allow. Guards fail OPEN - if your handler crashes, the action happens.
function free_forge_crafting_pay_component_costs(_ctx) {
    // _ctx is { item_id, quantity, context }.
    //   .item_id  - the id of the item being crafted.
    //   .quantity - how many are being crafted.
    //   .context  - the crafting discipline.
    // if (<your condition>) {
    //     return false; // veto - the materials are never deducted (craft for free)
    // }
    return undefined; // allow: pay normally
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("crafting.pay_component_costs", free_forge_crafting_pay_component_costs);
```

## Engine Wiring

- Seam [`crafting_pay_component_costs`](../seams/crafting_pay_component_costs.md) dispatches from `gml/scripts/GameplaySystems/Recipe.gml`, at the head of `pay_component_costs()`. On veto the engine runs `return;`. The materials are never deducted.

## See Also

- [crafting.max_crafts](crafting.max_crafts.md) - Take over how many of a recipe can be crafted.
- [items.infusion_generate](items.infusion_generate.md) - Stop a recipe from rolling infusions.
