# Extension Point: npc_roster

One registration per custom NPC.

`npc_roster` is an **extension point**. MOMI generates the engine-side identity for each registered NPC from the catalog's templates. Mods ship typed values, never engine text. See [Extension Points](../EXTENSIONS.md) for the concept, the registration how-to, and journal visibility.

## Registration

`momi/extensions/npc_roster/<name>.toml`, one field:

| Field | Type | Meaning |
| ----- | ---- | ------- |
| `object` | identifier | The GML object for this NPC, created by the mod's own `gml/` (`object_create(..., object_reserve("par_NPC"), ...)`) |

Companions the registration requires:

| Companion | Enforcement |
| --------- | ----------- |
| `fiddle/npcs/<name>.toml`, the NPC prototype (renamed to `<symbol>.toml` at install) | **Error**. The mod is skipped without it, because a member without data crashes the game during Setup |
| An `object_create` for `object` in the mod's `gml/` | Convention (see [EXTENSIONS.md](../EXTENSIONS.md)) |

## Generated Sites

| Site | File | What a registrant gets |
| ---- | ---- | ------------------------ |
| `enum_member` | `gml/scripts/GameplaySystems/NPCs/NpcId.gml` | `<symbol> = <ordinal>,` before the `LEN` sentinel |
| `id_to_obj` | same file | `case NpcId.<symbol>: return <object>;` in `npc_id_to_gm_obj_id` |
| `obj_to_id` | same file | `case <object>: return NpcId.<symbol>;` in `gm_obj_id_to_npc_id` |
| `object_macro` | `gml/objects/object_manifest.gml` | `#macro <object> object("<object>")` appended |
| `basement_schedule` | `t2/Schedules/basement_schedule.s.toml` | `<symbol>."6:00am" = "aldaria/default"` appended, because the engine natively refuses to boot any member without a schedule |

## Vacancy

When a registered mod is uninstalled, its symbol keeps its ordinal. The vacancy renders: the enum member and schedule line stay, `id_to_obj` maps to the framework's `obj_mmapi_npc_vacant`, a minimal "Departed Villager" fiddle stub is generated, and the [npc_is_unlocked_vacancy](../seams/npc_is_unlocked_vacancy.md) seam keeps it out of the journal.

## Ships With

- [npc_load_missing_blob_guard](../seams/npc_load_missing_blob_guard.md) lets pre-existing saves load after a custom NPC is installed, and lets mod-era saves load after the NPC is removed. Hearts and gift history restore into the parked, inert vacancy.
- [npc_is_unlocked_vacancy](../seams/npc_is_unlocked_vacancy.md) feeds [npc.is_unlocked](../hooks/npc.is_unlocked.md), covering vacancy hiding and author-controlled gating.
- `obj_mmapi_npc_vacant` and its macro, in the mmapi payload.
