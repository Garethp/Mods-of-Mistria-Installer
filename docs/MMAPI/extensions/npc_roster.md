# Extension Point: npc_roster

One registration per custom NPC.

`npc_roster` is an **extension point**. MOMI generates the engine-side identity for each registered NPC from the catalog's templates. Mods ship typed values, never engine text. See [Extension Points](../EXTENSIONS.md) for the concept, and [Your First Custom NPC](../CUSTOM_NPC.md) for a start-to-finish walkthrough.

## Registration

`momi/extensions/npc_roster/<name>.toml`, one field:

```toml
# momi/extensions/npc_roster/luna.toml
object = "obj_luna"
```

| Field | Type | Meaning |
| ----- | ---- | ------- |
| `object` | identifier | The GML object for this NPC, created by the mod's own `gml/` (`object_create(..., object_reserve("par_NPC"), ...)`) |

Companions the registration requires:

| Companion | Enforcement |
| --------- | ----------- |
| `fiddle/npcs/<name>.toml`, the NPC prototype (renamed to `<symbol>.toml` at install) | **Error**. The mod is skipped without it, because a member without data crashes the game during Setup |
| An `object_create` for `object` in the mod's `gml/` | Convention. MOMI warns at install when no `object_create` names the object |

### Short Names

The registration's file name is the NPC's **short name**, such as `luna` above. It is what you use to reference the NPC throughout your mod's *content* (non-GML) files: `fiddle/npcs/`, `t2/`, `animations/`, and `shapes/`. MOMI rewrites it to the full symbol at install.

### Symbols

Use the full **symbol** in `gml/` files for enum references, such as `NpcId.author_mymod_luna`. See [Symbols](../EXTENSIONS.md#symbols) for how it is derived.

The full symbol is also required in one content-side place the rewrite does not reach: **world-fact keys**. Enum-derived facts are named for the symbol, so a condition on them spells it out: `{ author_mymod_luna_is_traveling = true }`, never `luna_is_traveling`. The short form is an undeclared fact, and the boot error will not point here.

### Object Names

Your NPC's **object name** is neither the **short name** nor the **symbol**. You define it separately, in the GML file for your NPC:

```gml
object_create("obj_luna", object_reserve("par_NPC"), { sprite_index: spr_npc_mask })
```

The registration's `object` field tells MOMI which object to wire into the enum's switch cases, and MOMI generates a manifest macro, so `obj_luna` resolves in your GML to the object type, exactly like a vanilla object identifier.

> [!TIP]
> Object names share one global namespace, with no automatic prefixing. In a real mod, build your symbol into the name, such as `obj_author_mymod_luna`.

### The Prototype

Your NPC's prototype is `fiddle/npcs/<short name>.toml`. It is mandatory, the game crashes during Setup without it.

All 21 of these fields must be present in your prototype:

`aldarian_name, birthday, cycles, date_photo_offset, dateable, disliked_gift_tags, gossip, hated_gift, icon_sprite, job, journal_background_color, journal_portrait_offset, liked_gifts, loved_gifts, name, offsets, outfits, portraits, small_icon_sprite, small_outlined_icon_sprite, tags`

> [!IMPORTANT]
> Always include a `spring` outfit. The engine's default outfit selector requires one.

That is everything your mod supplies. MOMI generates the rest: the enum member, both id-to-object switch cases, the `object_manifest.gml` macro, and the baseline schedule entry.

## Shipping Your NPC's Art

Ship your sprites through the [named sprite](../EXTENSIONS.md#named-sprites) lane, at vanilla-shaped paths such as `animations/NPCs/<YourNpc>/...`, named with the **short name**.

Walk cycles require the `[asset_properties.offset]` sprite origin. Without it, the body and its collision box disagree by half a sprite.

The three `icon_sprite` fields are ordinary strings, and may point at any existing sprite. Declare a portrait in your prototype only if you ship its related sprite.

> [!NOTE]
> At runtime, sprites are looked up by **symbol**-derived names, in the form `spr_portrait_<symbol>_<outfit>_<emotion>` and `spr_npc_<symbol>_<outfit>_<cycle>_<direction>`.

## Schedules and Waypoints

A schedule destination is `"location/Waypoint Name"`. The waypoint is a **trellis point**, a named `obj_trellis_point_default` object defined in the room's TMX. Your schedule can only reference waypoints that already exist in the rooms.

> [!IMPORTANT]
> The first entry must be exactly 6:00am. It is the day-start anchor the engine snaps NPCs to. A first entry at any other time leaves the NPC on the basement fallback, invisible forever.

Later entries walk as real itineraries. The NPC is interactable mid-walk, and `on_arrival_actions = [{ animation = "action" }]` plays a cycle on arrival.

Check your waypoint for collisions before claiming it. Activities (NPCs roaming a zone) check occupancy and yield to whoever is standing there. A hard schedule `destination` does not, so two NPCs scheduled to the same point at the same time overlap on the same tile.

> [!TIP]
> Check the vanilla `t2/Schedules/**` for your waypoint across every season, weather, festival, and story-state variant. Points that exist in the TMX but appear in no schedule are safe picks.

## Dialogue

Ship banked-line conversations as `t2/Conversations/Bank/<YourNpc>/Banked Lines/<key>.c.toml`. MOMI installs them as new files, and the native conversation engine indexes them at boot. The binding is the `{ npc = "<short name>" }` condition, not the folder name.

> [!TIP]
> For authoring the conversations themselves, branching, prompts, cadence, and world facts, see the worked example in [Your First Custom NPC](../CUSTOM_NPC.md#the-dialogue).

## Custom World Facts

> [!IMPORTANT]
> Every fact must be declared. An undeclared fact in `requires` or `writes` stops the game from booting.

Declare your facts by shipping `t2/t2.meta.toml` with only your keys under `[asset_properties.initial_data]`. MOMI merges them into the vanilla registry without touching it:

```toml
[asset_properties.initial_data]
	mymod_met_the_stranger = false
```

Declared facts are written by conversation `writes`, and hard-gate anything the condition system reaches. They are readable and writable from GML via `T2R.read` and `T2R.write`.

> [!NOTE]
> Per-NPC facts (`<symbol>_zone`, `<symbol>_has_met`, `<symbol>_is_traveling`, and the rest) derive from the enum automatically. Your NPC gets them without declaring anything.

## Journal Visibility

Your NPC is visible in the relationships journal by default, as a faded row with a black silhouette and a `???` name until met. The `npc.is_unlocked` filter hook is the primary gate. Returning `false` hides the NPC entirely.

To gate on your own condition, register a filter handler using the standard [Quick Start](../QUICK_START.md) skeleton, with a top-level named handler:

```gml
function mymod_luna_unlocked(unlocked, npc_id) {
    if (npc_id != NpcId.author_mymod_luna) {
        return undefined;              // not ours, keep the current value
    }
    return NPCS[NpcId.Adeline].heart_level() >= 2;    // shown once Adeline is a friend; false hides the row entirely
}

// inside your registration latch, alongside your other registrations
mmapi_filter("npc.is_unlocked", mymod_luna_unlocked);
```

The filtered value is the unlocked boolean, and `ctx` is the `NpcId` ordinal. Return `undefined` to keep the current value.

> [!IMPORTANT]
> Remember to add `npc.is_unlocked` to your manifest's `requires_hooks`.

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

Hearts, gift history and the rest of the relationship restore when the mod is reinstalled.

> [!WARNING]
> Festival dates are the one exception. If your NPC joins a festival's `npc_date.participants` table, a save made between accepting a festival date and the festival itself will error on festival day after your mod is removed, because the participants entry is your mod's data and leaves with it. Weigh festival participation accordingly.

## Ships With

- [npc_load_missing_blob_guard](../seams/npc_load_missing_blob_guard.md) lets pre-existing saves load after a custom NPC is installed, and lets mod-era saves load after the NPC is removed. Hearts and gift history restore into the parked, inert vacancy.
- [npc_is_unlocked_vacancy](../seams/npc_is_unlocked_vacancy.md) feeds [npc.is_unlocked](../hooks/npc.is_unlocked.md), covering vacancy hiding and author-controlled gating.
- `obj_mmapi_npc_vacant` and its macro, in the mmapi payload.
