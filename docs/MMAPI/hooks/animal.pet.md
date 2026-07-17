# Hook: animal.pet

Know when the player pets or puts down an animal.

`animal.pet` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when the player pets a barn/coop animal (top of `obj_player_animal` `on_pet()`) or puts a held animal down (end of `put_down()`). ctx is the `obj_player_animal` instance, not the `Animal` data struct: its Animal data is `ctx.me`, and `ctx.create_animal_currency_dance(count, face_ari)` plays the celebration that drops `count` AnimalCurrency "beads" at this animal (itself a no-op unless `Perk.CurrencyOfCare` is active). This hook is observation only.

The two call sites share one hook, so a handler that wants once-per-day (or once-per-animal) behavior must latch itself. The `on_pet` site fires before the animal's own `can_pet()` gate, so it can fire even when the pet turns out to be a no-op.

| | |
| --- | --- |
| **Fires** | At the top of `on_pet()` and at the end of `put_down()` in `obj_player_animal`. |
| **ctx** | The `obj_player_animal` instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

ctx is the `obj_player_animal` world instance, not the `Animal` data struct. Its notable members:

- `me` - the animal's `Animal` data struct.
- `create_animal_currency_dance(count, face_ari)` - plays the celebration that drops `count` AnimalCurrency "beads" at this animal. Itself a no-op unless `Perk.CurrencyOfCare` is active.

## Usage

```gml
// animal.pet is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function pet_parade_animal_pet(_ctx) {
    // _ctx is the obj_player_animal instance (not the Animal data struct).
    //   .me - the animal's Animal data struct.
    //   .create_animal_currency_dance(count, face_ari) - plays the celebration
    //     that drops count AnimalCurrency "beads" at this animal (a no-op
    //     unless Perk.CurrencyOfCare is active).
    // your code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("animal.pet", pet_parade_animal_pet);
```

## Engine Wiring

- Seam [`animal_on_pet`](../seams/animal_on_pet.md) dispatches from `gml/objects/obj_player_animal.gml`, at the head of `on_pet()`. The body never touches the FSM, so a currency-dance celebration set up here survives the rest of the body. This site fires before `can_pet()`.
- Seam [`animal_put_down`](../seams/animal_put_down.md) dispatches from the same file, after `put_down()`'s last statement. A head emit's celebration state would be clobbered, because `put_down`'s body changes the FSM to Wander.

## See Also

- [animal.heart_points](animal.heart_points.md) - Adjust the heart points a barn animal gains.
