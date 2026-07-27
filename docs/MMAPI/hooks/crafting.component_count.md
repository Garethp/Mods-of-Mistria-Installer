# Hook: crafting.component_count

Adjust one recipe component's effective cost, including making crafts instant via a zero duration.

`crafting.component_count` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires as `get_modified_component_count(component, context, item_id)` returns. That function is the single point every crafting cost read flows through. `maximum_crafts`' ceiling, `can_fulfill_component`, and `pay_component_costs` all call it, and the engine's own crafting perks (ingot discounts, the per-discipline time perks) apply their reductions inside it. The filtered value is the engine-computed count for ONE recipe component. ctx is `{ component, context, item_id }`. Return a replacement count, or `undefined` to keep the engine's value.

| | |
| --- | --- |
| **Fires** | As `get_modified_component_count` returns, on every crafting cost read. |
| **Value** | The engine-computed count for one recipe component. The perk-modified item/essence/gold/tag count, or for a `RecipeComponentType.Duration` component, the craft time in seconds (paid as a `CLOCK.jump`). |
| **ctx** | `{ component, context, item_id }`. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

Read `ctx.component.type` (a `RecipeComponentType`) to identify the component kind, and `ctx.context` (a `RecipeContext`) for the crafting discipline. `ctx.item_id` is the recipe's output item.

> [!IMPORTANT]
> Returning `0` for a **Duration** component is safe and makes the craft instant. `maximum_crafts` guards the zero denominator, and `pay_component_costs` skips the zero clock jump. Returning `0` for an **Item** component makes that ingredient free and ceiling-less. The [max_crafts_zero_component](../seams/max_crafts_zero_component.md) engine fix skips zero-cost components in the ceiling loop, and the MAX button stays bounded by the engine's own 999 quantity clamp.

> [!NOTE]
> This hook fires several times per menu interaction (once per component per cost read). Keep handlers cheap, and return `undefined` fast on components you do not touch.

## Usage

```gml
// crafting.component_count is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function my_mod_component_count(_value, _ctx) {
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (!is_struct(_ctx)) return undefined;
    var _comp = _ctx[$ "component"];
    if (!is_struct(_comp)) return undefined;
    // Instant crafts for one discipline: zero the DURATION component only.
    if (_comp[$ "type"] == RecipeComponentType.Duration
        && _ctx[$ "context"] == RecipeContext.Blacksmithing)
    {
        return 0;
    }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("crafting.component_count", my_mod_component_count);
```

## Engine Wiring

- Seam [`crafting_component_count`](../seams/crafting_component_count.md) wraps `get_modified_component_count` in `gml/scripts/GameplaySystems/Recipe.gml`, filtering its return value.

## See Also

- [crafting.max_crafts](crafting.max_crafts.md) - Replace the craft ceiling (and force craftability).
- [crafting.pay_component_costs](crafting.pay_component_costs.md) - Skip the material payment entirely.
- [items.consumed](items.consumed.md) - Know when items leave the inventory.
