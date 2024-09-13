## TODO

 * [ ] Anywhere where we auto-generate localisation, allow for more than one language to be defined
 * [x] Allow for installing either one mod or multiple
 * [ ] Add a way to install mods from a zip file
 * [ ] New installers:
    * [ ] `animation/generated/player_tools.json`
    * [ ] `animation/generated/shadow_manifest.json`
    * [ ] `starting_farms/farms.json`
    * [ ] `room_data/hyper_points.json`
    * [ ] `t2_input.json`
 * [x] Generate correct checksums
 * [ ] Allow installers/generators to self-register
 * [ ] Add sounds?
 * [/] Rewrite to C#
   * [x] Implement the Schedules Installer
   * [x] Implement the Conversations Installer
   * [x] Implement the Simple Conversations Generator
   * [ ] Implement the Mist Generator
   * [x] Implement the Checksum Installer
   * [ ] Implement the Uninstaller
 * [/] Add UMT integration
   * [x] Implement Sprite Installers
   * [ ] Implement Tileset Generators/Installers
   * [ ] Implement Portrait Generators/Installers
 * [x] Add a way to check if an update has occurred for fresh installs
 * [x] Fetch modnames from the manifest
 * [x] Pass around a mod object instead of a source folder
 * [/] Add a basic GUI
   * [x] Auto-detect the game's location
   * [ ] Allow for selecting the game's location
   * [ ] Allow for selecting the mod folder
   * [x] Show the progress of the installation
 * [x] Add an example mod