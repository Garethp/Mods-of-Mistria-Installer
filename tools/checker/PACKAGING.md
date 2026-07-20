# Release Packaging

How `momi-gml-check` reaches an end user's machine, and why `momi-gml-probe`
never does. This covers the build-and-bundle path only. For the binaries
themselves, see [CHECKER.md](CHECKER.md) and [PROBE.md](PROBE.md).

A published MOMI app is a single self-contained executable per platform. The
checker is not a second file beside it and not a separate download. It is
embedded inside that one executable and extracted at launch. Only the checker
ships. `momi-gml-probe` executes GML and is never bundled (see
[PROBE.md](PROBE.md)).

## The Single-File Model

Both shipping projects, `ModsOfMistriaCommandLine` and `ModsOfMistriaGUI`,
publish as a self-contained single file:

- `PublishSingleFile=true`, `SelfContained=true`, `PublishReadyToRun=true`
- `IncludeAllContentForSelfExtract=true`

The last one is what carries the checker. It embeds every content item, the
checker among them, into the single-file bundle, and the host extracts them to
a temporary directory at launch. `AppContext.BaseDirectory` points at that
directory, so the extracted checker sits beside the running app. (`Debug`
builds set `PublishSingleFile=false`, so there the checker is a loose file in
the output directory instead.)

## The Bundle Hookup

Each project's `.csproj` pulls the checker in as a content item, guarded on the
per-RID directory existing so a dev build without it still compiles (the gate
then reports no backend):

```xml
<ItemGroup Condition="Exists('..\tools\checker\dist\$(RuntimeIdentifier)')">
  <None Include="..\tools\checker\dist\$(RuntimeIdentifier)\momi-gml-check*"
        CopyToOutputDirectory="PreserveNewest" Link="%(Filename)%(Extension)" />
</ItemGroup>
```

`dist/<rid>/` is where CI stages the arch-matched binary before publish. Only
`momi-gml-check*` is globbed. The probe is never in that directory.

## Built and Bundled in CI

The checker is native, so it cannot ride the .NET cross-compile. It is built on
a runner matching its target, then handed to the publish job as an artifact.
Both release workflows follow the same shape.

`compile.yml` (GitHub releases, on `release: published`):

1. A `checker` job builds `momi-gml-check` for each arch (`win-x64`, `win-x86`,
   `linux-x64`, `osx-x64`) on a native runner (`rustup target add`, then
   `cargo build --release --bin momi-gml-check --target <target>`), and uploads
   it as artifact `checker-<arch>`.
2. The `compile` job (per project × arch) downloads `checker-<arch>` into
   `tools/checker/dist/<arch>`, runs `dotnet publish -r <arch>`, which embeds
   the checker per the csproj rule above, and attaches the result with
   `gh release upload <tag> Release/*`.

GitHub-release assets are the bare single-file executables (`Release/*`), one
per project and arch, each with the checker inside.

`nexus_upload.yml` (NexusMods, on `release: released`) does the same for the
GUI across `win-x64`, `linux-x64` and `osx-x64`, laying every `checker-<arch>`
out under `tools/checker/dist/<arch>` before publishing. The Windows build
uploads as a bare `.exe`. The Linux and OSX builds are zipped for the Nexus
uploader. That zip is only a transport wrapper. The checker is still embedded
in the one executable inside it.

Only `momi-gml-check` is built in either release workflow. `ci.yml` builds both
binaries, but for tests, not for shipping (see [PROBE.md](PROBE.md)).

## Runtime Discovery

At apply time `GmlCompileGate.ResolveChecker` (in
`ModsOfMistriaInstallerLib/Tools/GmlCompileGate.cs`) locates the binary in
order:

1. `MOMI_GML_CHECKER`, if set. A set-but-missing path throws rather than
   silently demoting to no gate.
2. Beside the app (`AppContext.BaseDirectory`), where the single-file bundle
   self-extracts. On non-Windows the extract drops the execute bit, so the gate
   restores it.
3. The repo dev build at `tools/checker/target/release/`.

If none resolve, an auto gate logs and skips and a mandatory gate errors. A
shipped app always resolves case 2.
