# Investigation: Custom Music Tracks

**Status:** core question answered for "replace"; "add" remains blocked pending outside input. Branch: `custom-music-investigation`. Native MOMI implementation of the "replace" path (pure C# + P/Invoke into FMOD's own libraries, no third-party tool dependency) is now underway on this same branch - see the `ModsOfMistriaInstallerLib/Audio/` code for the current state.

## Question

Can a mod add new music tracks to Fields of Mistria, or replace existing ones? Raised in Discord (#mod-development, 2026-08-06, Draven): *"Would it be possible to add custom music tracks by replacing the music files for the new crystals with whatever you want"* - never answered there.

## Bottom line

**Replacing existing music works, confirmed end-to-end.** A community tool ([Fmod-Bank-Tools](https://github.com/Wouldubeinta/Fmod-Bank-Tools), GPLv3) patches WAV content *inside* an existing bank in place, keeping its identity/GUIDs/structure untouched. Tested by swapping the farm's Fall-season playlist tracks (`snd_Fall_ChangingWinds_HidehitoIkumo.wav` and `snd_Fall_DanceOfTheLeaves_HidehitoIkumo.wav`) inside `Fall.bank`, replacing the file in `assets.zip` (a true byte replacement, not an addition), and **confirmed audibly in-game** - the new track played on the farm. Not just a clean boot; the actual swapped audio was heard. MOMI has no packaged support for this today - every step here was manual - but the underlying mechanism is now proven, and reading the tool's own source (see "Toward a real MOMI feature" below) clarified how to build it properly.

**Adding genuinely new tracks is still blocked.** Four live tests of "ship a whole new, independent bank" all failed - not a blanket "extra banks rejected," but a specific FMOD architectural constraint: vanilla's audio is **one FMOD project with one shared strings bank** (`Master.strings.bank`) that every content bank (`Farm.bank`, `Spring.bank`, etc.) references into, and a mod's own independently-authored project doesn't slot into that cleanly. Likely a dead end without either extending the game's own existing project (hard without the original FMOD Studio project file) or engine-level cooperation from the devs - genuinely needs outside input, not more local trial and error (Discord questions posted, see below).

Separately, extending the `SONGS` catalog to expose *existing* tracks under new mod-defined names works today, safely, with zero new code - a smaller but real and immediately shippable capability, independent of either question above.

## How the game's music actually works

- Music runs through **Tango**, a compiled GML extension (no GML body - `tango_play`, `tango_name_exists`, the bank loader are all native) wrapping **FMOD Studio**. `fmod.dll`/`fmodstudio.dll` ship with the game.
- Audio data lives in compiled **FMOD `.bank` files** at `assets/assets/audio/`: `Spring.bank`, `Summer.bank`, `Fall.bank`, `Winter.bank`, `Farm.bank`, four Mines-biome banks, `Master.bank`, `Master.strings.bank`. Each has a `.meta.toml` sidecar (`asset_kind = "AudioBank"`, a 16-hex-char `id`, `load_sync`) - the same loose-asset convention the game uses for images.
- Track names (e.g. `"Music/Playlists/Spring Year 2 Plus"`, `"Music/Location Tracks/InnMoreBusy"`) are **FMOD event paths** inside a bank, not filenames.
- Selection logic lives in `gml/scripts/SceneAudioPlayer.gml`'s `in_game_music_selector`: dungeon biome (`DUNGEON.biomes[DUNGEON_BIOME].music`), per-location tracks (`location.music[$ "day"/"night"]`), festival tracks, weather tracks, and seasonal playlists, in that priority order.
- The game already has a **player-facing song-override system**: `SONGS` (a fiddle-loaded catalog, `load_songs()` in `FiddleParsers.gml`) plus `ARI.song_overrides[location_id]` / `ARI.dyn_song_overrides[dyn_index]`, set via an in-game interaction (`GridActions/Interact.gml:688`) - almost certainly the "crystals" Draven meant. This lets a player remap which *existing* track plays where; it doesn't add new audio.
- MMAPI currently hooks exactly one sliver of this: [`audio.music_selector`](../MMAPI/hooks/audio.music_selector.md), a filter on the dungeon-biome branch only. Everything else in `in_game_music_selector` (location tracks, seasons, festivals, the song-override resolution itself) has no hook.

## What MOMI can do today: nothing

- Zero audio-related C# anywhere in `ModsOfMistriaInstallerLib` (confirmed by a full case-insensitive grep for `sound|audio|music|.ogg|.wav|audiogroup|TANGO`; the one hit was an unrelated comment). `ROADMAP.md` has an unchecked `Sounds installer` line and nothing else.
- No generic raw-file passthrough exists either. Every install path in `ModInstaller.RunInstallers` is a hardcoded extension filter (`.png` → `ImageInstaller`, `.toml`/`.meta.toml` → `TOMLInstaller`, `.json` → `JSONInstaller`, `.gml` → GML seam staging, `.mist` → `MISTInstaller`, which is also `string`-based/`ReadAllText`, not byte-safe). `.bank` matches none of them - a mod shipping `assets/audio/Spring.bank` today is silently ignored, not copied anywhere.

## The two sub-problems

**Replace an existing track.** Needs (a) new MOMI support to deliver a `.bank` file at all (byte-safe, unlike `.mist`'s installer), and (b) the replacement bank to contain an event at the *same path* as the one being replaced, so every existing caller (`location.music`, the seasonal switch, etc.) keeps working unchanged. Building that replacement bank means either reverse-engineering the compiled original (FMOD banks aren't encrypted, so extraction tools exist, but it's real work re-sourcing every other event that bank also carries) or accepting a from-scratch rebuild that only contains the one changed event.

**Add a brand new track.** Needs everything above, plus an answer to a question that's closed to GML inspection: does Tango's native bank loader scan `assets/audio/` for banks, or load a fixed list? No GML anywhere calls a bank-loading function - it's entirely inside the native extension. If it's a fixed list, a new standalone bank never loads regardless of what MOMI ships, and "add" collapses back into "replace" (rebuild an existing bank to also contain the new event).

## Experiment

Only way to settle the fixed-list-vs-scan question without FMOD Studio (not installed here, and authoring a real event needs it): duplicate an existing small bank (`Master.strings.bank`, ~40KB) under a new name with a fresh `meta.toml`/id, inject it into the *actually-loaded* archive (`assets.zip` - the loose `assets/` folder alongside it is a stale reference copy the game does not read when `assets.zip` is present), and see what happens at boot.

**Risk/reversibility:** `assets.zip` was backed up before the edit and fully restored after (verified: the injected entries are gone, confirmed working again by relaunching). `assets.bak.zip` (MOMI's untouched pristine backup) was never touched and remains a second line of recovery via a normal reinstall.

### Result: crashed, and the crash itself is the useful data point

Injecting `assets/audio/MomiTest.bank` (a byte-identical duplicate of `Master.strings.bank`, new `meta.toml`/id) crashed the game **on the loading splash screen** - before the title screen renders, the window just closes, no dialog, and critically **no new `error_log.json` entry** (the one present was hours stale, confirmed by mtime, and unrelated - the classic trap already documented from prior MCNPC work: always check the timestamp before trusting that file).

That timing and failure shape both matter:

- **The crash happening at all is the signal.** `Setup.gml` creates `TANGO = new Tango()` and loads audio banks very early in boot, before the title screen. A crash exactly there means the native loader *did something* with the extra file - it didn't just silently skip an unrecognized filename. This argues for "scans the directory" over "loads a fixed list," though it's not airtight proof (a fixed-list loader that also validates the directory's total contents against something could theoretically produce the same symptom).
- **No `error_log.json` entry** means this is a crash below the game's own GML-level error handler - consistent with a fault inside the compiled Tango/FMOD extension itself, not something the game caught and logged gracefully.
- **The likely specific cause weakens how far this generalizes.** The duplicate had *identical internal FMOD GUIDs/string tables* to the already-loaded `Master.strings.bank` - a well-known FMOD failure mode (loading two banks that define the same GUIDs) independent of whether the file was expected. This crash most plausibly demonstrates "loading a bank with colliding GUIDs is fatal," not "any unrecognized bank is fatal." A bank built fresh in FMOD Studio (new unique GUIDs, whether at an existing or a new event path) might load cleanly where this duplicate could not - that's still an open question.

### Updated bottom line

The loader almost certainly does more than consult a hardcoded filename list (a genuine finding), but this experiment couldn't isolate "extra file present" from "extra file has colliding content" as the actual cause, and the failure mode (silent native crash, no log) makes further guess-and-check testing against the live game costly - each iteration risks another unlogged crash with no diagnostic trail. Real progress from here needs a bank with genuinely unique content, which needs FMOD Studio (or a bank-editing tool capable of producing valid unique GUIDs) rather than more byte-duplication experiments.

### Post-crash diagnostics checked, both dead ends

Before doing anything further, checked whether the crash left any trace to analyze:

- **Windows Error Reporting crash dump** (`%LOCALAPPDATA%\CrashDumps`): a `FieldsOfMistria.exe` dump exists but is stale (Aug 8, days before this test) - no fresh dump was generated.
- **Windows Event Viewer Application log**: zero entries for `FieldsOfMistria`/`fmod` around the crash time.

The absence of *both* is itself informative: a true unhandled native exception (access violation, etc.) always gets logged by Windows regardless of app-level handling. Nothing being logged anywhere means this was a clean, deliberate process exit, not memory corruption - consistent with FMOD's bank-load call returning an error on the duplicate GUIDs and the game's own boot code hard-asserting on that result (the same `impossible()`/`assert`-style pattern seen elsewhere in this codebase), terminating before either Windows' crash reporting or the game's own JSON error logger ever run.

## Second experiment: extending the `SONGS` catalog (no bank files involved)

`SONGS` (`global.__songs`, `Setup.gml`) loads from `SONGS = load_songs()`, which does `fiddle_get("songs")` - a plain fiddle TOML file (`assets/fiddle/songs.toml`), the exact same mechanism MOMI already fully supports for quests/items via its existing `TOMLInstaller`. This is the catalog the in-game "Bell Tower Resonator" object's song-selection UI (`song_selection_ui` in `anchor_utils.gml`) reads from - confirmed as the real "music crystal" system, one further step from `obj_tower_resonator.gml`'s `misc_local/select_town_song` option.

Real vanilla schema (`assets/fiddle/songs.toml`), one entry per song:
```toml
[farm_boy]
	name = "Farm Boy"
	icon = "spr_ui_item_song_crystal_farm_boy"
	track = "Music/Crystal Tracks/FarmBoy"
```

Note `name` is a plain display string, not a registered mod loc key - vanilla content apparently keys its own localization tables directly off the English text, and `local_get`'s documented miss behavior (echo the key back) makes this safe even unlocalized.

One relevant nuance for actually **selecting** a song in-game: `song_selection_ui` checks `array_has(ARI.song_unlocks, key)` per entry and calls `.set_soft_locked(!unlocked)` - a not-yet-unlocked song still appears in the list but grayed out with its name replaced by `"???"`. So a new entry installs and is listed either way; actually picking it needs an unlock too (untested - save-data edit or a possible BUGGER command, not pursued here).

**Test**: built a throwaway mod (`mods/momi_music_test/`, manifest + one `fiddle/songs.toml` fragment) adding a single new entry pointing at an *existing* valid track and an *existing* icon sprite (deliberately not touching any bank, to isolate "does the catalog accept a new entry" from every open question about audio files themselves):
```toml
[momi_test_track]
	name = "MOMI Test Track"
	icon = "spr_ui_item_song_crystal_farm_boy"
	track = "Music/Playlists/Spring"
```

**Result: clean success.** Installed through the normal MOMI CLI alongside the other four already-installed mods, no errors. Verified directly in the built `assets.zip` that `momi_test_track` merged in after all the vanilla entries, untouched. Game booted normally (confirmed by the user).

**Conclusion**: the `SONGS` catalog is safely mod-extensible today, with zero new MOMI code, for the "let a mod register a new named choice using an *existing* track" case - a real, shippable capability independent of whether new/replaced audio ever becomes possible. Confirming actual in-game selectability (the unlock piece) is a natural but not-yet-done follow-up.

## Third experiment: a genuinely fresh FMOD Studio bank

Installed FMOD Studio (free tier, fmod.com), built a brand-new throwaway project (`momi_test`) with one event (a placeholder sound), and exported it - producing `Master.bank` (1074 bytes) and `Master.strings.bank` (724 bytes) under `Build/Desktop/`. Being a fresh project, both files carry newly-generated GUIDs with **no relationship to the game's own banks** - this was specifically meant to rule the first experiment's leading "duplicate GUID collision" explanation in or out.

Injected both files into a fresh copy of the live `assets.zip` as `assets/audio/MomiFmodTest.bank` + `.meta.toml` and `assets/audio/MomiFmodTest.strings.bank` + `.strings.meta.toml` (new unique ids, same `asset_kind = "AudioBank"` convention), same backup-first precaution as before.

**Result: crashed again, same shape.** Loading splash screen, window closes, no dialog. Checked immediately afterward: no fresh WER crash dump, no fresh Event Viewer entry (only unrelated `MsiInstaller` events), no touched log file besides `settings.json` - identical silent-failure signature to the first crash. Restored `assets.zip` from backup; confirmed working again by relaunch.

**This meaningfully changes the leading explanation.** GUID collision can no longer explain this crash - these GUIDs were never seen by the game before. The two remaining candidates:

1. **The loader genuinely has a fixed, hardcoded list of expected bank filenames**, and encountering any file outside that list is fatal (not gracefully skipped) - the "scans vs. fixed list" question resolves toward "fixed list, and mismatches crash" rather than "scans and safely ignores extras."
2. **Loading a second, independently-authored *strings* bank specifically is the problem**, separate from whatever happens with a second content bank alone. FMOD Studio's own API treats the strings bank as special (the GUID-to-path lookup table); loading two unrelated strings banks in one Studio system may not be supported the way loading two ordinary content banks (e.g. DLC-style) can be, regardless of GUID uniqueness. Both experiments so far always injected a `.strings.bank` alongside the content bank, so this hasn't been isolated yet.

A test that would distinguish these: inject *only* a content bank (no accompanying `.strings.bank`) built from a project that reuses no vanilla paths, and see whether it crashes the same way.

## Breakthrough: real diagnostics via stdout/stderr capture

A Discord tip (pixie) suggested piping the game's own console output instead of relying on `error_log.json`/WER/Event Viewer (all of which had come up empty on every crash so far). Simple `2>&1 | Write-Host` piping returned nothing usable (the process appears to exit before a simple pipe can capture it), but `Start-Process -RedirectStandardOutput -RedirectStandardError -Wait` against `FieldsOfMistria.exe` directly works and captures everything.

This also revealed the actual engine architecture: Fields of Mistria runs on a custom Rust engine (internally "Maybe"/"Mistria" - `sdl-backend`, `mwr`/`mwe` crates for rendering, `fabricator` for GML compilation/execution - the same crate family MOMI's own `momi-gml-check` compile-gate tool is built on), not stock GameMaker Runner. "Tango" is a Rust module (the `fmod-audio` crate) wrapping real FMOD, exposed to the GML layer as native functions.

Re-ran the third experiment (same `MomiFmodTest.bank` + `.strings.bank` pair) with output captured. The real error, previously completely silent:

```
Error:
   0: FMOD_Studio_Bank_GetLoadingState: The specified bank has already been loaded. (70)
Location:
   fmod-audio/src/lib.rs:106
```

This is a precise, specific FMOD state conflict, not a generic rejection - and it reframes the whole investigation. The most likely cause: this test shipped **both** `MomiFmodTest.bank` (content) and `MomiFmodTest.strings.bank` as two independently-declared `AudioBank` assets. FMOD very plausibly auto-loads a bank's strings dependency internally as part of loading the content bank; if the engine's boot loop then separately, explicitly loads the strings bank too (because it was independently declared via its own `meta.toml`), FMOD correctly reports it as already loaded. That would make this **a redundant-declaration bug in the test setup, not evidence that new banks are rejected** - directly matching candidate 2 from the prior section, now with hard evidence instead of a guess. Candidate 1 ("fixed list, any extra bank is fatal") is looking less likely, since this specific, narrow FMOD error wouldn't be the natural failure shape for "unrecognized file, reject outright."

## Fourth experiment: content bank alone, and the real architectural picture

Re-injected just `MomiFmodTest.bank` (no `.strings.bank`, no second `meta.toml`) and launched with output captured. Different error this time:

```
Error:
   0: FMOD_Studio_Bank_GetLoadingState: An error occured in the FMOD system. Use the logging version of FMOD for more information. (28)
Location:
   fmod-audio/src/lib.rs:106
```

Same source line, different failure - a generic FMOD-internal error rather than the specific "already loaded" one. Both attempts failed, just differently, which rules out the simple "just don't redundantly declare the strings bank" fix.

**What actually explains both results**: re-checked the vanilla bank list (see "How the game's music actually works" above) - there is exactly **one** strings bank total, `Master.strings.bank`, shared by every content bank (`Farm.bank`, `Spring.bank`, the Mines banks, etc. each reference into that same shared table; none has its own). That's standard FMOD Studio structure: one project, one strings ecosystem, many content banks within it.

The FMOD Studio test project built for this investigation is a **separate, independent project** - its own self-contained `Master.bank`/`Master.strings.bank` pair, unrelated to the game's own project despite being renamed on disk. That reframes both crashes as the same underlying issue, not two different bugs:
- **Both files present** (error 70): FMOD auto-resolves the content bank's dependency on its own strings data internally when loading it; the engine's explicit separate load of the strings bank then finds it already loaded.
- **Content bank alone** (error 28): without its own strings data ever loaded, FMOD can't resolve the bank's internal references at all - a vaguer failure, consistent with "the system doesn't know what to do with this."

Either way, this engine's FMOD setup expects one strings-bank ecosystem, and a second, independently-authored one - whole or partial - doesn't slot in cleanly. This isn't "extra banks are rejected outright" (the earlier, blunter theory); it's a specific structural mismatch between "a mod's own separate FMOD project" and "the game's single existing FMOD project." Extending the *game's own* project (adding events that reference the existing `Master.strings.bank`) is the more promising shape for a real fix, but doing that from outside requires either the original FMOD Studio project file (not available) or engine-side cooperation - not something resolvable by dropping files from here. Stopping live-game experimentation at this point; the remaining open questions are genuinely better answered by someone who knows the engine/FMOD setup firsthand than by more local trial and error.

## Questions posted to Discord (2026-08-11)

Asked in #mod-development, aimed at `annanomoly`/`Felix`/`Garethp` or anyone who's touched Tango before:

1. Does Tango's bank loader scan `assets/audio/` for `.bank` files at boot, or load a fixed list?
2. Has anyone tried adding or replacing a `.bank` file manually and seen what happens?
3. Does Tango resolve events by FMOD path string only, or does it also depend on the original project's GUIDs?
4. Is Tango a custom in-house wrapper or a known FMOD-for-GameMaker integration (vendor docs might exist even though the compiled GML functions have no visible body)?
5. Is there any way to get FMOD/Tango load errors into a log instead of a silent crash-and-close? (Independently confirmed necessary - see the diagnostics dead-end above.)
6. Does the in-progress MMAPI Extensions work touch audio/custom sound assets at all?

### Follow-up round, post-breakthrough (2026-08-11)

Posted after getting real diagnostics and narrowing the failure to a specific FMOD architecture mismatch (see Fourth Experiment above). Superseded questions 1 and 5 above (both now answered by direct evidence); 2, 3, 4, 6 still open.

1. Does the engine's FMOD Studio System get initialized in a way that only supports one project's worth of banks/strings, or can a second independent bank-set coexist if loaded a specific way?
2. Is there any supported path for a mod to add new events into the game's own existing project (referencing the existing `Master.strings.bank`) rather than shipping a separate one - or does that fundamentally require the original `.fspro` project file?
3. Does `FMOD_Studio_Bank_GetLoadingState` error 70 ("already loaded") or error 28 (generic) ring a bell for anyone who's poked at this before?
4. (Repeated) Does the in-progress MMAPI Extensions work touch audio at all?

Also shared as a discovery, not a question: `Start-Process -RedirectStandardOutput <file> -RedirectStandardError <file> -Wait` against `FieldsOfMistria.exe` directly gets real crash diagnostics, where `error_log.json`, Windows Error Reporting, Event Viewer, and a plain `2>&1 | Write-Host` pipe all came up empty.

## A different, more promising approach: in-place bank patching

A community tool, [Fmod-Bank-Tools](https://github.com/Wouldubeinta/Fmod-Bank-Tools) (Qt GUI, Extract/Rebuild), does something fundamentally different from every experiment above: it extracts the WAV audio embedded in an **existing** `.bank` file, lets you swap those WAVs for new ones, and rebuilds the *same* bank file - patching audio content in place rather than constructing an independent FMOD project. This sidesteps the core problem every prior experiment hit (a mod's own separate project/strings-bank ecosystem not slotting in next to the game's existing one), because the bank's own identity, GUIDs, and event structure never change - only the embedded audio bytes do.

From the README:
- Explicitly excludes `Master.bank`/`Master.strings.bank` ("they don't have audio in them") - consistent with what this investigation already established about those two files.
- Replacement WAVs must match the original's **file type, bitrate, and duration same-or-less** - almost certainly the source of the "same size or smaller" comment from Discord. This reads as a straightforward in-place binary-patch constraint (can't grow embedded audio past its originally allocated slot in the bank's internal layout), unrelated to `assets.zip` itself (a real zip, freely resizable, as this session already demonstrated repeatedly).
- Handles encrypted banks too (not expected to be relevant here, vanilla banks in this game don't appear to need it, untested).

**What this can and can't do:** replaces the audio *content* of an existing, already-expected track - real progress on the "replace" half of the original question, with a plausible mechanism this time. It cannot add a brand-new named track (there's no existing WAV slot to replace), so "add new music" likely still needs the harder, unresolved path (extending the game's own strings-bank ecosystem, still an open question for the Discord thread).

**Next step (not yet run):** a Qt GUI tool - same hand-off shape as the FMOD Studio experiment. Extract a real content bank (e.g. `MinesUpper.bank`, the smallest at ~4.9MB, for a fast test), replace one WAV with a new same-or-shorter one, rebuild, hand the rebuilt `.bank` back for injection (overwriting the original bank's bytes in a copy of `assets.zip`, same backup-first precaution as every prior test) and testing via the now-working `Start-Process` diagnostic capture.

### Result: clean success - the first working audio modification in this investigation

Extracted `MinesUpper.bank` with Fmod-Bank-Tools, replaced one of its three WAVs (`snd_DigDeeper_CattonArthur.wav` / `snd_LetsGetToWork_CattonArthur.wav` / `snd_RestAWhile_CattonArthur.wav`), rebuilt. The rebuilt bank came out larger overall (13.75MB vs. the original 4.88MB, likely a re-encoding/compression difference from the tool, not a violation of the tool's actual constraint which is per-WAV duration) - flagged as a risk but not something that turned out to matter.

Injected via a **true replacement** this time, not the append trick used for every earlier addition test: rewrote the whole `assets.zip`, copying every entry unchanged except swapping `assets/audio/MinesUpper.bank`'s bytes for the rebuilt version (Python's `zipfile` has no in-place single-entry replace; a full rewrite is the correct way to guarantee every reader sees exactly one entry per path, not a same-path append some tools might resolve differently than others). Verified the resulting file size delta matched the expected difference exactly (+8.87MB), confirming nothing else in the archive was disturbed.

Launched with the `Start-Process` diagnostic capture: **clean boot.** `Completed Setup in 1.144711s!`, reached `Entering main loop`, stayed running normally until manually stopped after 15s (not a crash - the test harness force-stops on a successful boot to avoid leaving the process running unattended). Zero FMOD errors in stderr, first time any bank modification test has produced a clean run.

**This confirms in-place bank patching is a real, working path for replacing existing audio content**, distinct from and much more promising than every "ship an independent bank" experiment above. Not yet confirmed at that point: whether the *specific* replaced sound effect actually plays with the new audio in-game (boot-only test).

### Second replacement, to test generalization: farm music

Wanted a clearly audible test (music, not a short SFX bark) and to confirm the approach isn't a one-off fluke. Checked the fiddle location data (`assets/fiddle/locations.toml`): `[farm]` has no `music` field, so farm music is just whatever the current seasonal playlist track is - confirmed against the save (Fall) that this means `Fall.bank`.

Extracted `Fall.bank` with Fmod-Bank-Tools - real credited tracks this time (composer Hidehito Ikumo): `snd_Fall_ChangingWinds_HidehitoIkumo.wav`, `Fall - Changing Winds (Extended)_HidehitoIkumo.wav`, `snd_Fall_CrowsInAClearSky_HidehitoIkumo.wav`, `snd_Fall_DanceOfTheLeaves_HidehitoIkumo.wav`, plus a large set of `snd_fall_day_*`/`snd_fall_night_*` ambience (birds/crows/crickets/owls - a separate system, not music). Replaced `snd_Fall_ChangingWinds_HidehitoIkumo.wav` (matches the base `Music/Playlists/Fall` event name most directly), rebuilt (24.2MB, up from 9MB original - consistent with the same re-encoding size growth seen on the first test).

Replaced `Fall.bank` in the *same* `assets.zip` that already had the patched `MinesUpper.bank` - both replacements live simultaneously. **Clean boot again**: `Completed Setup in 1.125412s!`, `Entering main loop`, zero FMOD errors. Confirms this isn't a one-off - two independent bank replacements coexist fine.

First relaunch landed on `DanceOfTheLeaves` (the untouched track) rather than the swapped `ChangingWinds` - confirmed `Music/Playlists/Fall` is genuinely a rotating pool, not one fixed track, matching the "Playlists" naming. Replaced `snd_Fall_DanceOfTheLeaves_HidehitoIkumo.wav` too (on top of the already-swapped `ChangingWinds`, both in the same rebuild) so either rotation choice would be covered, rebuilt again (29.6MB), replaced `Fall.bank` a second time, clean boot again.

**Confirmed audibly in-game**: the swapped track played on the farm. This is the real proof, not just a clean boot - **in-place bank replacement is a fully confirmed, working path for replacing existing game music.**

## Toward a real MOMI feature: reading Fmod-Bank-Tools' actual source

Every step above was manual (GUI tool + hand-written Python zip surgery). To turn this into something a mod author can use without any of that, read the tool's actual C++ source (`bank_extract.cpp`, `rebuild_worker.cpp`, `bank_header.h`) rather than just its README.

**Correction to the "same size or smaller" constraint**: it's not a hard format limit. `rebuild_worker.cpp`'s `bankRebuild()` fully recomputes the bank's `SNDH` offset/size table and reflows every subsequent embedded audio chunk when writing the rebuilt file - the container format supports growing or shrinking freely (`sndh_fsbOffset[i+1] = sndh_fsbOffset[i] + fsbSizes[i] + snd_buffer[i+1] + 8`, recomputed each rebuild). This matches what was actually observed: every rebuilt bank in this investigation came out *larger* than the original and still worked. The README's guidance is most likely conservative advice for cases with tight gameplay-timing dependencies (a bark synced to an animation frame, say), not a technical wall for music.

**The actual format**, confirmed by reading the parser: a `.bank` file is a RIFF/`FEV ` container with `PROJ`→`BNKI` chunks; an `SNDH` chunk lists (offset, size) pairs, one per embedded `FSB5` sub-bank; each `FSB5` can itself bundle multiple named subsounds (this is why `MinesUpper.bank` extracted to one `.fsb` containing all three `CattonArthur` barks together, and why rebuilding requires *every* original WAV in that group to be present, not just the one being changed - `FSBank_Build` regenerates the whole subsound group from scratch each time). Plain, portable binary parsing - genuinely easy to reimplement.

**The one real dependency**: actually re-encoding audio into the `FSB5` format calls `FSBank_Build`, part of Firelight's proprietary FMOD SDK (`fsbank64.dll` + `libfsbvorbis64.dll`, both already bundled in the tool's own public releases under FMOD's redistribution terms). This is the one piece that can't just be reimplemented from scratch - it needs to be called, not replaced.

**Licensing check**: Fmod-Bank-Tools is GPLv3. MOMI's own `LICENCE.txt` is also GPLv3. No conflict porting or adapting its code.

**Proposed shape for a real feature**: fork/strip Fmod-Bank-Tools' core extract/rebuild logic (drop the Qt GUI, keep the container parsing) into a small headless CLI helper, bundled the same way `momi-gml-check.exe` already is - a native binary the C# installer shells out to, invisible to mod authors. A mod would ship a plain WAV plus a small manifest declaring which bank and which original track name it replaces; MOMI's installer would, at install time: locate the target bank in the pristine backup, extract the full subsound group the target belongs to, overlay the mod's replacement WAV by matching filename (keeping every other WAV in that group untouched), rebuild via the bundled helper, and splice the result into the built `assets.zip` - the same shape as every other asset kind MOMI already handles, with the FMOD/bank-format complexity fully hidden.

## Phase 1 built and proven: `tools/audio-replace`

The proposed shape above (pure C# + P/Invoke, no forked native helper - see the implementation plan this branch carried) is now real code, not just a proposal: [`ModsOfMistriaInstallerLib/Audio/`](../../ModsOfMistriaInstallerLib/Audio) reads/writes the `.bank` container, decodes existing subsounds via the real FMOD Core API, and re-encodes via the real FSBank API. A small CLI wraps it end to end - see [tools/audio-replace/README.md](../../tools/audio-replace/README.md) for how to use it.

Verified against the live game, not just in isolation: the C# pipeline decoded the real `Fall.bank`, re-encoded all 29 subsounds, rebuilt the bank, and the game loaded and played it correctly - confirmed audibly (`It sounds like the normal fall music`).

## Phase 2 built and proven: `momi/audio/` mods

The mod-facing format is real too: `momi/audio/*.toml` + `AudioInstaller`, wired into `ModInstaller.RunInstallers`. A mod author ships a plain WAV and a manifest entry; MOMI's installer does the decode/replace/re-encode/splice at install time - see [docs/AUDIO_REPLACEMENT.md](../AUDIO_REPLACEMENT.md) for the manifest schema and [`AudioInstaller.cs`](../../ModsOfMistriaInstallerLib/Installer/AudioInstaller.cs) for the implementation.

Verified through the real install path, not just unit tests: built the actual CLI (`ModsOfMistriaCommandLine`) with `AudioInstaller` wired in, ran it against the live game with a real test mod (`momi/audio/*.toml` + WAV) sitting in the real `mods/` folder alongside the other installed mods, confirmed the swap landed in the resulting `assets.zip`, and confirmed a clean boot. Caught and fixed one real gap along the way: FSBank silently drops embedded subsound names when built from in-memory buffers rather than files (`FsBankNative` already worked around this - see its class comment), and a first pass at hooking it up to the CLI surfaced that the CLI's pre-install validation pass is currently disabled (`Standalone.cs:39`, pre-existing, not introduced here), so a broken `wav` path fails silently rather than printing an error - worth fixing separately, not blocking here since the install still simply skips the broken entry.

Native FMOD DLLs (`fmod64.dll`/`fsbank64.dll`/`libfsbvorbis64.dll`) now bundle into both shipping projects via the same content-item pattern `momi-gml-check` uses - see [`tools/fmod/PACKAGING.md`](../../tools/fmod/PACKAGING.md). Redistribution terms for those proprietary binaries are still unresolved, flagged there - not a blocker for building/testing, but is one before a real public release.

## A proper test: real audio, composition, and malformed manifests

The first CLI pass above proved the mechanism; a follow-up round specifically tried to break it in ways a real mod author would hit:

- **Real replacement audio, not an internal swap.** Installed a mod replacing Fall's `ChangingWinds` with a genuine external track (a 152s stereo battle theme, 44.1kHz - a different sample rate than any vanilla track, so the format itself is proof the real file landed, not just a plausible-sounding one). Confirmed audibly in-game (`I hear the battle music`).
- **Two mods replacing different tracks in the same bank.** One mod touching `ChangingWinds`, a second touching `DanceOfTheLeaves`, both in `Fall.bank`, installed together. Both landed simultaneously - confirmed by reading the resulting bank back with `tools/audio-replace list` and checking both tracks' durations changed independently, proving `AudioInstaller`'s "read the current state via `IFileModifier`" composition rule actually holds across mods, not just in theory.
- **A mod with only broken entries** (a typo'd track name, a missing `bank` field, a missing `wav` field) installed in 8.6ms doing nothing, while the other mods around it installed completely normally - confirming a broken audio mod can't take down an install, before the deeper native-layer version of that same question came up below.

Also discovered along the way: the game doesn't always play the same track for a given season - `Music/Playlists/Fall` is an FMOD playlist event that picks randomly among four tracks internally, invisible to and uncontrollable from GML or this feature. Documented in [docs/AUDIO_REPLACEMENT.md](../AUDIO_REPLACEMENT.md) with a worked multi-track example (replace the whole pool, not just one track, to guarantee hearing a swap) - verified the quoted-TOML-key syntax needed for the one track name with spaces actually parses correctly with Tomlyn before writing it down, rather than assuming.

A committed example mod ([`examples/audio-replacement-demo`](../../examples/audio-replacement-demo)) and a full [track catalog](../AUDIO_TRACKS.md) (every replaceable name across all 11 banks, 1,438 tracks, generated via `tools/audio-replace list` rather than hand-written) came out of this round too.

## Verified against a real published build

Every test up to this point - CLI runs, the proper-test round above - used a Debug build, where `dotnet build` drops the FMOD DLLs as loose files right next to the exe. That's a meaningfully easier environment than what an actual MOMI release is: a single self-contained published exe with bundled content extracted at launch. Publishing one for real, copying just that one exe into an empty folder (nothing else beside it, to rule out coincidentally finding a stray DLL), and running it against the live game surfaced two real bugs Debug testing had been masking:

1. **`FsBankNative` never preloaded `libfsbvorbis64.dll` itself.** Only test helpers and `tools/audio-replace` did that preload, not the actual shipped class. `fsbank64.dll` loads its Vorbis encoder plugin via its own internal library lookup, which doesn't necessarily search the same paths .NET's P/Invoke resolution does - in the real published build this failed with `FSBANK_ERR_ENCODER_FILE_NOTFOUND` even though `fmod64.dll`/`fsbank64.dll` themselves resolved fine. Fixed by preloading it from `AppContext.BaseDirectory` inside `FsBankNative` itself, defensively (a test that already preloaded it from a different directory shouldn't be broken by this one failing to find it at that path).
2. **More seriously: `AudioInstaller` had no protection against the native FMOD layer being unavailable at all** (no bundled DLLs). `ModInstaller`'s per-mod loop has no exception isolation of its own - so an unhandled `DllNotFoundException` from one mod's audio replacement propagated all the way up and aborted the *entire* install mid-run. Reproduced this for real: published a build with no FMOD DLLs bundled (temporarily set aside `tools/fmod/dist`), ran it against the live game with the audio demo mod still installed alongside five others, and watched `assets.zip` come out reverted to bare vanilla (122,403 entries, matching the untouched backup) - every other already-installed mod's content gone, not just the audio one skipped. Fixed by catching per-bank in `AudioInstaller` and recording a validation error instead of letting the exception propagate.

Both fixes verified against real isolated single-file publishes again after the fix, not just unit tests: with the FMOD DLLs bundled, the swap works and all 6 mods install (exit code 0); with them absent, the audio mod's own replacement is skipped but the other 5 mods still install correctly and `assets.zip` comes out with all their content intact, not reverted. The live game was backed up before every one of these publish tests and restored to byte-identical afterward.

## A new limit: long replacements get cut off partway through

Testing a much longer replacement track (7:00, swapped into `snd_Fall_ChangingWinds_HidehitoIkumo` which is normally 2:00) surfaced a new failure mode: the game plays it, but audibly stops and advances to the next playlist track partway through, well before the replacement's real end.

**Three attempts, three FSBank build-flag configurations, one consistent result:**

| Attempt | Build flags | Where it cut off (as heard) |
|---|---|---|
| 1 | default (matches Fmod-Bank-Tools' own defaults) | ~2:50 (rough estimate) |
| 2 | `FSBANK_BUILD_NOGUID` added | 2:43 |
| 3 | `FSBANK_BUILD_NOGUID` \| `FSBANK_BUILD_DISABLESYNCPOINTS` | 2:46 |

Two real hypotheses were tested, not just guessed at:

- **Stale runtime header cache.** fsbank.h documents that a non-null FSB GUID enables runtime header caching; since this feature rebuilds the same subsound slot with different content on every install, that caching is actively wrong for this use case regardless of this bug. `FSBANK_BUILD_NOGUID` disables it. Kept (harmless, arguably more correct) but alone did not fix the cutoff.
- **FSBank's own automatic sync points.** By default FSBank analyzes audio during encoding and embeds its own sync points; if FMOD's playlist logic advances on reaching an embedded sync point rather than true end-of-file, that would explain an early cutoff. `FSBANK_BUILD_DISABLESYNCPOINTS` disables that analysis. Kept alongside `NOGUID`, but combined, still did not fix it.

The result across all three attempts clustered around the same ~2:43-2:50 mark despite meaningfully different encode configurations - evidence *against* anything in our own encoding, and toward something outside the audio data entirely.

### What's actually in the bank file, beyond the SNDH/SND audio blob

Walked the *full* top-level chunk list of both `Fall.bank` and `Master.bank` (not just searching for `SNDH` the way `FmodBankFile` does) - both share the identical structure: a long run of `LIST` chunks first, *then* `SNDH`/`STDT`/`STBL`/`HASH`/`DEL `/`MUTE`/`REFI`/`PLAT`/`SND ` (the small build-metadata chunks; `STBL` is empty - 0 bytes - in both banks, ruling out an earlier guess that it might carry per-track metadata).

Scanning those `LIST` chunks for readable ASCII turned up real FMOD Studio construct names: `EVTS`/`EVNT` (Events), `TMLN`/`TRAN` (Timeline/Transitions), and - most relevantly - **`PLST`/`PLSTH`** (Playlist) and **`MUIT`/`MUIS`/`MUIB`** (almost certainly "Multi Instrument," FMOD Studio's actual construct for an instrument that randomly selects among several assigned sounds each trigger - exactly the behavior already confirmed for `Music/Playlists/Fall`).

Also checked the GML side again, specifically for anything duration/trigger-region related: nothing. The two files that matched a `max_length` search were unrelated controller-rumble code. This reconfirms `SceneAudioPlayer.gml` never manages *which* track within a playlist is playing or for how long - that's entirely inside FMOD's compiled event data, invisible to GML.

**Conclusion**: the per-track playback-length control almost certainly lives inside the compiled Multi Instrument/Playlist event data in those `LIST` chunks - a proprietary, versioned FMOD Studio binary format, structurally separate from the simple FSB5-in-a-container format this feature already understands. Unlike the container format, there's no GPLv3 (or any open-source) reference implementation to port logic from here - Fmod-Bank-Tools never needed to touch event data, only raw audio. Reverse-engineering it well enough to safely locate and patch a "max instrument length" field, without corrupting parameter references, transitions, or mixer routing elsewhere in the same compiled event, would be a substantially larger and riskier undertaking than everything built so far - closer in kind to the already-deferred "add a wholly new track" problem than to a bug in this feature.

### Going deeper: proving it isn't our patch, and finding where it likely lives

Went further than reasoning about this - actually verified it, and actually looked at the bytes.

**Byte-for-byte proof our patch never touches this data.** Compared the live, heavily-rebuilt `Fall.bank` (post pain.wav swap) against the pristine backup: the entire event-graph region - everything from the `FEV ` magic up to where `SNDH` starts, 15,800 bytes - is **byte-for-byte identical**, once the one legitimately-different field (the RIFF container's own top-level size, which necessarily changes when the file grows) is excluded. `FmodBankFile.ReplaceGroup` only ever writes from `Groups[0].Offset` onward by construction, but this confirms it directly rather than trusting the code reading. Whatever governs this behavior is reading data our tool has never modified, on every single test.

**Chunk-level structure.** Walked the *full* top-level chunk list of both `Fall.bank` and `Master.bank` (both share the identical shape). Found four repetitions each of `TMLN`/`TLNB` (Timeline), `TRAN`/`TRNS` (Transition), and `MUIT`/`MUIB` (Multi Instrument) - matching the four tracks in the Fall playlist suspiciously well. Targeted searches for the four tracks' own known lengths, encoded as float32 seconds, int32 milliseconds, or int32 samples-at-48kHz, found no exact matches anywhere in that region. Hex-dumping the `MUIT` blocks directly turned up a repeating 4-byte pattern that decodes cleanly to `8.333...` in two blocks and `33.333...` in the other two (`100/12` and `100/3` - plausible weighted-selection percentages or automation-curve keyframes, not a duration field).

This is the point actual reverse-engineering stopped, deliberately: without a documented spec or an existing open-source parser for FMOD Studio's compiled event format (unlike the container format, which had one), continuing past pattern-matching into blind edits risks corrupting cross-references, transitions, or mixer routing this project has no way to validate before a real in-game test - a fundamentally different risk profile than the well-understood, already-extensively-tested SNDH/SND container patching this feature already does safely.

### The real test: a non-playlist track has the same problem, differently

Swapped the same long file into `snd_fall_day_bed` too - a single ambient loop, not part of any `Music/Playlists/*` construct (`SceneAudioPlayer.gml`'s `in_game_ambience_selector` selects ambience directly, by location and day/night, never through a playlist). With the music channel muted to isolate it: **the ambient track plays, but starts looping after ~30 seconds** - not skipping to something else the way the playlist track does, but restarting from the beginning.

The vanilla `snd_fall_day_bed` is **27.4 seconds**. That's a strikingly close match to the observed ~30s loop point - much tighter than the playlist case's own (real but looser) correlation to `ChangingWinds`'s original 2:00.

**Working theory, now with two independent, differently-shaped confirmations**: the game (via the compiled FMOD event data, proven above to be something this feature's patching never touches) references each track's own *originally authored* length - not anything derived from whatever audio is actually in the bank - to decide when to act. For a Multi Instrument/playlist construct, that action is "stop and advance to the next pick." For a plain looping instrument, it's "loop back to the start." Both are consistent with the same underlying fact: **the compiled event only ever expects (and only ever schedules for) the vanilla track's own length**, regardless of what audio the bank itself actually contains.

### Measured directly, not just inferred: FMOD's own Studio API

Went one step further than binary archaeology: the FMOD *Studio* API (a separate, higher-level, publicly documented API from the Core API this feature already uses - `fmodstudio.dll`, already shipped alongside the game's own `fmod.dll`) exposes `Studio::EventDescription::getLength()`, whose own documentation says exactly what was suspected: *"the length of the timeline... the largest of any logic markers, transition leadouts and the end of any trigger boxes."* A small standalone P/Invoke probe - bindings verified against a real working C# FMOD wrapper project and FMOD's own generated docs, not guessed - loaded `Master.strings.bank` (required for path-based lookups), `Master.bank`, and `Fall.bank` through the real Studio API and queried it directly:

- `event:/Ambience/FallDayRandom` → **27,363 ms**. Vanilla `snd_fall_day_bed` is 27.4s. Effectively an exact match - no longer "strikingly close," now confirmed.
- `event:/Music/Playlists/Fall` → **124,000 ms**. Close to vanilla `ChangingWinds`'s 2:00 (120.0s), but short of the ~2:43-2:50 actually observed in-game. The gap is plausibly the "transition leadout" the API docs call out explicitly - a crossfade that keeps the old track audibly playing for some time after the nominal timeline length while the next pick fades in, which would also explain why the perceived cutoff point wasn't perfectly reproducible across the three earlier build-flag attempts.

This is hard confirmation, not inference: the compiled event genuinely carries a fixed length independent of the audio content, retrieved through FMOD's own official API rather than pattern-matched out of raw bytes. It does not, however, open an editing path - `getLength()` is a read-only query against an already-compiled, already-loaded bank; the Studio API has no corresponding setter, and actually changing it would need FMOD Studio (the authoring application) and the original `.fspro` project source, which doesn't exist for this feature to reach.

### Practical takeaway

Not narrow to playlists after all - this affects both constructs tested, just differently:

- **Music (playlist) tracks**: a much longer replacement gets cut short and the playlist advances early.
- **Ambient (single-instrument, looping) tracks**: a much longer replacement gets cut short and loops from the start early.

In both cases, a replacement *close to* the original track's length should be unaffected (and everything shorter clearly works fine, per every other successful test this session). A replacement meaningfully *longer* than the track it replaces will not play to completion. Fixing that for real would mean changing the compiled event's own timeline length, and the only way to do that is FMOD Studio (the authoring application) plus the original `.fspro` project source - not something reachable by patching bank bytes, however well understood, since the value is read at load time from data this feature has no source project to regenerate. Documented as a real, confirmed, structural limitation, not a bug to keep chasing.

## Open questions for later

- **Can a mod extend the game's own existing strings-bank ecosystem** (append/reference into `Master.strings.bank`'s namespace) rather than shipping an independent second one? This is the crux of whether "add new tracks" is fixable at all without engine cooperation - genuinely needs someone who knows the FMOD/engine integration, not more local trial and error.
- If replacement banks need re-sourcing every other event in that bank, is a smaller/more granular bank split (per-track banks, not per-season) something worth asking the MOMI/MMAPI maintainers about, versus modders always inheriting that cost?
- ~~Does replacing the *bytes* of an existing, already-expected bank filename work, and does the swapped audio actually play?~~ **Confirmed yes, fully.** Tested on both a short SFX bark (`MinesUpper.bank`) and real music-length tracks (`Fall.bank`, two separate WAVs), boots clean and plays audibly in-game. Not yet tested: whether this holds for *every* bank (e.g. the Mines biome banks, or `Master.bank` itself), or whether there's a practical limit on how many/how large a set of simultaneous replacements can be shipped.
- Worth coordinating with `annanomoly` (MMAPI dev) given her in-progress "MMAPI Extensions" work already touches adjacent systems (see the engine-update Discord digest from this branch's sibling investigation, `custom-npc-id-registration`).
- Does granting a `SONGS` entry's unlock (`ARI.song_unlocks`) actually make it selectable end-to-end in the Bell Tower Resonator UI? Not yet tested.
- Whether `momi_music_test` should be deleted from the live `mods/` folder once this investigation wraps, or left as a working example - currently still installed.
- The `Start-Process -RedirectStandardOutput/-RedirectStandardError -Wait` technique (see Breakthrough section) is now the standard way to get real diagnostics from this game rather than relying on `error_log.json`/WER/Event Viewer, all three of which came up empty on every crash here - worth remembering for future investigations on this branch and elsewhere.
