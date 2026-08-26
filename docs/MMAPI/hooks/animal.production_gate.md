# Hook: animal.production_gate

Change which animals are eligible to produce each day.

`animal.production_gate` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `Stable.on_new_day()`, once per barn/coop animal in the stall list, babies and unhappy animals included, after the day's happiness result is settled and the CloseBond heart bonuses have applied. The filtered value is the vanilla production gate boolean, `!animal.is_baby() && is_happy`. ctx is `{ animal, stable, production, is_happy }`. Return a replacement boolean, or `undefined` to keep the current value.

A final `true` runs the vanilla production step, where `production_days` increments and the [animal.product_ready](animal.product_ready.md) decision follows. A final `false` skips the step entirely, so no production progress accrues that day. Forcing the gate open for a baby runs the same production struct vanilla uses for the species' adults, so it works mechanically, but it is cheat territory by design.

| | |
| --- | --- |
| **Fires** | In `Stable.on_new_day()`, once per barn/coop animal, after the happiness result and heart bonuses. |
| **Value** | The vanilla production gate boolean, `!animal.is_baby() && is_happy`. |
| **ctx** | `{ animal, stable, production, is_happy }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `animal` - the `Animal` struct the production step is for.
- `stable` - the `Stable` processing its new day (`self` inside `on_new_day()`).
- `production` - the production struct for the animal's sex from its prototype. Read `production.days_to_produce`, `production.normal_product`, and `production.golden_product`.
- `is_happy` - the vanilla happiness result: not left outside, fed, and petted.

## Usage

```gml
// animal.production_gate is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function eager_rancher_animal_production_gate(_value, _ctx) {
    // _value is the vanilla gate: !is_baby && is_happy.
    // _ctx is { animal, stable, production, is_happy }.
    // e.g. let unhappy adults keep accruing production progress:
    // if (!_ctx.animal.is_baby()) return true;
    return undefined; // undefined = keep the game's value
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("animal.production_gate", eager_rancher_animal_production_gate);
```

## Interactions

- The CloseBond and CloseBondTwo heart bonuses read the raw `is_happy` upstream of this seam. Forcing the gate open produces drops without granting the heart bonuses a happy day earns.
- Stables process inside the engine's new-day work, before [game.new_day](game.new_day.md) fires. A `game.new_day` handler already sees this step's results, and the end-of-day autosave captures them.
- `Stable.on_new_day()` also runs from the engine's own debug progression helper and test-suite callers, so the hook also fires when those callers skip days. A skipped day is still a day, matching the `game.new_day` stance.

## Engine Wiring

- Seam [`animal_production_gate`](../seams/animal_production_gate.md) dispatches from `gml/scripts/GameplaySystems/Ranching/Stable.gml`, hoisting the gate condition into a filtered local ahead of the production step.

## See Also

- [animal.product_ready](animal.product_ready.md) - This hook is the inner half of the pair. It decides whether the accrued counter delivers today.
- [animal.pet](animal.pet.md) - Petting feeds the happiness result this gate tests.
- [animal.heart_points](animal.heart_points.md) - Adjust the heart points an animal gains.
