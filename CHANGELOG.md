# Changelog

## 0.15.5-fork

- Updated the per-mod installation status panel to use theme-aware colours, including a readable dark-theme status and error state.
- Install is now disabled when the selected profile exactly matches the installed mod IDs and versions, and is re-enabled when the set changes or a rebuild is required.
- Play is now available for a valid clean game installation even when no MOMI mods are installed; it is only blocked while an installation is in progress or the game files are not usable.

## 0.15.4-fork

- Retained the staged `assets.zip` rebuild and pristine-backup workflow, including archive validation before the live archive is replaced.
- Preserved the previous working archive when installation fails and added clearer mod-specific diagnostics and error logs.
- Added installed-state and version reporting so the UI can distinguish a matching MOMI installation from a changed or externally modified archive.
- Added safer game-update handling, including adoption of a verified new vanilla archive and preservation of the previous backup during migration.
- Added discovery for supported case variants of the `mods` folder and for a mods folder next to the game or MOMI executable.
- Added persisted profiles and mod load order, high-DPI UI improvements, and a guarded **Play** button.
- Added support for current 1.0.x TOML localization, font, manual-load asset, and cosmetics workflows already supported by this fork.
- Added direct support for archive-backed mods in ZIP and RAR format alongside existing extracted mod folders.
- Archive-backed mods can contain either `manifest.toml` or `manifest.json` at their mod root.
- Location and tiled-asset installation now reads through the mod abstraction, so the same supported content can be installed from a folder or an archive.
- Added validation coverage for archive-compatible location installation.
- Documented that only one copy of a mod should be kept in the active mods folder. Keeping an extracted folder and an archive of the same mod together makes MOMI discover both copies.

The 0.15.4 fork also includes the upstream 1.0.x MMAPI compatibility updates present in this branch, including controller support, current movement and mount seams, infusion chance support, spell-cast compatibility, and cosmetics validation.

This fork does not include game archives or copyrighted localization data.
