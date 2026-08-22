# Mods of Mistria Installer

## Installation
1. Create a mods folder to put your mods
   * On Windows, you'll want to create "mods" folder inside your Fields of Mistria folder, next to the `FieldsOfMistria.exe`.
   * On the Steam Deck (or other Linux distros) you can also create a mods folder inside your Fields of Mistria folder, 
     or you can create a `mistria-mods` folder in your home directory.
2. Download the installer from the [releases page](https://github.com/Garethp/Mods-of-Mistria-Installer/releases).
3. Double-click the installer to run it. If it's not able to detect the Fields of Mistria location, try placing the
   installer in your Fields of Mistria folder, next to `Maybe.toml` file.
4. Click the "Install" button to install the mods. If you have mods in your mods folder, they should appear in a list.
5. Next time the game updates, run the installer again to re-install your mods.

## Troubleshooting
**I installed a new cosmetic, but it's not appearing in existing games**  
First try downloading the latest version of MOMI and re-installing. If you are still encountering the issue, open the page where you
downloaded the mod. First check its latest update date (anything prior to **July 2026** won't work), then check the mod description
to see if it mentions a specific shop where you can buy the item or another way to obtain it. If nothing is mentionned, check the
general store. If you're still having issues, feel free to come to the Discord Server to ask for help.

**The installer says it cannot find the Fields of Mistria Location**  
Try placing the installer in your Fields of Mistria folder, next to `Maybe.toml` file, this should allow the installer to find
the game.

**The installer says it cannot find the mods folder**  
Make sure you have created a folder called "mods" in your Fields of Mistria folder, next to `Maybe.toml` file, or a folder
called `mistria-mods` in your home directory if you're on the Steam Deck/Linux.

**The installer says it didn't find any mods to install**  
Make sure you have mods in your mods folder and the mods are compatible with the installer. If you're unsure, check the
mod folder, inside it there should be a `manifest.toml` file. If there's not, the mod is not compatible and will have to
be updated by the mod author.

The installer cannot install mods that are `.zip` files, so make sure the mods are extracted. When extracting, make sure
that the mod folder is directly inside the mods folder, not inside another folder. For example, if you're installing
"Effe's Decor - Fridge", make sure that the folder structure is `mods -> Effe's Decor - Fridge -> manifest.toml` and not
`mods -> Effe's Decor - Fridge -> Effe's Decor - Fridge -> manifest.toml`. Noticed the duplicate name ? This is called 
"nested folders", which stops MOMI from finding the `manifest.toml` file and thus detecting the mod.

**I've got a different problem**  
If your problem isn't listed above, please come and ask in the [Fields of Mistria Discord](https://discord.com/invite/j6bTZvMtsg).
There's a `#modding-game-help` channel that you'll see after you accept the rules and that's the best place to get help. To provide
more information, try downloading the `-cli` version of the installer, running that and then screenshotting the window
that popped up. The `-cli` version doesn't look as nice, but should provide more information about what's going wrong.

## Mod Format
If you're a modder and want to make your mod compatible with this installer, feel free to refer to the [`mods`](./mods)
folder for example mods. Below is information for what you'll need. This is not a comprehensive list and more
documentation will be added in the future.

### `manifest.toml`
```toml
name = "Mod Name"
author = "Mod Author Name"
version = "1.0.0"
minInstallerVersion = "0.13.1"
manifestVersion = "2"
```

Your mod will be given an ID that's based on the author and name fields, so make sure that those two combined are unique.
From version 0.1.3 onwards, the installer will check the `minInstallerVersion` field to make sure that the installer is
new enough to install the mod and tell the user if they're unable to install the mod without updating the installer.
The `manifestVersion` field isn't used yet, but will allow for backwards compatibility in future versions of the installer
if large changes are made to how mods are structured.

### `momi/cosmetics/`
If you want to add new cosmetics to the game, you can do so by placing a TOML definition for the cosmetic in the 
`momi/cosmetics/` folder and the sprites should be in a `images/` folder, however they can go anywhere. Here's an example
file:

```toml
[lryn_celine_summer_skirt]
name = "Celine's summer skirt"
ui_slot = "bottom"
ui_sub_category = "skirt"
default_unlocked = true

cosmetic_sprites = { waist = "img/skirt.png" }

lut = "img/lut.png"
ui_sprites = {
    ui = "img/ui.png",
    outline = "img/outline.png"
}
```

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

## AI Policy

As MOMI is meant to be a community project there's no policy against AI assisted contributions with the exception of
Translations which cannot be AI assisted or Machine Translated, due to the difficulty of performing a meaningful review
of other languages. That being said, it is expected that if AI is used, it should be used for assistance, not for doing
the entire contribution.

Regardless of whether AI was used to assist in a contribution or not, each contribution should meet the following
standards:

1. The contributor should fully understand what it is they're contributing and how it'll affect the application.
2. The contributor should be able to meaningfully communicate the changes and the purpose of the changes.
3. The contributor should be able to support those changes after they go live.

In effect, this means that your PR should not contain AI written summaries and you should be able to explain the changes
made and their purpose. If there are any questions or requested clarifications for your PR the responses should come from
you, not from an AI. If the communication for your PR is written by AI then it's difficult to be confident that you
understand the changes made and can support them.