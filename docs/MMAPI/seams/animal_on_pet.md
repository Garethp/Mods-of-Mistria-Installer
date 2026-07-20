# Seam: animal_on_pet

Announces the moment the player pets a barn animal.

`animal_on_pet` is a **template seam** (`op = "emit"`). It feeds [animal.pet](../hooks/animal.pet.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/obj_player_animal.gml` |
| **Locator** | structural target: `on_pet`, at head |
| **Op** | `emit` |
| **Feeds** | [`animal.pet`](../hooks/animal.pet.md) |
| **ctx built** | `self` (the `obj_player_animal` instance) |
| **Marker** | `mmapi_animal_run_pet_on_pet` |

## The Edit

The generated emit lands at the head of `obj_player_animal`'s `on_pet()`, calling `mmapi_emit("animal.pet", self)` in the uniform try/catch shape. The head placement is deliberate: `on_pet`'s body never touches the FSM, so celebration state a handler sets up here (`ctx.create_animal_currency_dance(count, face_ari)`, which drops `count` AnimalCurrency "beads" at the animal, itself a no-op unless `Perk.CurrencyOfCare` is active) survives the rest of the body. Its twin [animal_put_down](animal_put_down.md) has to emit at the *end* of its function for exactly the mirrored reason.

ctx is the `obj_player_animal` instance, not the `Animal` data struct. The data is `ctx.me`. Because the emit precedes the animal's own `can_pet()` gate, it can fire even when the pet turns out to be a no-op. The two `obj_player_animal` sites share the one `animal.pet` hook, so a handler that wants once-per-day (or once-per-animal) behavior must latch itself.

## See Also

- [animal.pet](../hooks/animal.pet.md) - This is the hook this seam dispatches.
- [animal_put_down](animal_put_down.md) - This is the twin emit when a held animal is set back down, placed after the FSM change instead of before it.
- [animal_heart_points](animal_heart_points.md) - This is the filter on barn-animal heart-point deltas.
