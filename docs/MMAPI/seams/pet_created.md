# Seam: pet_created

Announces the farm pet's instance the moment `spawn_pet()` creates it.

`pet_created` is a **text seam**. It feeds [pet.created](../hooks/pet.created.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Pet.gml` |
| **Locator** | text anchor on the whole `spawn_pet()` definition |
| **Feeds** | [`pet.created`](../hooks/pet.created.md) |
| **ctx built** | the captured `obj_pet` instance |
| **Marker** | `mmapi_pet_created` |

## The Edit

Pristine `spawn_pet()` is a single `instance_create_layer` call whose return value is discarded, so the template ops cannot say this edit. The text seam rewrites the body to capture the created instance in a local, then emits `mmapi_emit("pet.created", __mmapi_pet_inst)` in the uniform try/catch shape.

By emit time the pet's create event has fully run. `me` is the `PET` global, `me.instance` points back at the instance, the FSM is spawned, and the vanilla interactions (feed, pick up, check, pet) are registered. Every engine pet spawn routes through `spawn_pet()`, the Mist cutscene actor spawn included, so the one seam covers room population, adoption, the held-animal summon, and cutscene staging alike. Handlers can tell the cutscene case apart by `ctx.override_pet_initialization == PetInitialization.Cutscene`.

## See Also

- [pet.created](../hooks/pet.created.md) - This is the hook this seam dispatches.
- [npc_created](npc_created.md) - This is the sibling emit for the NPC spawn function.
- [animal_created](animal_created.md) - This is the sibling emit for the barn/coop animal spawn function.
