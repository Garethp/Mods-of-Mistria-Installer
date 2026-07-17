# Hook: items.infusion_generate

Stop a recipe from rolling infusions.

`items.infusion_generate` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Recipe.generate_infusions()`. ctx is the recipe struct. Return `false` to veto infusion generation (an empty list is returned). `undefined` or `true` allows.

| | |
| --- | --- |
| **Fires** | At the top of `Recipe.generate_infusions()`. |
| **ctx** | The recipe struct. |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx parameter

- ctx - the `Recipe` struct whose infusions are being generated (`self` inside `generate_infusions()`).

## Usage

```gml
// items.infusion_generate is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function pure_craft_items_infusion_generate(_ctx) {
    // _ctx is the Recipe struct whose infusions are being generated.
    // if (<your condition>) {
    //     return false; // veto - generate_infusions returns an empty list
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("items.infusion_generate", pure_craft_items_infusion_generate);
```

## Engine Wiring

- Seam [`items_infusion_generate`](../seams/items_infusion_generate.md) dispatches from `gml/scripts/GameplaySystems/Recipe.gml`, anchored just after `var infusions = List();` at the top of `generate_infusions()`. On veto the engine runs `return infusions;`, the freshly created empty list.

## See Also

- [crafting.pay_component_costs](crafting.pay_component_costs.md) - Veto a recipe's material payment.
- [crafting.max_crafts](crafting.max_crafts.md) - Take over how many of a recipe can be crafted.
