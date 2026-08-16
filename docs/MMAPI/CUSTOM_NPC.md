# Your First Custom NPC

[← MMAPI](MMAPI.md)

The best way to understand [extension points](EXTENSIONS.md) is to build a small custom NPC end to end. This includes:

- A registration file that gives the NPC an identity in the engine's roster.
- A GML object file the engine spawns for them.
- A prototype data file, cloned from a vanilla villager.
- The villager's sprites, cloned the same way.
- A daily schedule and a line of dialogue.

This page explains building `luna_example`, a mod that adds one villager named Luna. She stands in town, walks her schedule, appears in the journal, and can be talked to.

## How It Fits Together

A custom NPC is a division of labor between MOMI and your mod:

| MOMI generates | Your mod supplies |
| -------------- | ----------------- |
| The `NpcId` enum member and its permanent ordinal. | The registration file that asks for it. |
| The id-to-object mappings and the object macro. | The GML object, created in your own `gml/`. |
| The baseline fallback schedule. | The real schedule, the prototype data, the art, the dialogue. |

The registration wires up identity. Everything the player actually sees comes from ordinary content files in your mod, installed through the same pipelines every content mod uses.

## Starting From a Vanilla Villager

This tutorial does not have you write an NPC prototype from scratch. The engine reads dozens of fields from an NPC's data file, optional fields included, and a missing one tends to crash or prevent the game from loading.

To get started copy a vanilla villager's prototype and change only identity:

1. Pick a base villager. This tutorial uses Merri.
2. Copy her data file and her sprites out of `assets.zip`.
3. Rename every copied file and every identity field to your NPC.
4. Leave every other field exactly as it was.

The copies are large. As of this article's writing, Merri ships 99 sprites, each a `.png` with a `.meta.toml` beside it, plus 99 collision shape files.

> [!IMPORTANT]
> Copy from a pristine `assets.zip`. If you have mods installed, MOMI keeps the untouched original next to it as `assets.bak.zip`. Copy from that one.

## Naming

Pick one **short name** for your NPC and use it everywhere in your files. This tutorial uses `luna`.

The **registration** file is `luna.toml`, the **prototype** is `luna.toml`, the **sprites** are named with `luna`, the **schedule keys** are `luna`, and the **dialogue** points at `luna`. This is the [One Name Everywhere](MOD_ANATOMY.md#one-name-everywhere) pattern applied to your mod's content.

Behind the scenes the engine needs a longer, globally unique name so two mods can each add a villager named `luna` without clashing. MOMI automatically builds that name from your mod's `id` (see [The Install Namespace](MANIFEST.md#the-install-namespace)) and rewrites your files to use it at install time. You type it yourself in one place, world-fact keys, covered in [Symbols](EXTENSIONS.md#symbols).

If you are curious, for author `you` and mod `luna_example` the registration `luna.toml` becomes the symbol `you_luna_example_luna`, and that is what appears in the enum member, the installed file names, and the save data. The rest of this tutorial refers to the **short name** as `luna`, just like your content files should.

## The Folder

```text
luna_example/
├─ manifest.json
├─ momi/
│  ├─ extensions/
│  │  ├─ npc_roster/
│  │  │  ├─ luna.toml
├─ gml/
│  ├─ Luna.gml
├─ fiddle/
│  ├─ npcs/
│  │  ├─ luna.toml
├─ animations/
│  ├─ ... (the 99 renamed sprite pairs)
├─ shapes/
│  ├─ ... (the 99 renamed shape files)
├─ t2/
│  ├─ Schedules/
│  │  ├─ Luna Schedules/
│  │  │  ├─ luna_spring.s.toml
│  │  │  ├─ luna_summer.s.toml
│  │  │  ├─ luna_fall.s.toml
│  │  │  ├─ luna_winter.s.toml
│  ├─ Conversations/
│  │  ├─ Bank/
│  │  │  ├─ Luna/
│  │  │  │  ├─ Banked Lines/
│  │  │  │  │  ├─ luna_hello.c.toml
```

The `animations/` and `shapes/` trees mirror where the copied files lived inside `assets.zip`. Keep the same subfolders the originals had.

## The Manifest

```json
{
    "name": "luna_example",
    "author": "you",
    "version": "1.0.0",
    "description": "Adds Luna, a custom villager.",
    "minInstallerVersion": "0.16.0",
    "manifestVersion": 1,
    "requires_hooks": []
}
```

A plain villager needs no hooks. The `requires_hooks` list stays empty unless your NPC's GML registers for one.

## The Registration

`momi/extensions/npc_roster/luna.toml` is the whole registration:

```toml
object = "obj_luna"
```

A registration is data. It names the GML object that represents your NPC in the world, and nothing else. MOMI validates it before installing anything, and a registration problem skips the whole mod the same way a GML problem does. See [Registering a Custom NPC](EXTENSIONS.md#registering-a-custom-npc) for the full rules.

## The Object

`gml/Luna.gml` creates the object the registration named. It is a copy of what the engine's own villager objects look like:

```gml
object_create(
    "obj_luna",
    object_reserve("par_NPC"),
    {
        sprite_index: spr_npc_mask,
    }
);
```

That is the whole file. A pure-content NPC has no runtime behavior, so there is no boot skeleton, no `mmapi_mod_declare`, and nothing to register. The engine's own villager objects are the same shape. If you later give Luna behavior through a hook, that code takes the full [boot file skeleton](MOD_ANATOMY.md#the-boot-file-skeleton) and the hook joins `requires_hooks` in the manifest.

> [!IMPORTANT]
> The parent must be `par_NPC`, and `spr_npc_mask` is the shared collision mask every villager uses. MOMI warns at install when a registration names an object that never appears in an `object_create` call in your mod's own GML.

The **object name** is the one name the content rewrite leaves alone, because it lives in your GML rather than your content files. Write the same name in the registration and keep it distinct from other mods. This tutorial uses `obj_luna`.

## The Prototype

`fiddle/npcs/luna.toml` is Merri's `fiddle/npcs/merri.toml`, copied entirely, with the identity fields changed:

- `name` becomes `"Luna"`.
- `aldarian_name` becomes her name in the Aldarian script.
- `birthday`, `job`, and the other flavor fields become whatever you like.
- Every `merri` in a sprite or portrait name becomes `luna`.

Leave the rest alone. The structural tables in this file tell the engine which sprites, portraits, and outfit sets exist for this NPC.

Your display `name` stays `"Luna"` with a capital letter, and the rewrite never touches it, because it matches on the lowercase `luna` you use for internal names.

> [!WARNING]
> Keep `"spring"` in the `outfits` list. The engine's default outfit selector falls back to the spring outfit for every season, so an NPC without one resolves a wardrobe key that does not exist. MOMI warns at install when the list omits it.

## The Art

Every sprite whose file name contains `merri` gets copied and renamed to contain `luna` instead. That covers the walk cycles, the portraits, and the journal icons, in their original subfolders under `animations/` and `shapes/`.

Two edits apply to every copied `.meta.toml`:

1. Delete the `id` line. MOMI assigns each installed asset a fresh `id`.
2. In the shape files, also delete the `required_assets` line. MOMI relinks each shape to its renamed sprite at install.

Change nothing else in the meta files. Fields like `offset` carry the sprite's origin, and the engine reads them wherever they are present.

At install, MOMI rewrites the `luna` in each file name to the full symbol, so the installed sprites match the names the prototype references. The engine registers a named sprite for every `.meta.toml` and `.png` pair installed under `animations/`, and your art then resolves everywhere the prototype points at it.

## The Schedule

MOMI generates one baseline schedule entry for every registered NPC, because the engine refuses to boot an NPC that has no schedule at all. That baseline parks them in `Aldaria`. To put Luna in the actual game world, give her a real schedule.

`t2/Schedules/Luna Schedules/luna_spring.s.toml`:

```toml
requires = [
	{ season = "spring" }
]

[luna."6:00am"]
destination = "town/Other Town Center Flower Beds"

[luna."3:00pm"]
destination = "town/Near South Stairs"
```

The `luna` table key is the NPC **short name**. MOMI rewrites it to the full symbol at install, the same as everywhere else.

Copy the file for the other three seasons and change the `season` value in each. Two rules matter:

- The first entry must be exactly `6:00am`. That entry is the day-start anchor the engine snaps NPCs to when the day begins. Later entries are walked as real itineraries.
- A destination is `"location/Trellis Point Name"`, and the point must already exist in that room. Check the vanilla schedules under `t2/Schedules/` before claiming a waypoint, because two NPCs scheduled to the same point at the same time will stand on the same tile.

See [Schedules and Waypoints](EXTENSIONS.md#schedules-and-waypoints) for the full behavior.

## The Dialogue

Without any dialogue files, Luna already talks. Villagers inherit the game's generic line pools, so gifts and idle chatter work from the start. A banked line makes her speak by default.

`t2/Conversations/Bank/Luna/Banked Lines/luna_hello.c.toml`:

```toml
[luna_hello]
refresh = "2m"
requires = [{ npc = "luna" }]
local = "Oh, hello. I don't think I've seen you around before."
```

The `npc = "luna"` condition ties the line to your NPC, and MOMI rewrites that value to the full **symbol** at install. The `[luna_hello]` key is your own name for the line and stays as written.

From here, the whole conversation-tree game vocabulary is available for you to design custom conversations.

## Sending Mail

Luna can send the player letters. Letters are fiddle data, so your mod ships them in `fiddle/letters.toml`:

```toml
[luna_first_letter]
	npc = "luna"
	subject_line = "Come Stargaze With Me"
	local = """Dear [Ari],

The sky over the Eastern Road is supposed to be clear tonight. Come find me if you'd like to watch the stars for a while."""
	requirements = { reached_date = { season = "spring", day = 22 } }
```

The `npc` value is the sender, and MOMI rewrites the **short name** to the full **symbol** at install, the same as everywhere else. The letter arrives on the first day its `requirements` pass, once, unless you set `can_repeat = true`. The `[luna_first_letter]` key is your own name for the letter and stays as written.

> [!NOTE]
> The rewrite covers your own mod's files only. A different mod writing a letter from your custom NPC must spell the full symbol in its `npc` value.

## Install and Run

Install with MOMI like any other mod. The install log shows the registration being collected and the prototype, schedules, and art being installed. A skipped mod names its reason, most often a missing companion file or a GML problem. See [Troubleshooting](TROUBLESHOOTING.md).

In game, load a save and check three things:

1. The villager journal shows a new silhouette at the end of the list. Unmet villagers stay silhouetted until you find them.
2. Luna is standing at her 6:00am waypoint. If you loaded mid-day, she appears there at the next day start, since the schedule snap happens when the day begins.
3. Talking to her plays her hello line, and gifting her works through the generic interactions.

Saves made before Luna existed load fine. MOMI ships a load guard that constructs missing NPCs fresh, with zero hearts and the NPC never met.

## Uninstalling

Removing the mod does not free Luna's ordinal. The **symbol** keeps it forever, and MOMI generates an inert placeholder in her place, because existing saves can reference her by name in places a cleanup may not find.

The placeholder is invisible in normal play. There's no journal row, no world or festival presence, no mail, and ongoing status events stay parked rather than manifesting.

Reinstalling the mod brings her back with hearts, gift history, and other status intact.

## Next Steps

- Read [Extension Points](EXTENSIONS.md) for everything this page glossed over: journal visibility, custom world facts, art rules, and the save semantics in full.
- The `status_effect` extension point follows the same registration pattern with far less bookkeeping: one registration file, one `register` call from your GML, and a HUD icon hook. See [Custom Status Effects](CUSTOM_STATUS_EFFECTS.md).
- Custom spells and perks need no extension point at all. See [Custom Spells](CUSTOM_SPELLS.md) and [Custom Perks](CUSTOM_PERKS.md).
