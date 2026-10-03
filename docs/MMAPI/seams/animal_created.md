# Seam: animal_created

Announces every barn/coop animal instance the moment `spawn_animal()` creates it.

`animal_created` is a **template seam** (`op = "emit"`). It feeds [animal.created](../hooks/animal.created.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/AnimalUtils.gml` |
| **Locator** | structural target: `spawn_animal`, after the whole `animal.instance = instance_create_layer(...)` assignment |
| **Op** | `emit` |
| **Feeds** | [`animal.created`](../hooks/animal.created.md) |
| **ctx built** | `animal.instance` (the `obj_player_animal` instance) |
| **Marker** | `mmapi_animal_created` |

## The Edit

The generated emit lands at the end of `spawn_animal()`, calling `mmapi_emit("animal.created", animal.instance)` in the uniform try/catch shape. The token anchor is the function's whole assignment statement, so the emit sits after `animal.instance` is written, and that ordering is the point. `obj_player_animal`'s create event receives `me` through the construction struct but never writes `me.instance` itself. Only `spawn_animal()`'s assignment links the pair, so an emit at the end of the create event would hand handlers an animal whose `me.instance` is still unset. `spawn_animal()` is the only engine path that creates `obj_player_animal` instances, covering `Stable.on_room_start()`'s room population, the held-animal summon, and the test suite.

## See Also

- [animal.created](../hooks/animal.created.md) - This is the hook this seam dispatches.
- [npc_created](npc_created.md) - This is the sibling emit for the NPC spawn function.
- [pet_created](pet_created.md) - This is the sibling emit for the farm pet's spawn function.
- [animal_on_pet](animal_on_pet.md) - This is the emit when the player pets an animal this seam announced.
