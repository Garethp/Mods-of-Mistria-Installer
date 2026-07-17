# Seam: items_infusion_generate

Puts a veto check in front of a recipe's infusion generation.

`items_infusion_generate` is a **template seam** (`op = "guard"`). It feeds [items.infusion_generate](../hooks/items.infusion_generate.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Locator** | structural target: `generate_infusions`, at after `var infusions = List();` |
| **Op** | `guard` |
| **Feeds** | [`items.infusion_generate`](../hooks/items.infusion_generate.md) |
| **ctx built** | `self` (the recipe struct) |
| **On veto** | `return infusions;` |
| **Marker** | `mmapi_items_run_infusion_generate_guards` |

## The Edit

The generated dispatch lands inside `Recipe.generate_infusions()`, immediately after the pristine `var infusions = List();` line, a deliberate placement, because the veto path leans on that local: when any guard returns `false`, the injected line runs `return infusions;`, handing callers the freshly created, still-empty list. A veto therefore yields a well-typed empty infusion list rather than `undefined`, and infusion generation is skipped cleanly.

The dispatch calls `mmapi_check_guards("items.infusion_generate", self)` in the uniform try/catch shape (catch var `__mmapi_items_infusion`). The ctx is the recipe struct itself. `undefined` or `true` lets generation proceed. The structural target is matched token-wise inside `generate_infusions`, immune to whitespace and comment drift, and with zero handlers the guard check early-outs on an empty registry.

## See Also

- [items.infusion_generate](../hooks/items.infusion_generate.md) - This is the hook this seam dispatches.
- [crafting_pay_component_costs](crafting_pay_component_costs.md) - This is the other `Recipe.gml` seam, which guards the material payment.
