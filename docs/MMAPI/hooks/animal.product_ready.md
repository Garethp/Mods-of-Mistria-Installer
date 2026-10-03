# Hook: animal.product_ready

Change whether an animal produces today.

`animal.product_ready` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `Stable.on_new_day()`'s production step, after `production_days` increments, for every barn/coop animal the [animal.production_gate](animal.production_gate.md) decision let through. The filtered value is the readiness comparison `animal.production_days >= production.days_to_produce`, computed with the incremented counter. ctx is `{ animal, stable, production, is_happy }`. Return a replacement boolean, or `undefined` to keep the current value.

A final `true` runs the complete vanilla drop block. The counter resets to 0, the production tier and perk bonuses compute, and the product drops land in the stable grid. A final `false` skips the drop block and leaves the incremented counter in place, so the counter keeps climbing and vanilla produces on the first day the filter stops vetoing.

| | |
| --- | --- |
| **Fires** | In `Stable.on_new_day()`'s production step, after `production_days` increments. |
| **Value** | The readiness comparison, `animal.production_days >= production.days_to_produce`. |
| **ctx** | `{ animal, stable, production, is_happy }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `animal` - the `Animal` struct producing. Read `animal.production_days` for the incremented counter.
- `stable` - the `Stable` processing its new day (`self` inside `on_new_day()`).
- `production` - the production struct for the animal's sex from its prototype. Read `production.days_to_produce`, `production.normal_product`, and `production.golden_product`.
- `is_happy` - the vanilla happiness result. It is only `false` here when an [animal.production_gate](animal.production_gate.md) handler forced the gate open.

## Usage

```gml
// animal.product_ready is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function eager_rancher_animal_product_ready(_value, _ctx) {
    // _value is the readiness comparison with the incremented counter:
    // production_days >= days_to_produce.
    // _ctx is { animal, stable, production, is_happy }.
    // e.g. every happy animal produces every day:
    // return true;
    return undefined; // undefined = keep the game's value
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("animal.product_ready", eager_rancher_animal_product_ready);
```

## Interactions

- The released drop block is entirely engine-owned. The production tier derives from heart level, the BarnyardBounty and Eggstra perk bonuses roll, `GAME_STATS.animal_production` records, and the CurrencyOfCareTwo bead drop and the bonus gather item spawn roll too. To change the drop lists themselves rather than the schedule, filter [animal.product_drops](animal.product_drops.md) instead.
- Heart level selects the production tier, so [animal.heart_points](animal.heart_points.md) indirectly decides how many products a delivery yields.
- `Stable.on_new_day()` also runs from the engine's own debug progression helper and test-suite callers, so the hook also fires when those callers skip days.

## Engine Wiring

- Seam [`animal_product_ready`](../seams/animal_product_ready.md) dispatches from `gml/scripts/GameplaySystems/Ranching/Stable.gml`, hoisting the readiness comparison into a filtered local between the counter increment and the drop block.

## See Also

- [animal.production_gate](animal.production_gate.md) - This hook is the outer half of the pair. It decides which animals run the production step at all.
- [animal.product_drops](animal.product_drops.md) - Change what an animal's production drops.
- [animal.heart_points](animal.heart_points.md) - Adjust the heart points an animal gains.
- [game.new_day](game.new_day.md) - The day-boundary event that fires after all stables have processed.
