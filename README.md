# Mods of Mistria Installer

This is the in-progress installer for Fields of Mistria mods. As it's currently very early in development, please keep
in mind that it may have many bugs and may not work on all systems. Similarly, it won't support all mods that have been
release up until now, each mod will need to be updated to work with this installer, which many modders are already doing.

## Installation
1. Create a mods folder to put your mods
   * On Windows, you'll want to create "mods" folder inside your Fields of Mistria folder, next to the `data.win` file
   * On the Steam Deck (or other Linux distros) you can also create a mods folder inside your Fields of Mistria folder, 
     or you can create a `mistria-mods` folder in your home directory.
2. Download the installer from the [releases page](https://github.com/Garethp/Mods-of-Mistria-Installer/releases).
3. Double-click the installer to run it. If it's not able to detect the Fields of Mistria location, try placing the
   installer in your Fields of Mistria folder, next to `data.win`.
4. Click the "Install" button to install the mods. If you have mods in your mods folder, they should appear in a list.
5. Next time the game updates, run the installer again to re-install your mods

## Troubleshooting
**I installed a new cosmetic, but it's not appearing in existing games**
First try downloading the latest version of MOMI and re-installing. Many cosmetic mods will add that cosmetic will also
add that cosmetic to be sold in a shop however installers older than `0.1.3` aren't able to add items to the shop. If
updating the MOMI installer doesn't work, check the description of the mod to see if it talks about how to acquire the
cosmetic. For example, some mods may choose to only add the item to the Saturday Markets. If you're still having issues,
feel free to come to the Discord Server to ask for help.

**The installer says it cannot find the Fields of Mistria Location**
Try placing the installer in your Fields of Mistria folder, next to `data.win`, this should allow the installer to find
the game.

**The installer says it cannot find the mods folder**
Make sure you have created a folder called "mods" in your Fields of Mistria folder, next to `data.win`, or a folder
called `mistria-mods` in your home directory if you're on the Steam Deck/Linux.

**The installer says it didn't find any mods to install**
Make sure you have mods in your mods folder and the mods are compatible with the installer. If you're unsure, check the
mod folder, inside it there should be a `manifest.json` file. If there's not, the mod is not compatible and will have to
be updated by the mod author.

The installer cannot install mods that are `.zip` files, so make sure the mods are extracted. When extracting, make sure
that the mod folder is directly inside the mods folder, not inside another folder. For example, if you're installing
"Effe's Decor - Fridge", make sure that the folder structure is `mods -> Effe's Decor - Fridge -> manifest.json` and not
`mods -> Effe's Decor - Fridge -> Effe's Decor - Fridge -> manifest.json`.

**I've got a different problem**
If your problem isn't listed above, please come and ask in the [Fields of Mistria Discord](https://discord.com/invite/j6bTZvMtsg).
There's a `#modding` channel that you'll see after you accept the rules and that's the best place to get help. To provide
more information, try downloading the `-cli` version of the installer, running that and then screenshotting the window
that popped up. The `-cli` version doesn't look as nice, but should provide more information about what's going wrong.

## Mod Format
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

### `localisation/`
JSON files in the `localisation/` folder will get merged into the game's `__localisation__.json` file. You can name them
however you want, but they should end in `.eng.json` or `.jpn.json` (or using a similar language code) to specify the
language they're for. For now Mistria only supports English, but more languages may be supported in the future. Here's
an example file:

`localisation/first_mod.eng.json`
```json
{
  "letters/first_mod/subject_line": "Olrics Favour",
  "letters/first_mod/local": "I found something when rummaging through my items the other day and I want you to have it.\n\nCome see me at the Blacksmith shop when you have a moment."
}
```

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
    "ui_sub_category": "back",
    "lutFile": "images/lut.png",
    "uiItem": "images/ui.png",
    "outlineFile": "images/outline.png",
    "animationFiles": {
      "back_gear": "images/tail_animation"
    }
  }
}
```

For a full example, check out the [`dolphin_tail`](./mods/dolphin_tail) example.

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
      "random_selections": 5
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
    "IsAnimated": true,
    "Location": "images/v1/bounce",
    "OriginX": 16,
    "OriginY": 56,
    "MarginLeft": 3,
    "MarginRight": 29,
    "MarginBottom": 39,
    "MarginTop": 15
  }
}
```

For a full example, take a look at the [`Effe's Decor - Fridge`](./mods/Effe's%20Decor%20-%20Fridge) example. Files 
that are multiple frames of the same animation should be in their own folder, separate from other sprites. For reference,
the full list of sprite properties that you can control are:

```json
{
  "sprite_name": {
    "Location": "imageLocation.png",
    "IsAnimated": true,
    "BoundingBoxMode": 2,
    "OriginX": 0,
    "OriginY": 0,
    "MarginRight": 0,
    "MarginLeft": 0,
    "MarginTop": 0,
    "MarginBottom": 0,
    "IsPlayerSprite": true,
    "IsUiSprite": true
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

### `aurie/`
**Note: At the moment, Aurie mods will not install on the Steam Deck/Linux**

If you have Aurie DLLs as part of your mod, put them in the `aurie/` folder of your mod. MOMI will then automatically
install Aurie onto the players setup, register the keys necessary and copy your DLL into the correct Aurie folder. If
you do this, please set `minInstallerVersion` in your `manifest.json` to no lower than `0.2.0`.
