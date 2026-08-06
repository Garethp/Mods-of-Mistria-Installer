# Upstream issue mapping

This document records which upstream reports are addressed by the current
fork changes. An issue remains open upstream until the change is reviewed and
merged by the upstream maintainers.

## Addressed by this change

### [#112 – Persistent changes in the Priority Listing](https://github.com/Garethp/Mods-of-Mistria-Installer/issues/112)

Profiles now persist enabled mod IDs and load order. The active profile is
restored when the profile list is rebuilt, and the current profile is saved
when the application closes. Regression coverage is in
`ModsOfMistriaInstallerLibTests/ProfileManagerTest.cs`.

### [#129 – Validate TOML files and give user-friendly error message](https://github.com/Garethp/Mods-of-Mistria-Installer/issues/129)

Source TOML files are validated before an archive transaction begins. Errors
include the mod name, mod ID, and relative source file path. The final archive
validator also reports the exact invalid ZIP entry. Coverage is in
`ModsOfMistriaInstallerLibTests/ModInstallerTest.cs` and
`ModsOfMistriaInstallerLibTests/Store/AssetsStoreTest.cs`.

### [#122 – More descriptive errors when MOMI cannot detect Fields of Mistria](https://github.com/Garethp/Mods-of-Mistria-Installer/issues/122)

The setup screen now distinguishes missing `Maybe.toml`, missing game assets,
missing or damaged `assets.zip`, missing mods folders, and unreadable paths.
Coverage is in `ModsOfMistriaInstallerLibTests/LocationDiagnosticsTest.cs`.

## Related but not claimed as fixed

Reports about Linux/Wine/Aurie behavior, specialized asset formats, Nexus
update checking, and older “mods installed but not visible” reports require
separate reproduction and scope. The transactional archive rebuild reduces
the risk of several failure modes, but it is not sufficient evidence to close
those issues.
