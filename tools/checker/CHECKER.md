# momi-gml-check

A small prebuilt binary that compile-checks GML files with the
[Fabricator](https://github.com/kyren/fabricator) compiler, so MOMI can
validate mod scripts at apply time without end users needing a Rust
toolchain. It compiles only, nothing is executed.

This crate also builds a second binary, `momi-gml-probe`, which *does* execute. See
[PROBE.md](PROBE.md) for additional details.

## Modes

| Mode | Description |
| ---- | ----------- |
| `momi-gml-check files [--files-from <listfile>] [paths...]` | Compiles each path as an independent chunk. Every failing file is reported. |
| `momi-gml-check unit [--files-from <listfile>] [paths...]` | Compiles all paths together as one compilation unit, in lexicographic path order. This additionally catches cross-chunk duplicate top-level function exports and cross-chunk stdlib shadowing. |
| `momi-gml-check --version` | Prints the crate version and the pinned Fabricator rev. |
| `--files-from <listfile>` | Reads one path per line (UTF-8, blank lines ignored) and avoids Windows command-line length limits. It can be combined with positional paths. |

## Exit Codes

- `0`: All files compiled.
- `1`: At least one compile failure. Each is reported to stderr as
  `<path>: <error>`.
- `2`: Usage or I/O error (bad arguments, unreadable file or listfile).

## What it Catches

| Case | Caught? | Notes |
| ---- | ------- | ----- |
| Syntax and structural errors | Yes | |
| Same-file duplicate top-level functions | Yes | |
| Shadowing of Fabricator stdlib names | Yes | |
| Cross-chunk duplicate top-level exports | `unit` mode | Reported as a duplicate-export error. |
| Cross-chunk stdlib shadowing | `unit` mode | |
| Unresolved names | No | Fabricator late-binds unknown identifiers and calls in every dialect, so calling a nonexistent function still compiles. |
| Shadowing of game-engine API names | No | The checker's magic set is the Fabricator stdlib only, so a mod function that shadows a Fields of Mistria engine builtin passes here but fails at game boot. |

## Fabricator Pin

The `rev` on each Fabricator dependency in this crate's `Cargo.toml`
(`d7f0cbdce2ac877c90304261a0793ceaf85f21e9`) is the authoritative Fabricator
pin.

`src/rev.rs` holds the string both binaries report via `--version`. Keep it in step with the dependency revs. `GmlCompileGateTest` asserts all five
references agree, so a partial re-pin fails the build rather than shipping a
binary that misreports its own VM.

## Building

```
cargo build --release --manifest-path tools/checker/Cargo.toml
```

builds both binaries; see [PROBE.md](PROBE.md) for `momi-gml-probe`. The
release profile uses `strip` and `lto`.

`momi-gml-check` is ~1.6 MB.
`MOMI_GML_CHECKER` overrides checker discovery so a set-but-missing path fails
loudly rather than silently skipping.

## Bundling

The release pipeline builds `momi-gml-check` per OS and bundles it into the
published apps, so end users get it without a Rust toolchain. How that works, and why the probe never rides along, is explained in [PACKAGING.md](PACKAGING.md).
