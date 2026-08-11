# Engine Fix: npc_custom_entry_transition

Resolves a registered custom NPC id in the Mist `look_for_entry_transition` command.

`npc_custom_entry_transition` is an **engine fix**, an anchored edit with no hook behind it. See [npc_custom_actor_resolution](npc_custom_actor_resolution.md) for the full rationale shared by all six of these edits, and [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Mist/Std.gml` |
| **Locator** | text anchor: `look_for_entry_transition`'s resolve line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_entry_transition` |

## The Edit

`look_for_entry_transition` resolves its `name` parameter before calling the object's own `look_for_entry_transition()`:

```gml
var obj = __mmapi_custom_npc_object_for(name) ?? npc_id_to_gm_obj_id(string_to_npc_id(name));
```

A registered custom id resolves via the registry; anything else falls through to the native chain unchanged.

## See Also

- [npc_custom_actor_resolution](npc_custom_actor_resolution.md) - The core Mist actor chokepoint, and the full rationale for these six targeted edits over a single call rewrite.
- [npc_custom_door_use](npc_custom_door_use.md) - The matching edit for the door-use command, same file.
