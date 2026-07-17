# momi-gml-probe

A binary that compiles a GML file and executes it on the pinned
[Fabricator](https://github.com/kyren/fabricator) VM, so MOMI's test suite can
exercise the carried MMAPI framework without a separate Fabricator checkout. It
executes, so it never ships.

Its sibling `momi-gml-check` compiles but *never* executes. See
[CHECKER.md](CHECKER.md) for additional details.

## Modes

| Mode | Description |
| ---- | ----------- |
| `momi-gml-probe run <path>` | Compiles `<path>` and executes it on the pinned Fabricator VM. The script's own output (`show_debug_message`) goes to stdout and diagnostics go to stderr, so stdout parses as probe output alone. |
| `momi-gml-probe --version` | Prints the crate version and the pinned Fabricator rev. |

## Exit Codes

- `0`: Ran to completion.
- `1`: Compile error or uncaught runtime error. Reported to stderr as
  `<path>: <error>`.
- `2`: Usage or I/O error (bad arguments or an unreadable file).

## What it is For

Its consumers are the MMAPI runtime suites and the engine-claim probes, which
execute the carried framework on the pinned VM. They are the regression net
under the VM-behaviour claims MMAPI's architecture rests on.

This binary is built from the same
pinned deps as the checker, so the suites arm themselves wherever the checker
is built.

## Why a Separate Binary

Rather than a `run` mode on `momi-gml-check`, the checker is what the installer
invokes on user mod code, and "compiles, never executes" is a safety property
of that binary.

The
probe stays a distinct binary so the checker keeps its compile-only guarantee.

## Never Shipped

The probe executes GML and is never bundled into a released app. Only the
checker ships, so the probe cannot reach an end user's machine (see
[PACKAGING.md](PACKAGING.md)). It is built and used in CI (`ci.yml`, on every
push) and in local test runs.

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

Builds both binaries. See [CHECKER.md](CHECKER.md) for `momi-gml-check`. The
release profile uses `strip` and `lto`.

`MOMI_GML_PROBE` overrides probe discovery so a set-but-missing path fails
loudly rather than silently skipping.

`ci.yml` builds both binaries on every push and points the test suite at them
through `MOMI_GML_CHECKER` and `MOMI_GML_PROBE`, so the MMAPI runtime suites
and the engine-claim probes run wherever CI runs, and locally the same way.
