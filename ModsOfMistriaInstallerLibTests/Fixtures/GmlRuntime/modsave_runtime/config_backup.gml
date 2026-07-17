// The last-good copy for mod configs. Same failure, same fix and same shared
// helpers as the save sidecars: a corrupt config used to silently become {},
// which reads to a mod as every setting being back to default, with nothing
// said.

test_fs_reset();
mmapi_log_set_sinks(false, true);

var path = mmapi_config_path("cfg_test");
var backup = path + ".bak";

// ── A first save writes the config and no backup ────────────────────────────
mmapi_config_save("cfg_test", { volume: 5 });
deq("the config saved", mmapi_config_load("cfg_test").volume, 5);
dcheck("no .bak yet - there was nothing to copy", file_exists(backup) == false);

// ── The second save backs up the old config first ───────────────────────────
mmapi_config_save("cfg_test", { volume: 9 });
deq("the config advanced", mmapi_config_load("cfg_test").volume, 9);
deq("the .bak holds the PREVIOUS config", try_read_json_file(backup, undefined, false).volume, 5);

// ── A corrupt config recovers from .bak instead of resetting to {} ──────────
// mmapi_config_load used to return {} here and every mmapi_config_get fell
// through to its default, so a player's settings quietly reverted.
test_fs_corrupt(path);
var cfg = mmapi_config_load("cfg_test");
dcheck("the corrupt config did not read as an empty struct", cfg[$ "volume"] != undefined);
deq("...it recovered the last-good value", cfg.volume, 5);

var lines = mmapi_log_buffer_lines("cfg_test");
var found = 0;
for (var i = 0; i < array_length(lines); i++) {
    if (string_pos("recovered the last-good copy", lines[i]) > 0) { found += 1; }
}
deq("the recovery was logged", found, 1);

// ── An absent config is still {} ────────────────────────────────────────────
// The contract for a mod that has never saved: it must not inherit a stale .bak.
test_fs_reset();
var fresh = mmapi_config_load("cfg_test");
dcheck("an absent config is an empty struct", fresh[$ "volume"] == undefined);

// ── A corrupt config with no .bak still degrades to {} and warns ────────────
// The old behaviour, still correct when there is nothing to recover.
test_fs_reset();
mmapi_config_save("cfg_test", { volume: 3 });
test_fs_corrupt(path);
global.__fs[$ backup] = undefined;      // no last-good copy exists
var degraded = mmapi_config_load("cfg_test");
dcheck("a corrupt config with no .bak still returns {}", degraded[$ "volume"] == undefined);
var warn_lines = mmapi_log_buffer_lines("cfg_test");
var warned = 0;
for (var i = 0; i < array_length(warn_lines); i++) {
    if (string_pos("failed to parse", warn_lines[i]) > 0) { warned += 1; }
}
dcheck("...and it says so rather than resetting in silence", warned > 0);
