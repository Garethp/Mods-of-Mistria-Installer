// The last-good copy for mod save sidecars.
//
// save_json_file is single-shot: a crash partway through the save event leaves
// a truncated sidecar, try_read_json_file yields undefined, and load_fn receives
// undefined, which is indistinguishable from a fresh save. The mod's per-save
// state silently resets with nothing said, and a player loses progress with no
// error to report.
//
// Every path here is built by the framework's own mmapi_modsave_file and
// mmapi_mod_data_dir, off a stubbed CONFIG_DIRECTORY, so the tests never
// hardcode a layout the code could drift away from.

test_fs_reset();
mmapi_log_set_sinks(false, true);   // file sink on: these tests assert on log lines
mmapi_mod_declare("ms_test", "1.0.0");

global.ms_payload = { n: 1 };
global.ms_loaded = [];

function ms_save()      { return global.ms_payload; }
function ms_load(data)  { array_push(global.ms_loaded, data); }

mmapi_modsave_register("ms_test", ms_save, ms_load);

var save_path = "C:/saves/game-abc123-01.sav";
var sidecar = mmapi_modsave_file("ms_test", "abc123");
var backup = sidecar + ".bak";

// ── A fresh save writes the primary and no backup ───────────────────────────
// There is nothing to back up yet, so a .bak here would describe nothing.
mmapi_emit("save.game_saving", { save_path: save_path });
deq("a fresh save writes the sidecar", try_read_json_file(sidecar, undefined, false).n, 1);
dcheck("...and writes no .bak, because there was nothing to copy",
       file_exists(backup) == false);

// ── The second save copies the old contents to .bak, then writes the new ────
// The ordering is the design: .bak carries generation 1 while the primary moves
// to 2. A .bak written from the new payload would carry nothing worth keeping.
global.ms_payload = { n: 2 };
mmapi_emit("save.game_saving", { save_path: save_path });
deq("the primary advanced to the new payload",
    try_read_json_file(sidecar, undefined, false).n, 2);
deq("the .bak holds the PREVIOUS contents, not the new ones",
    try_read_json_file(backup, undefined, false).n, 1);

// ── A corrupt primary is recovered from .bak on load ────────────────────────
// The case the whole thing exists for. load_fn used to get undefined and the
// mod reset; now it gets the last good state, and the log says so.
test_fs_corrupt(sidecar);
dcheck("the corrupt sidecar still EXISTS (that is what makes it dangerous)",
       file_exists(sidecar) == true);
dcheck("...and reads back as undefined, exactly like a truncated write",
       try_read_json_file(sidecar, undefined, false) == undefined);

global.ms_loaded = [];
mmapi_emit("save.game_loaded", { save_path: save_path });
deq("load_fn was called once", array_length(global.ms_loaded), 1);
dcheck("load_fn received the recovered data, NOT undefined",
       global.ms_loaded[0] != undefined);
deq("...and it is the last good generation", global.ms_loaded[0].n, 1);

var lines = mmapi_log_buffer_lines("ms_test");
var found = 0;
for (var i = 0; i < array_length(lines); i++) {
    if (string_pos("recovered the last-good copy", lines[i]) > 0) { found += 1; }
}
deq("the recovery was logged - a silent recovery is still a silent surprise",
    found, 1);

// ── A corrupt primary must not clobber the good .bak ────────────────────────
// Saving while the primary is corrupt must not copy garbage over the last good
// copy, which is the one thing standing between the player and data loss.
// __mmapi_backup_last_good bails when the old contents do not parse.
test_fs_corrupt(sidecar);
global.ms_payload = { n: 3 };
mmapi_emit("save.game_saving", { save_path: save_path });
deq("the .bak still holds the last GOOD generation, not the corruption",
    try_read_json_file(backup, undefined, false).n, 1);
deq("...and the primary was written normally",
    try_read_json_file(sidecar, undefined, false).n, 3);

// ── A fresh save is not handed a stale .bak ─────────────────────────────────
// The primary is absent, not corrupt: a fresh save, whose contract is that
// load_fn receives undefined. Resurrecting a .bak here would hand the mod
// another save's state.
test_fs_reset();
global.ms_payload = { n: 9 };
mmapi_emit("save.game_saving", { save_path: save_path });     // creates gen 9
global.ms_payload = { n: 10 };
mmapi_emit("save.game_saving", { save_path: save_path });     // .bak = 9, primary = 10
global.__fs[$ sidecar] = undefined;                           // primary deleted, .bak survives
dcheck("the primary is gone but the .bak remains", file_exists(backup) == true);
global.ms_loaded = [];
mmapi_emit("save.game_loaded", { save_path: save_path });
dcheck("an ABSENT primary still loads as undefined (a fresh save stays fresh)",
       global.ms_loaded[0] == undefined);

// ── A throwing save_fn is isolated, and costs no other mod its save ─────────
// run_save loops every registered mod. One mod's save_fn throwing must not stop
// the mods behind it from saving, and the thrower's own previous sidecar must
// survive untouched: a save_fn that threw produced no data, and overwriting a
// good sidecar with nothing is the worst outcome available.
//
// This case was missing until a mutation test went looking: removing the
// try/catch from run_save passed the whole suite.
test_fs_reset();
global.ms_payload = { n: 7 };

function ms_boom_save()      { throw "save_fn exploded"; }
function ms_boom_load(data)  { throw "load_fn exploded"; }
function ms_other_save()     { return { m: 1 }; }
function ms_other_load(data) { global.ms_other_loaded = true; }

// The good mod saves first so it has a sidecar to protect, then a thrower joins.
mmapi_emit("save.game_saving", { save_path: save_path });
deq("the good mod's sidecar exists before the thrower registers",
    try_read_json_file(sidecar, undefined, false).n, 7);

mmapi_modsave_register("boom_mod", ms_boom_save, ms_boom_load);
mmapi_modsave_register("other_mod", ms_other_save, ms_other_load);
var boom_sidecar = mmapi_modsave_file("boom_mod", "abc123");
var other_sidecar = mmapi_modsave_file("other_mod", "abc123");

global.ms_payload = { n: 8 };
mmapi_emit("save.game_saving", { save_path: save_path });
dcheck("a throwing save_fn did not propagate out of run_save", true);
deq("  the mod registered BEFORE it still saved",
    try_read_json_file(sidecar, undefined, false).n, 8);
deq("  the mod registered AFTER it still saved",
    try_read_json_file(other_sidecar, undefined, false).m, 1);
dcheck("  and the thrower wrote no sidecar of its own (no data to write)",
       file_exists(boom_sidecar) == false);

// ── A throwing load_fn is isolated too ──────────────────────────────────────
global.ms_other_loaded = false;
global.ms_loaded = [];
mmapi_emit("save.game_loaded", { save_path: save_path });
dcheck("a throwing load_fn did not propagate out of run_load", true);
dcheck("  and the mod behind it still loaded", global.ms_other_loaded == true);

// ── A failing backup must never cost the real save ──────────────────────────
// The guard's purpose: if the .bak write throws, the save still lands.
test_fs_reset();
global.ms_payload = { n: 1 };
mmapi_emit("save.game_saving", { save_path: save_path });
global.ms_payload = { n: 2 };
test_fs_throw_on_write(backup);
mmapi_emit("save.game_saving", { save_path: save_path });
deq("the primary still saved even though the .bak write threw",
    try_read_json_file(sidecar, undefined, false).n, 2);
dcheck("...and the failure was logged rather than swallowed",
       array_length(mmapi_log_buffer_lines("ms_test")) > 0);
