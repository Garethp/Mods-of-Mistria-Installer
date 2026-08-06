# Mods of Mistria Installer

MOMI is a mod installer for Fields of Mistria. This fork is maintained for
Fields of Mistria 1.0.x and is based on the upstream
[Mods of Mistria Installer](https://github.com/Garethp/Mods-of-Mistria-Installer).

The current application version is 0.15.1. The 1.0.x game update changed the
game data and modding interfaces, so mods must explicitly support the current
game and MOMI version. Always keep a backup of your game installation and
check the mod author's compatibility notes.

MOMI rebuilds `assets.zip` from a verified pristine archive whenever mods are
installed. Changes are staged and validated before the live archive is
replaced. This allows disabled or removed mods to be removed on the next
successful rebuild and prevents a failed installation from leaving a partial
game archive.

## Installation
1. Create a mods folder to put your mods
   * On Windows, you'll want to create "mods" folder inside your Fields of Mistria folder, next to the `FieldsOfMistria.exe`.
   * On the Steam Deck (or other Linux distributions) you can also create a mods folder inside your Fields of Mistria folder,
     or you can create a `mistria-mods` folder in your home directory.
2. Download the installer from the [releases page](https://github.com/AcTePuKc/Mods-of-Mistria-Installer/releases).
3. Double-click the installer to run it. If it's not able to detect the Fields of Mistria location, try placing the
   installer in your Fields of Mistria folder, next to `Maybe.toml` file.
4. Click the "Install" button to install the mods. If you have mods in your mods folder, they should appear in a list.
5. After a game update, start MOMI again and reinstall the enabled mods. MOMI
   will detect an archive it does not recognize and will preserve it instead
   of overwriting it automatically.

For technical details about archive transactions and update detection, see
[`docs/ASSETS_STORE_TRANSACTION_DESIGN.md`](docs/ASSETS_STORE_TRANSACTION_DESIGN.md).

## Troubleshooting
**I installed a new cosmetic, but it's not appearing in existing games**  
First try downloading the latest version of MOMI and reinstalling. If you are still encountering the issue, open the page where you
downloaded the mod. Check whether the mod explicitly supports Fields of Mistria 1.0.x, then check the mod description to see if it
mentions a specific shop where you can buy the item or another way to obtain it. If nothing is mentioned, check the general store.
If you are still having issues, feel free to come to the Discord server to ask for help.

**The installer says it cannot find the Fields of Mistria location**
Try placing the installer in your Fields of Mistria folder, next to
`Maybe.toml`. This gives MOMI a direct location to inspect. Also verify that
the game installation is complete through Steam.

**The installer says it cannot find the mods folder**  
Make sure you have created a folder called "mods" in your Fields of Mistria folder, next to `Maybe.toml` file, or a folder
called `mistria-mods` in your home directory if you're on the Steam Deck/Linux.

**The installer says it didn't find any mods to install**  
Make sure you have mods in your mods folder and the mods are compatible with the installer. If you're unsure, check the
mod folder. It should contain a `manifest.json` or `manifest.toml` file. If neither is present, the mod is not compatible
and will have to be updated by the mod author.

MOMI can read supported mod packages such as `.zip` files. When extracting a
package manually, make sure the mod folder is directly inside the mods folder,
not inside another folder. For example, if you're installing
"Effe's Decor - Fridge", make sure that the folder structure is `mods -> Effe's Decor - Fridge -> manifest.json` and not
`mods -> Effe's Decor - Fridge -> Effe's Decor - Fridge -> manifest.json`. The duplicated folder is called a
"nested folder". It prevents MOMI from finding the manifest and detecting the mod.

**I've got a different problem**  
If your problem isn't listed above, please come and ask in the [Fields of Mistria Discord](https://discord.com/invite/j6bTZvMtsg).
There's a `#modding-game-help` channel that you'll see after you accept the rules and that's the best place to get help. To provide
more information, try downloading the `-cli` version of the installer, running that and then screenshotting the window
that popped up. The `-cli` version doesn't look as nice, but should provide more information about what's going wrong.

## Mod format
If you're a modder and want to make your mod compatible with this installer, feel free to refer to the [`mods`](./mods)
folder for example mods. Below is information for what you'll need. This is not a comprehensive list and more
documentation will be added in the future.

### `manifest.json`
```json
{
  "author": "Mod Author Name",
  "name": "Mod Name",
  "version": "1.0.0",
  "minInstallerVersion": "0.1.3",
  "manifestVersion": 1
}
```

Your mod will be given an ID that's based on the author and name fields, so make sure that those two combined are unique.
From version 0.1.3 onwards, the installer will check the `minInstallerVersion` field to make sure that the installer is
new enough to install the mod and tell the user if they're unable to install the mod without updating the installer.
The `manifestVersion` field isn't used yet, but will allow for backwards compatibility in future versions of the installer
if large changes are made to how mods are structured.

### `fiddle/`
JSON files in the `fiddle/` folder will get merged into the game's `__fiddle__.json` file. You can name the files however
you want and have multiple JSON values in one file or split them up into multiple files as you see fit.

### Localization

The Fields of Mistria 1.0.x release uses localization files inside `assets.zip`:

- `assets/localization/l10n.meta.toml`
- `assets/localization/translations/`
- `assets/localization/source_caches/`

The pre-1.0 `__localisation__.json` workflow described in older MOMI
documentation is no longer current. MOMI's supported mod format and
localization workflow are being updated for the 1.0.x format; do not copy
pre-1.0 localization files into a current game installation.

### `outfits/`
If you want to add new outfits to the game, you can do so by placing a JSON definition for the outfit in the `outfits/`
folder and the sprites should be in a `images/` folder. Files that are multiple frames of the same animation should be
in their own folder, separate from other sprites. Here's an example file:

```json
{
  "dolphin_tail": {
    "name": "Dolphin Tail",
    "description": "A dolphins tale.",
    "ui_slot": "back",
    "default_unlocked": true,
    "ui_sub_category": "capes",
    "lut_file": "images/lut.png",
    "ui_item": "images/ui.png",
    "outline_file": "images/outline.png",
    "animation_files": {
      "back_gear": "images/tail_animation"
    },
    "price_override": 0 // This is an optional field, but cannot be below 0
  }
}
```

For a full example, check out the [`dolphin_tail`](./mods/dolphin_tail) example.

### `objects/`
If you want to add new objects to the game, you can do so by placing a JSON definition for the object in the `objects/`
folder. The format of the file should be

```json
{
  "object_id": {
    "category": "category",
    "overwrites_other_mod": false,
    "data": {
      ...
    }
  }
}
```

The category of the object must be one of the following: "breakable", "building", "bush", "crop", "dig_site", "furniture",
"grass", "rock", "stump", "tree". Trying to add another value will result in MOMI having an error for the mod.

`overwrites_other_mod` is required for all objects but doesn't change how MOMI works. In a future version of MOMI, this
key will be used to automatically detect and warn users about conflicting mods. In that future update, if two mods add
objects with the same ID and they both have `overwrites_other_mod` set to `false`, the installer will warn the user
that these two mods probably conflict with each other.

Here's an example file:

```json
{
  "my_new_object": {
    "category": "furniture",
    "overwrites_other_mod": false,
    "data": {
      "size": [
        3,
        2
      ],
      "collision_grid": "2",
      "south": {
        "sprite": "spr_decor_dragon_statue_v1_spring_south",
        "offset": [
          12,
          8
        ]
      },
      "north": {
        "sprite": "spr_decor_dragon_statue_v1_spring_north",
        "offset": [
          12,
          8
        ]
      }
    }
  }
}
```

### `items/`
If you want to add new items to the game, you can do so by placing a JSON definition for the item in the `items/`
folder. The format of the file should be

```json
{
  "item_id": {
    "overwrites_other_mod": false,
      ...
  }
}
```

`overwrites_other_mod` is required for all items but doesn't change how MOMI works. In a future version of MOMI, this
key will be used to automatically detect and warn users about conflicting mods. In that future update, if two mods add
items with the same ID and they both have `overwrites_other_mod` set to `false`, the installer will warn the user
that these two mods probably conflict with each other.

An example of a full file is:

```json
{
  "wheedle_statue":  {
    "icon_sprite": "spr_ui_item_dragon_statue_replica_v1",
    "name": "Wheedle Statue",
    "overwrites_other_mod": false,
    "description": "items/furniture/mistrian_history_set/dragon_statue_replica_v1/description",
    "object": "dragon_statue_replica_v1",
    "tags": [
      "furniture",
      "mistrian_history_set",
      "misc_decor"
    ],
    "recipe_key": "dragon_statue_replica",
    "crafting_level_requirement": 20,
    "recipe": [
      {
        "count": 100,
        "item": "ore_stone"
      },
      {
        "count": 2,
        "item": "monster_core"
      },
      {
        "count": 2,
        "item": "monster_horn"
      },
      {
        "hours": 0,
        "minutes": 30
      }
    ],
    "value": { 
      "bin": "self.recipe*1.1"
    }
  }
}
```

### `stores/`
If you want to add categories to a store, or new items to a category in a store, you can do so by placing a JSON in the
`stores/` folder of your mod. In your JSON, you can either define a list of new categories to add to a store, a list
of new items to add to categories or both. Below is an example of the options that you can set:

```json
{
  "items": [
    {
      "item": "seed_turnip",
      "store": "general",
      "category": "modded_icon",
      "season": "spring"
    },
    {
      "item": { "cosmetic":  "froggy_hat" },
      "store": "general",
      "category": "modded_icon"
    },
    {
      "item": { "recipe_scroll":  "custom_recipe" },
      "store": "general",
      "category": "modded_icon"
    },
    {
      "item": { "cosmetic":  "froggy_hat" },
      "store": "louis",
      "category": "modded_icon",
      "random_stock": true
    }
  ],
  "categories": [
    {
      "store": "general",
      "icon_name": "modded_icon",
      "sprite": "images/icon_modded.png"
    },
    {
      "store": "louis",
      "icon_name": "modded_icon",
      "sprite": "images/icon_modded.png",
      "target_selections": 5
    }
  ]
}
```

If multiple mods add a category with the same `icon_name` to the same store, only one category by that name will be added.
The `category` key for an item should always match the `icon_name` of the category you want to add it to, whether it's a
category that's been modded in or a vanilla category. If you set the `season` key for an item, it will be added to the
seasonal stock for that category, otherwise it will be added to the year-round stock.

### `sprites/`
If you want to add new sprites to the game, you can do so by placing the sprites in the `images/` folder and then
creating a definition JSON file in the `sprites/` folder. Here's an example file:

```json
{
  "spr_furniture_stone_storage_chest_spring_v1_bounce": {
    "is_animated": true,
    "location": "images/v1/bounce",
    "origin_x": 16,
    "origin_y": 56,
    "margin_left": 3,
    "margin_right": 29,
    "margin_bottom": 39,
    "margin_top": 15
  }
}
```

For a full example, take a look at the [`Effe's Decor - Fridge`](./mods/Effe's%20Decor%20-%20Fridge) example. Files 
that are multiple frames of the same animation should be in their own folder, separate from other sprites. For reference,
the full list of sprite properties that you can control are:

```json
{
  "sprite_name": {
    "location": "imageLocation.png",
    "is_animated": true,
    "bounding_box_mode": 2,
    "origin_x": 0,
    "origin_y": 0,
    "margin_right": 0,
    "margin_left": 0,
    "margin_top": 0,
    "margin_bottom": 0,
    "is_player_sprite": true,
    "is_ui_sprite": true
  }
}
```

### `shadows/`
If you want to add shadow sprites to the game, create a JSON file in the `shadows/` folder with the following shape:

```json
{
  "shadow_sprite_name": {
    "regular_sprite_name": "spr_regular_sprite_name",
    "sprite": "images/sprite.png",
    "is_animated": false
  }
}
```

This will create new sprites in the `data.win` folder with the name `shadow_sprite_name` as well as an entry in
`animation/generated/shadow_manifest.json` which will look like:

```json
{
  "spr_regular_sprite_name": "shadow_sprite_name"
}
```

If you use this, please set `minInstallerVersion` in your `manifest.json` to no lower than `0.1.4`

### `gml/` (behavioural mods)
If you want your mod to change how the game behaves, put GML your files in a `gml/` folder, and set the `minInstallerVersion` in your `manifest.json` to no lower than `0.14.0`.

MOMI installs them into the game's scripts under a folder assigned to your mod, alongside the
MMAPI framework your code talks to.

> [!IMPORTANT]
> Mods using GML must be developed using MMAPI in order for MOMI to install them.

Before anything is written, MOMI checks that your mod's GML compiles and doesn't clash with the game or with other
mods. A mod that fails those checks is skipped completely.

For additional information, see [MMAPI](docs/MMAPI/MMAPI.md) documentation.

## Contributing Translations

If you're interested in contributing translations of MOMI into other languages, that would be super appreciated! Here's
some steps on how to go about doing that!

1. There's a main localisation file for English located at [`ModsOfMistriaInstallerLib/Lang/Resources.resx`](ModsOfMistriaInstallerLib/Lang/Resources.resx), go ahead and download it.
2. Go to [this page](https://catherinearnould.com/autres/resx/) to upload and edit the file.
3. If a string things like `{0}` and `{1}` in them, those are placeholders where MOMI will put other strings in at runtime, make sure that your translation keeps them.
4. When you're done editing, click "Save and download .resx"
5. Look up the "Language Culture Name" of your language from [this table](https://docwiki.embarcadero.com/RADStudio/Athens//en/Language_Culture_Names,_Codes,_and_ISO_Values). For example, Dutch in the Netherlands is "nl-NL".
6. Name your new file "Resources.culture-tag.resx", with the culture tag being lower-case. For example, a Netherlands Dutch translation file would be called "Resources.nl-nl.resx". If there's no regions of your language, or you're making a translation that should be fine for multiple regional variants, you can just use the first part of the culture tag. For example, "Resources.nl.resx" would apply to all versions of Dutch unless someone else contributes a more specific regional variant.
7. Contribute back your file through a PR in Github so that it can be reviewed and released

MOMI was built to be translatable from the start with a focus on being as accessible to as many people as possible! Any
contributions you can make in terms of translations is super appreciated! Thank you!
