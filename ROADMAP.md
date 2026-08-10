# Roadmap

## Project identity

- Next release branding: **AIM — Alternative Installer for Mistria**, starting at `0.1.0`.
- Do not retroactively rename the published `0.15.7` release.
- Do not add an `AI` suffix to AIM version labels.
- Ukrainian localization support is complete for the first AIM release.
- Keep user-facing AIM branding distinct while retaining the MOMI fork attribution, technical namespaces, MMAPI compatibility, and migration compatibility.

## 0.15.3 AI fork release
* [x] Keep the four focused MMAPI additions with explicit event/lifecycle contracts.
* [x] Add focused MMAPI hooks for fishing selection, museum donation attempts, pet rewards and crop harvest lifecycle.
* [x] Update the shipped MMAPI catalog to 103 hooks and 112 seams.
* [x] Validate the new catalog entries with focused and full test coverage.

## 0.15.2 AI fork release
* [x] Target Fields of Mistria 1.0.x archive and localization workflows
* [x] Stage and validate `assets.zip` rebuilds before replacing the live archive
* [x] Preserve the previous working archive when installation fails
* [x] Add TOML validation, custom font installation and manual-load support
* [x] Add installation diagnostics, high-DPI UI sizing and guarded game launch
* [x] Point update checks and release tooling at the maintained fork

## 0.15.1
* [x] Rebuild `assets.zip` transactionally from a verified pristine archive
* [x] Validate staged archives before replacing the live game archive
* [x] Detect game updates and unknown external archive changes
* [x] Restore the pristine archive transactionally during uninstall
* [x] Provide improved installation diagnostics and adaptive UI sizing

## 0.2.0
* [x] Add Aurie Integration

## 0.3.0
* [x] Enable installing from `.zip` files
* [x] Enable installing from `.rar` files
* [x] Add an uninstall button

## 0.4.0
* [x] Add some user-information when installing/uninstalling Aurie mods
* [x] Warn people when they are running the 32-bit version
* [ ] Automatically update Aurie
* [x] Select the Mistria/Mods folders in a setup screen if not found
* [x] Allow creating a mods folder automatically
* [ ] Add converting old sprite mods

## Future/Unknown
* [ ] Allow all "localised" text in easy JSON structures to be multi-lingual
* [ ] Add Validators for Simple Conversations
* [x] Store selected/deselected mods in the Mods folder
* [x] Allow load order modifying
* [x] Allow mods to declare dependencies on other mods
* [ ] Automatic updating
* [ ] `player_tools.json` installer
* [ ] `farms.json` installer
* [ ] `hyper_points.json` installer
* [ ] `t2_input.json` installer
* [ ] Sounds installer
* [ ] Improve translations for validations (the prefixes are not pulled from localisations)
* [ ] Add translations for exceptions
* [ ] Catch all exceptions in the GUI
* [ ] Add a error_log file for the GUI
* [ ] Add a JSON browser
* [ ] Scramble JSON automatically on install
* [ ] Cutscene generator
* [ ] Automatically refresh mods when a change has been made
* [ ] In the GUI, skip mods that fail `CanInstall` instead of disabling install
