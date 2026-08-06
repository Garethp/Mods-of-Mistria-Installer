# Release checklist

Use this checklist before publishing a MOMI build or opening a pull request.

## Repository hygiene

- [ ] No game archives, extracted game assets, generated reports, or local test data are tracked.
- [ ] No machine-specific paths, usernames, credentials, tokens, or private URLs are present.
- [ ] Test scripts use parameters or temporary directories instead of developer-specific paths.
- [ ] README and documentation describe the current 1.0.x workflow, not obsolete pre-1.0 behavior.
- [ ] Copyrighted game localization data is kept outside the repository.

## Build and tests

- [ ] Run `dotnet test ModsOfMistriaInstaller.sln --configuration Debug --no-restore`.
- [ ] Run the 1.0.x seam check against a disposable copy of the game archive.
- [ ] Test a successful rebuild and verify the resulting archive can be reopened.
- [ ] Test a failed modifier and confirm the previous live archive remains intact.
- [ ] Test uninstall and confirm the verified pristine archive is restored transactionally.
- [ ] Test a game update and an externally modified archive; neither should be overwritten by guesswork.
- [ ] Test repeated installation with the same mod set and removal of a previously enabled mod.
- [ ] Test multiple profiles and persisted load order in the GUI.

## Release notes

Document the supported MOMI version, supported game version, compatibility
limitations, safety improvements, and any known test limitations. Do not claim
an upstream issue is fixed unless the local implementation and a regression
test demonstrate it.
