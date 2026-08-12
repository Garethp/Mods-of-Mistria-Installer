# Changelog

## 0.1.1 — AIM

- Added Polish as a selectable AIM interface language.
- Added a complete Polish resource set for the GUI, CLI, archive recovery,
  validation diagnostics, profiles, mod dependencies, and cosmetic-mod errors.
- Added the Polish language name to every existing interface-language menu.

## 0.1.0 — AIM

- Renamed the user-facing application to **AIM — Alternative Installer for Mistria**.
- Reset the new AIM release line to version `0.1.0`; the published `0.15.7` history remains unchanged.
- Added Ukrainian as a selectable interface language.
- Added the first Ukrainian translations for the main window, profiles, installation flow, archive state, phases, and location detection.
- Documented direct ZIP/RAR mod reading; archives no longer need to be extracted before installation.
- Documented the gear-menu **Launch game directly** toggle, which starts the detected game executable and falls back to Steam when needed.
- Documented the current duplicate-version limitation: remove the older copy before adding a newer version of the same mod.
- Documented that AIM should be closed before moving or replacing mod archives, which may otherwise remain locked while the application is open.
- Kept technical MOMI namespaces, state paths, manifest keys, and migration compatibility unchanged.
- Clarified that AIM is an independently maintained fork of MOMI and retains MMAPI compatibility and upstream attribution.

## 0.15.7 AI

- Added runtime UI language switching without restarting MOMI.
- Persisted the selected UI language between launches.
- Added GUI resources for Bulgarian, German, French, Dutch, Brazilian Portuguese, Russian, Indonesian, Simplified Chinese, Traditional Chinese, Korean, Japanese, and Spanish.
- Added localized language names in their respective languages.
- Localized setup diagnostics, profile dialogs, missing-dependency dialogs, external-link prompts, file-picker titles, error details, update labels, and installation progress phases.
- Corrected archive-status wording so it reports detected installed mods instead of implying that a MOMI installation is being created.
- Kept archive status and installed-mod counts synchronized with the selected profile.
- Reduced language-switch refresh work by consolidating UI notifications and avoiding unnecessary `assets.zip` rescans.
- Added resource validation coverage for duplicate keys and malformed `.resx` files.
- Added a language-menu checkmark showing the currently selected UI language.
- Updated the application and CLI version metadata to 0.15.7.
- Known limitation: legacy cosmetic mods that use the old 49-frame `back_gear`
  format are not automatically converted to the current 59-frame animation
  layout. Such mods need an updated release from their author; changing only
  the TOML validation or offset is not sufficient to make them compatible.

## 0.15.6

- Fixed the Install button state after creating, switching, or deleting profiles.
- Install is now disabled when no mods are selected, including after all mods are deselected.
- Install state now refreshes immediately when profile selection changes.

## 0.15.5

- Added direct support for ZIP and RAR mods without requiring them to be extracted first.
- Added support for archive-backed mods containing either `manifest.toml` or `manifest.json` at the mod root.
- Added duplicate-mod guidance so the same mod is not kept both as a folder and as an archive in the active mods folder.
- Updated the per-mod installation status panel with theme-aware colours, including a readable dark-theme status and error state.
- Install is now disabled when the selected profile exactly matches the installed mod IDs and versions, and is re-enabled when the set changes or a rebuild is required.
- Play remains available for a valid clean game installation even when no MOMI mods are installed.
- Reduced the default GUI size and header image height for better use on high-DPI displays.
- Updated the release metadata and documentation for MOMI 0.15.5 AI.

This release does not include game archives or copyrighted localization data.
