# Seam: npc_created

Announces every NPC instance the engine spawns, after it is fully initialized.

`npc_created` is a **template seam** (`op = "emit"`). It feeds [npc.created](../hooks/npc.created.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/NpcDatabase.gml` |
| **Locator** | structural target: `spawn_npc`, after `new_inst.initialize(npc);` |
| **Op** | `emit` |
| **Feeds** | [`npc.created`](../hooks/npc.created.md) |
| **ctx built** | `new_inst` (the `par_NPC` instance) |
| **Marker** | `mmapi_npc_created` |

## The Edit

The generated emit lands inside `spawn_npc()`, between the `initialize(npc)` call and the return, calling `mmapi_emit("npc.created", new_inst)` in the uniform try/catch shape. `spawn_npc()` is the engine's only NPC creation path outside cutscene staging and the test suite, and every population route (`npcs_on_room_start`, `npcs_on_step`'s walk-in spawn, the schedule time jump) funnels through it.

The spawn-site placement, rather than an emit at the end of `par_NPC`'s create event, is what the hook's contract rests on. `initialize(npc)` runs after the create event returns, and it is what attaches the `Npc` data struct as `me` and spawns the FSM, so an in-create emit would hand handlers an instance with neither. It would also fire before `obj_caldarus` and `obj_elsie` register their own create-event interactions, making mod interactions on those two NPCs land earlier than on the other 32 and take priority the other NPCs would not give them. After the emit the walk-in call site still runs `look_for_entry_transition()` on the returned instance, so a spawn from that route may enter a door transition right after handlers finish.

## See Also

- [npc.created](../hooks/npc.created.md) - This is the hook this seam dispatches.
- [pet_created](pet_created.md) - This is the sibling emit for the farm pet's spawn function.
- [animal_created](animal_created.md) - This is the sibling emit for the barn/coop animal spawn function.
