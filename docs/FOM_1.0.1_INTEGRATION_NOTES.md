# Fields of Mistria 1.0.1 integration notes

The local Steam installation was updated after the initial disposable-mod
test. The 1.0.1 `assets.zip` has SHA-256
`e425179986d55ae807a3b657dc7b3375e879ca00c648aa8f0c61e3e58f5f588e`.

## Localization comparison

Compared with the pre-1.0.1 archive used by the first integration test:

- localization entry count stayed at 18;
- no localization entries were added or removed;
- exactly one localization entry changed:
  `assets/localization/translations/rus.meta.toml`.

This confirms that the Bulgarian pipeline must use the updated 1.0.1 archive
as its source, but the localization directory structure did not change.

## MOMI integration result

ATD's Farmer still installs in the 1.0.1 disposable copy. The initial test
showed Wiki being skipped because the updated engine changed the context around
`assets/gml/scripts/Combat/MonsterUtils.gml` for seam `monster_death`.

The seam was changed from a brittle context concatenation to a structural target
inside `monster_death_poof`, anchored at the tokenized
`instance_destroy(self.owner);` statement. Seam checks now pass against both
the pre-hotfix and 1.0.1 archives, and Wiki installs with `--fail-on-skip`.

The 1.0.1 combined install was deterministic across repeated rebuilds and
uninstall restored the updated pristine archive byte-for-byte.

This is a MOMI engine-seam compatibility issue, separate from the Russian
localization fix. The seam catalog must be updated and tested against both the
pre-hotfix and 1.0.1 archives before relying on GML mods after the hotfix.
