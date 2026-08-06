# AssetsStore transactional rebuild design

Status: investigation complete; implementation follows this design.

## Scope

This change hardens archive lifecycle behavior only. It does not implement Fields of Mistria localization. Generic TOML installation remains unchanged; the staged archive is the only new write boundary.

## Current call flow

`ModInstaller.InstallMods` calls `EnsureBackup`, stages optional GML changes, calls `BeginRebuild`, writes `manifest.toml` and all generated/mod files through `IFileModifier`, then calls `Commit`. `ModInstaller.Uninstall` calls `AssetsStore.Uninstall`. `SeamVerifier` reads the pristine backup but does not write it. Tests also call the store directly to pin failure behavior.

## Transaction model

1. Classify the live archive using ZIP readability plus the state file and hashes.
2. Preserve or verify the pristine archive. A verified backup is never overwritten merely because the live archive lacks `manifest.toml`.
3. Copy the pristine archive to `assets.momi.tmp.zip` in the same directory.
4. Open only the temporary archive in `ZipArchiveMode.Update`.
5. Apply all changes through the existing `ZipFileModifier`.
6. On commit, dispose the temporary archive, reopen it, and validate:
   - ZIP readability and CRCs;
   - at least one `assets/` game entry;
   - no duplicate normalized entry paths;
   - every `.toml` entry parses with the project TOML parser.
7. Write the next state file to a temporary sibling and validate its own contents.
8. Atomically replace the live archive. On Windows, use `File.Replace` when a destination exists and fall back to a same-volume `File.Move(..., overwrite: true)` only where replacement is unavailable; never delete the live archive first.
9. Publish the state file after the archive succeeds. If state publication fails, leave the archive intact and report the state failure; the next run treats the hash mismatch conservatively.
10. Delete only the transaction’s own temporary files after success or failure.

Newly created ZIP entries receive a fixed DOS-compatible timestamp so identical
mod sets produce byte-identical archives. Existing entries retain their source
metadata where the ZIP implementation permits it.

The previous working `assets.zip` is not touched until the staged archive and validation have succeeded. A failed modifier, flush, validation, or replacement leaves the previous live archive intact.

## State and update classification

`assets.momi.state.toml` records schema version, pristine SHA-256, generated live SHA-256, MOMI version, installation UTC timestamp, and installed mod IDs/versions. The backup remains `assets.bak.zip` for compatibility.

The live archive is classified as:

- known generated: state exists and live hash equals the recorded generated hash;
- matching pristine: live hash equals the verified pristine hash;
- new vanilla/update candidate: live is readable and unmarked, while its hash differs from the recorded generated hash;
- unknown external modification: readable but inconsistent with state and not safely classifiable;
- damaged: missing/unreadable/truncated ZIP.

Only a readable, unmarked archive that is not inconsistent with a verified MOMI installation may establish or refresh the pristine backup. A state-marked or unknown archive is preserved and rejected with a diagnostic instead of guessing.

## Uninstall

Uninstall validates the backup and current live classification first, then stages the pristine backup to a temporary archive, validates it, and atomically replaces the live archive. It does not restore an older backup over a newer unmarked game update. On success it retains the verified backup. A MOMI state file is retained with pristine-as-live hashes and an empty mod list so backup provenance survives uninstall; legacy untracked state is removed. Stale temporary files are removed safely.

## Compatibility

The public `AssetsStore` paths remain `assets.zip` and `assets.bak.zip`. Existing callers continue to call `EnsureBackup`, `BeginRebuild`, `Commit`, and `Uninstall`; `ModInstaller` supplies installed-mod metadata when committing. Existing mods and generic archive entries are still rebuilt from pristine, so disabling a mod removes its content on the next successful install.

For isolated integration tests and portable deployments, `MOMI_GAME_LOCATION`
overrides Steam discovery and must point to a directory containing `Maybe.toml`.
`MOMI_GAME_CONFIG_DIR` similarly isolates generated `mods/manifest.json` output.
