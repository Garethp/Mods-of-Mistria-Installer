# Seam: crafting_pay_component_costs

Puts a veto check in front of a recipe's material payment.

`crafting_pay_component_costs` is a **template seam** (`op = "guard"`). It feeds [crafting.pay_component_costs](../hooks/crafting.pay_component_costs.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | structural target: `pay_component_costs`, at head |
| **Op** | `guard` |
| **Feeds** | [`crafting.pay_component_costs`](../hooks/crafting.pay_component_costs.md) |
| **ctx built** | `{ item_id: item_id, quantity: quantity, context: context }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_crafting_run_pay_guards` |

## The Edit

The generated dispatch lands at the head of `pay_component_costs(components, item_id, quantity, context)`, before a recipe's materials are deducted. It calls `mmapi_check_guards("crafting.pay_component_costs", { item_id: item_id, quantity: quantity, context: context })` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return;`. The function exits without touching the player's materials, so the craft goes through for free. `undefined` or `true` lets the payment proceed normally.

ctx carries the crafted item's id, the quantity, and `context`, the crafting discipline. The `components` list itself is not in the ctx. The locator is a structural target (function + head), matched token-wise inside the named function, so it is immune to whitespace and comment drift. With zero handlers the guard check early-outs on an empty registry, leaving pristine behavior.

## See Also

- [crafting.pay_component_costs](../hooks/crafting.pay_component_costs.md) - This is the hook this seam dispatches.
- [crafting_max_crafts](crafting_max_crafts.md) - This seam overrides the craft-count ceiling upstream of payment.
- [items_infusion_generate](items_infusion_generate.md) - This is the other seam in `Recipe.gml`, guarding infusion generation.
