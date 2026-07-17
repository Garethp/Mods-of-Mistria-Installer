# Seam: animal_put_down

Announces the moment a held animal is set back down.

`animal_put_down` is a **template seam** (`op = "emit"`). It feeds [animal.pet](../hooks/animal.pet.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/obj_player_animal.gml` |
| **Locator** | structural target: `put_down`, at after `obj_ari.par.held_item_render_callback = undefined;` (the function's last statement) |
| **Op** | `emit` |
| **Feeds** | [`animal.pet`](../hooks/animal.pet.md) |
| **ctx built** | `self` (the `obj_player_animal` instance) |
| **Marker** | `mmapi_animal_run_pet_put_down` |

## The Edit

The generated emit lands in `obj_player_animal`'s `put_down()`, after its last statement (`obj_ari.par.held_item_render_callback = undefined;`), calling `mmapi_emit("animal.pet", self)` in the uniform try/catch shape. The tail placement is deliberate and the mirror image of the [animal_on_pet](animal_on_pet.md) twin's: `put_down`'s body changes the FSM to Wander, so an emit at the head would have any celebration state a handler sets up clobbered by the body. Emitting after the last statement lets `ctx.create_animal_currency_dance(count, face_ari)` (which drops `count` AnimalCurrency "beads" at the animal, itself a no-op unless `Perk.CurrencyOfCare` is active) play out intact.

ctx is the `obj_player_animal` instance, not the `Animal` data struct. The data is `ctx.me`. The two `obj_player_animal` sites share the one `animal.pet` hook, so a handler that wants once-per-day (or once-per-animal) behavior must latch itself. Observation only: the put-down proceeds regardless.

## See Also

- [animal.pet](../hooks/animal.pet.md) - This is the hook this seam dispatches.
- [animal_on_pet](animal_on_pet.md) - This is the twin emit at the head of `on_pet()`, where a head emit's state is safe.
- [animal_heart_points](animal_heart_points.md) - This is the filter on barn-animal heart-point deltas.
