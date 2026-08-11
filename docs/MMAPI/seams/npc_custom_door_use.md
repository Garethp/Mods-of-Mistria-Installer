# Engine Fix: npc_custom_door_use

Resolves a registered custom NPC id in the Mist `__use_door` command.

`npc_custom_door_use` is an **engine fix**, an anchored edit with no hook behind it. See [npc_custom_actor_resolution](npc_custom_actor_resolution.md) for the full rationale shared by all six of these edits, and [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Mist/Std.gml` |
| **Locator** | text anchor: `__use_door`'s resolve line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_door_use` |

## The Edit

`__use_door` resolves its `actor` parameter before routing it to the nearest door:

```gml
var obj = __mmapi_custom_npc_object_for(actor) ?? npc_id_to_gm_obj_id(string_to_npc_id(actor));
```

A registered custom id resolves via the registry; anything else falls through to the native chain unchanged.

## See Also

- [npc_custom_actor_resolution](npc_custom_actor_resolution.md) - The core Mist actor chokepoint, and the full rationale for these six targeted edits over a single call rewrite.
- [npc_custom_entry_transition](npc_custom_entry_transition.md) - The matching edit for the entry-transition command, same file.
