# Troubleshooting

[← MMAPI](MMAPI.md)

When a mod does not behave, the log is the first stop. Then work down this list by symptom.

## Read the Log First

Every mod gets a log file:

```text
%LOCALAPPDATA%/FieldsOfMistria/mod_data/<mod>/logs/<mod>.log
```

Log a line from your handler to prove it ran, and raise the level to see more. A lone Info or Debug line may wait for the 20-line file batch; use Warn for the probe, or call `mmapi_log_flush("my_mod")` after it. See [Log](API_REFERENCE.md#log).

## The Mod Was Skipped At Install

MOMI installs a mod whole or skips it whole, and prints the reason. Common causes:

- **A required hook is missing.** A hook in `requires_hooks` the catalog does not declare. Update MOMI, or correct the name. See [The Manifest](MANIFEST.md#requires_hooks).
- **The GML does not compile.** A syntax error anywhere in the mod's `gml/` tree. The message names the file and the parse error.
- **An install-namespace clash.** Two mods derive the same `scripts/<id>/` directory. Change one mod's `author` or `name`; there is no separate manifest id.
- **A duplicate top-level function.** The mod exports a name already owned by the engine, MMAPI, an earlier mod, or another file in the same mod. Prefix the function and update its calls.
- **An unsafe GML path.** A path under `gml/` would escape or alias the mod's script directory. Remove traversal and normalize the package layout.
- **A strict lint.** `--strict-lints` turns a file-bearing warning into a skip. Read the file and line in the finding, or rerun without strict mode to confirm the distinction.

A skipped mod installs none of its content. See [Install The Mod](QUICK_START.md#install-the-mod).

## The Handler Never Runs

The mod installed, the moment happens, and nothing fires. In order of likelihood:

- **Wrong directive for the kind.** A filter registered with `mmapi_on` never dispatches. The log carries a one-time warning naming the right directive. Check the hook's kind with `mmapi_hook_kind(name)` or the [Catalog](CATALOG.md).
- **Unknown hook name.** A typo, or a hook this catalog does not declare. It warns once. No MOMI seam dispatches it, though a deliberately custom hook still works when another mod calls the matching dispatcher. `mmapi_hook_exists(name)` confirms only catalog hooks.
- **The registration never ran.** A latch flag was left set, or `register_callbacks` was never called. Log at the registration site to confirm execution reached it.

See [Registration Checks](HOOKS.md#registration-checks).

## The Filter Does Nothing

- **Returning `undefined` every time.** `undefined` means keep the current value. Return the replacement to change it.
- **Treating an in-place filter like a replacement.** `combat.tarball_grid` and `ui.item_node` discard the return deliberately. Mutate the received instance or struct as their catalog pages describe.
- **Another handler wins.** For an override, the first non-`undefined` result wins, and a higher-priority mod may answer first. For a filter, an earlier handler may have already changed the value. See [Override Contention](HOOKS.md#override-contention) and [Registration Options](HOOKS.md#registration-options).

## A Handler Throws

A throwing handler never breaks the game or another mod. The framework skips it per its kind and logs a rate-limited warning attributed to your mod. Find it in the log, and check per-mod error counts with `mmapi_hook_stats()`. See [Error Isolation](HOOKS.md#error-isolation).

## Config Or Save Data Reset

- A config with the wrong `__config_version` is intentionally treated as empty by `mmapi_config_read_valid`; the standard house pattern then materializes the current defaults with `mmapi_config_write`. Migrate before raising the version if old values should survive.
- If an existing config or per-save primary no longer parses, MMAPI tries its adjacent `.bak` last-good copy and logs the recovery. If neither parses, the warning names both files.
- A missing primary means genuinely fresh data. MMAPI does not resurrect a leftover backup in that case.
- Config IO at top-level boot throws. Load from a handler or the first `mmapi_register` drain.

## Preflight One Mod

The CLI can run manifest validation, seam staging, skip checks, lints, and the compile gate without writing the game:

```powershell
dotnet run --project ModsOfMistriaCommandLine -- --lint "C:\path\to\mod-folder" "C:\path\to\pristine-assets.zip" --strict-lints --compile-check require
```

The pristine zip is optional when MOMI can locate the installed game's backup. `--strict-lints` is optional. `--compile-check on` uses a checker when one resolves, `off` disables the pass, and `require` fails when no checker is available.

| Exit | Meaning |
| ---- | ------- |
| `0` | The mod would install. |
| `1` | The mod would be skipped. |
| `2` | The lint could not run, such as a bad path, missing pristine source, or stale seam catalog. |

## It Worked, Then A Game Update Broke It

A game update rewrites engine scripts, so a seam may no longer anchor. On a normal install, a seam-staging failure skips every selected mod that carries GML and proceeds with the content-only rebuild; it does not retain the previous GML layer. `--fail-on-skip` turns that fallback into a hard stop. The fix is a MOMI update carrying a re-checked catalog, not a change to your mod. Catalog contributors can inspect the batch with `--seam-check`; see [Game Updates](SEAMS.md#game-updates) and [Reading Seam-Check Failures](SEAMS.md#reading-seam-check-failures).

## Still Stuck

Turn on the in-game debug agent for live watches, breakpoints, and stepping. See [Debug](DEBUG.md).
