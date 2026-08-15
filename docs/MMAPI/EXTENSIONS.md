# Extension Points

[← MMAPI](MMAPI.md)

An **extension point** lets a mod add a new member to one of the engine's own `enums`. This is the core mechanism behind custom NPCs.

While a [seam](SEAMS.md) observes or filters behavior at a point in a function, an extension point grows compile-time identity. MOMI generates the enum member, the switch cases that map it, the manifest macro that binds its object, and the schedule entry.

> [!TIP]
> This page is the technical reference. For a start-to-finish walkthrough that builds a working custom villager, see [Your First Custom NPC](CUSTOM_NPC.md).

## Determining When You Need One

| You want to... | Use |
| -------------- | --- |
| React to or change behavior at a point in a game function. | A [hook](HOOKS.md), no extension point needed. |
| Add a custom **spell**, **perk**, **monster**, or **blueprint**. | Plain fiddle content. Include the `fiddle/` file in your mod, and the engine derives those entries from data at boot. See [Custom Spells](CUSTOM_SPELLS.md) and [Custom Perks](CUSTOM_PERKS.md). |
| Add entirely new functions or objects. | Your mod's own `gml/`. |
| Add a new member to a GML-declared `enum`, such as a custom NPC or a custom status effect. | An extension point. See this page for NPCs and [Custom Status Effects](CUSTOM_STATUS_EFFECTS.md) for status effects. |

> [!NOTE]
> The determination is ultimately decided by *what* declares the enum. If the enum is explicitly declared in shipped GML, MOMI can extend it.

## Registering a Custom NPC

A **registration** is a single TOML file. The file name determines your NPC's **short name**, such as `luna` below.

```toml
# momi/extensions/npc_roster/luna.toml
object = "obj_luna"
```

### Short Names

This **short name** is what you use to reference the NPC throughout your entire mod in its *content* (non-GML) files. Use it in `fiddle/npcs/`, `t2/`, `animations/`, and `shapes/` files. 

> [!IMPORTANT]
> Keep it short and simple. Use lowercase letters, numbers, and underscores only. Start with a letter, and use at most 41 characters.

### Symbols

During install, MOMI automatically rewrites the **short name** to a full **symbol**. A **symbol** is your NPC's permanent identity, which is used to avoid naming conflicts between mods.

A symbol is generated using the mod's information, with the following form: `<author>_<mod>_<short name>`. The author and mod pieces come from your manifest, lowercased with punctuation stripped. The short name piece is used verbatim.

Use the full **symbol** in `gml/` files for enum references, such as `NpcId.author_mymod_luna`.

The full symbol is also required in one content-side place the rewrite does not reach: **world-fact keys**. Enum-derived facts are named for the symbol, so a condition on them spells it out: `{ author_mymod_luna_is_traveling = true }`, never `luna_is_traveling`. The short form is an undeclared fact, and the boot error will not point here.

> [!IMPORTANT]
> The stripped `<author>` must start with a letter, and the whole symbol may not exceed 81 characters.

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

Ship your sprites as meta and png pairs at vanilla-shaped paths, such as `animations/NPCs/<YourNpc>/...`, named with the **short name**.

A meta must carry the fields the engine loads for its sprite kind, and no `id`. MOMI assigns ids at install. Walk cycles require the `[asset_properties.offset]` sprite origin. Without it, the body and its collision box disagree by half a sprite.

Every sprite also requires a partner shape file: a `poly_*` meta under `shapes/`, at the mirrored path, carrying the sprite's geometry. MOMI links each shape to its paired animation.

> [!TIP]
> The vanilla metas are the reference for which fields each sprite kind carries.

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
> For authoring the conversations themselves, branching, prompts, cadence, and world facts, see the worked example in [Your First Custom NPC](CUSTOM_NPC.md#the-dialogue).

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

To gate on your own condition, register a filter handler using the standard [Quick Start](QUICK_START.md) skeleton, with a top-level named handler:

```gml
function author_mymod_luna_unlocked(unlocked, npc_id) {
    if (npc_id != NpcId.author_mymod_luna) {
        return undefined;              // not ours, keep the current value
    }
    return author_mymod_progress();    // false hides the row entirely
}

// inside your registration latch, alongside your other registrations
mmapi_filter("npc.is_unlocked", author_mymod_luna_unlocked);
```

The filtered value is the unlocked boolean, and `ctx` is the `NpcId` ordinal. Return `undefined` to keep the current value.

> [!IMPORTANT]
> Remember to add `npc.is_unlocked` to your manifest's `requires_hooks`.

## Custom Status Effects

Status effects are the second extension point. A single registration file mints a `StatusEffectId` member. Your GML applies the effect, draws its HUD icon, and reacts when it ends.

See [Custom Status Effects](CUSTOM_STATUS_EFFECTS.md) for the guide, and [status_effect](extensions/status_effect.md) for the point's schema.

## Uninstalling

Removing your mod never breaks a player's save. MOMI maintains a **ledger** containing your NPC's identity and persists it invisibly, so every save reference keeps resolving. Hearts, gift history, etc. are restored when the mod is reinstalled. Saves made before your NPC existed load fine too.

> [!WARNING]
> Festival dates are the one exception. If your NPC joins a festival's `npc_date.participants` table, a save made between accepting a festival date and the festival itself will error on festival day after your mod is removed, because the participants entry is your mod's data and leaves with it. Weigh festival participation accordingly.

## Rules

- Reference your NPC as `NpcId.<symbol>` in your own GML, or look it up with `mmapi_ext_id("npc_roster", "<symbol>")` and back with `mmapi_ext_symbol(point, ordinal)`. Both come from the generated registry (`mmapi_ext.gml`).
- Persist the **symbol**, never the ordinal, in your modsave.
- Registrations require a `minInstallerVersion` of at least `0.16.0`.
