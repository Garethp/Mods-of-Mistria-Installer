## TODO

* [ ] Anywhere where we auto-generate localisation, allow for more than one language to be defined
* [x] Allow for installing either one mod or multiple
* [ ] Add a way to install mods from a zip file
* [x] Allow random stock for the Store Generator
* [x] Generate correct checksums
* [x] Allow installers/generators to self-register
* [x] Rewrite to C#
  * [x] Implement the Schedules Installer
  * [x] Implement the Conversations Installer
  * [x] Implement the Simple Conversations Generator
  * [x] Implement the Checksum Installer
  * [x] Implement the Uninstaller
* [ ] Implement a shadow manifest generator
* [x] Add UMT integration
  * [x] Implement Sprite Installers
  * [x] Implement Tileset Generators/Installers
  * [x] Implement Portrait Generators/Installers
* [x] Add a way to check if an update has occurred for fresh installs
* [x] Fetch mod names from the manifest
* [x] Pass around a mod object instead of a source folder
* [ ] Add a basic GUI
  * [x] Auto-detect the game's location
  * [ ] Allow for selecting the game's location
  * [ ] Allow for selecting the mod folder
  * [x] Show the progress of the installation
* [x] Add an example mod
* [x] When importing files, order them by name alphabetically
* [x] Add a `minInstallerVersion` and `manifestVersion` to the manifest
* [x] Add null handling to the fiddle merge
* [x] Add a shop generator
* [ ] Validate the mods before installing
  * [x] Validate Store Category
  * [x] Validate Outfit
  * [x] Validate SpriteData
  * [x] Validate Tileset
  * [ ] Validate Simple Conversation
  * [x] Show errors/warnings on Command Line
  * [x] Show errors/warnings in the GUI
  * [ ] Perform exception checking when reading mod manifest
* [x] Show warnings and errors in the GUI
* [x] Allow users to select which mods to install
* [ ] Store selected/deselected mods to install in a JSON file in the mods folder
* [ ] Give the user a notice if there's a newer version of the installer
* [ ] Allow mods to declare dependencies on other mods
* [ ] New installers:
  * [ ] `animation/generated/player_tools.json`
  * [ ] `animation/generated/shadow_manifest.json`
  * [ ] `starting_farms/farms.json`
  * [ ] `room_data/hyper_points.json`
  * [ ] `t2_input.json`
  * [ ] `sounds`
* [ ] Add translations for exceptions
* [ ] Add translations for validation prefixes