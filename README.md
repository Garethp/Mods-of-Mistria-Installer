# Mods of Mistria Installer — MOMI 0.15.2 AI

This is an independent fork of [Mods of Mistria Installer](https://github.com/Garethp/Mods-of-Mistria-Installer), maintained for **Fields of Mistria 1.0.x**.

The `AI` label identifies this fork build. The numeric application version remains `0.15.2` so update checks and release tooling continue to use a normal semantic version.

## What this fork supports

- Fields of Mistria 1.0.x mod installations.
- Mod folders and archives containing either `manifest.toml` or `manifest.json`.
- TOML, JSON, image, outfit, furniture, item, object, store, shadow, font and manual-load mod content supported by the current MOMI installer modules.
- GML mods using the MMAPI format documented in [`docs/MMAPI`](docs/MMAPI).
- Profiles and persisted mod load order.
- Rebuilding `assets.zip` from a verified pristine backup, so disabled or removed mods are removed on the next successful rebuild.
- Staged installation diagnostics, archive validation and recovery when an installation fails.
- A Play button that is enabled only after MOMI has a valid installed archive.

This fork is intended for Fields of Mistria 1.0.x. Individual mods may still require a specific MOMI version or game patch; check the mod author's compatibility notes.

## Installation

1. Download the latest release from the [AcTePuKc fork releases page](https://github.com/AcTePuKc/Mods-of-Mistria-Installer/releases).
2. Create or select a `mods` folder next to `FieldsOfMistria.exe`, or use the supported `mistria-mods` location on Linux/Steam Deck.
3. Put each mod directly in the mods folder. A mod must contain `manifest.toml` or `manifest.json` at its root; nested duplicate folders prevent detection.
4. Start MOMI, select the mods to install, and click **Install**.
5. Use **Play** only after the installation completes successfully.

MOMI preserves a pristine backup and writes a staged archive before replacing the live `assets.zip`. Do not delete the backup while MOMI is managing the installation. Keep a separate game backup before testing unfamiliar mods.

## Updating the game

After a Fields of Mistria update, verify the game files through Steam if necessary, start MOMI, refresh the mod list, and reinstall the enabled mods. Mods made for an older game or installer version may need to be updated by their authors.

## Troubleshooting

- If the game location is not detected, place MOMI next to `Maybe.toml` or select the game directory in Settings.
- If no mods appear, check that the manifest is at the mod root and that the mod supports Fields of Mistria 1.0.x.
- If installation fails, MOMI keeps the previous live archive, shows the failing mod when available, and writes a diagnostic log under the MOMI local data directory.
- If the game was modified outside MOMI or the pristine backup is missing, restore/verify the game files through Steam before trying again.

For bugs and fork-specific support, use the [fork issue tracker](https://github.com/AcTePuKc/Mods-of-Mistria-Installer/issues). The upstream project and its documentation remain available at [Garethp/Mods-of-Mistria-Installer](https://github.com/Garethp/Mods-of-Mistria-Installer).

## Development

Build the solution with .NET 8:

```powershell
dotnet build ModsOfMistriaInstaller.sln --configuration Release
dotnet test ModsOfMistriaInstaller.sln --configuration Release
```

The release workflow builds the GUI and CLI for the supported desktop targets and uploads artifacts only to releases in this fork. Nexus publishing is manual and is not triggered by a normal GitHub release.

The repository does not include game archives or copyrighted game localization data.
