# FMOD Native Binary Packaging

How `fmod64.dll`, `fsbank64.dll` and `libfsbvorbis64.dll` reach a shipped
MOMI build, for [`ModsOfMistriaInstallerLib/Audio/`](../../ModsOfMistriaInstallerLib/Audio)'s
in-place audio bank replacement (see
[`docs/investigations/custom-music.md`](../../docs/investigations/custom-music.md)
and [`AudioInstaller`](../../ModsOfMistriaInstallerLib/Installer/AudioInstaller.cs)).

## Unresolved before a real release: redistribution terms

**These three DLLs are Firelight Technologies' proprietary FMOD Engine
binaries, not something this repo builds or owns.** Bundling them into
`momi-gml-check`-style content items (below) makes it technically possible to
ship them inside MOMI's own releases, but *whether MOMI is allowed to* is a
separate, unresolved question - FMOD's EULA has historically required either
a free indie/small-business registration (with specific in-product attribution
requirements) or a paid license once revenue crosses a threshold, and that
determination hasn't been made for this project. Do not cut a public release
with these bundled until that's checked. Local/dev builds and personal use are
unaffected either way.

## The staging convention

Same shape as [`tools/checker/PACKAGING.md`](../checker/PACKAGING.md)'s
`momi-gml-check` bundling, but for binaries this repo doesn't build:

```xml
<ItemGroup Condition="Exists('..\tools\fmod\dist\$(RuntimeIdentifier)')">
  <None Include="..\tools\fmod\dist\$(RuntimeIdentifier)\*.dll"
        CopyToOutputDirectory="PreserveNewest" Link="%(Filename)%(Extension)" />
</ItemGroup>
```

Present in both `ModsOfMistriaCommandLine.csproj` and `ModsOfMistriaGUI.csproj`.
`IncludeAllContentForSelfExtract=true` (already set in both, per
`tools/checker/PACKAGING.md`) carries these into the single-file publish the
same way it carries the checker.

Conditioned on existence so a dev build without them still compiles.
Missing at runtime is handled gracefully, not a crash: `AudioInstaller`
catches the native load failure per bank and records it as a validation
error against that specific mod, so a mod that doesn't touch `momi/audio`
installs completely normally, and even a mod that does only loses its own
audio replacement rather than taking the whole install down. This wasn't
true until it was actually tested against a real published build - see
[`docs/investigations/custom-music.md`](../../docs/investigations/custom-music.md)'s
"Verified against a real published build" section for what broke and what
that fixed.

## Populating `dist/<rid>/` for a local build

Not staged by CI today (see below). To bundle for your own build:

```powershell
mkdir tools\fmod\dist\win-x64
copy \path\to\fmod64.dll,fsbank64.dll,libfsbvorbis64.dll tools\fmod\dist\win-x64\
```

Get the three files from the FMOD Engine SDK (needs a free FMOD account) or
from any release of [Fmod-Bank-Tools](https://github.com/Wouldubeinta/Fmod-Bank-Tools),
which bundles the identical files. Same source `tools/audio-replace`
(a dev CLI, not part of the shipped product) already documents via
`MOMI_FMOD_NATIVE_DIR` - see [`tools/audio-replace/README.md`](../audio-replace/README.md).

## Not wired into CI

Unlike `momi-gml-check` (built fresh by a CI job every release,
`tools/checker/PACKAGING.md`'s CI section), there is no CI step that
populates `tools/fmod/dist/<rid>/` - these binaries are not something CI can
build or fetch from a public URL. Wiring that up needs a decision on *how*
CI would obtain them (a private release asset, a secret holding a download
URL, checking them into private storage) - out of scope for this change,
and blocked on the licensing question above regardless.

## Platform scope

Windows only (`win-x64`) for now, matching `ModsOfMistriaInstallerLib/Audio`'s
own current scope. Linux/macOS FMOD binaries and P/Invoke resolution are
deliberately deferred, not forgotten.
