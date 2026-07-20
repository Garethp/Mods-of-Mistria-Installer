// mmapi_modsave.gml. Per-save mod data: each registered mod hands over a
// struct payload, which lands in a JSON sidecar keyed by the save prefix. The
// prefix is read off the save path.
// Example: ".../saves/game-abc123-01.sav" → prefix "abc123"
//
// This module uses the framework the same way a mod would. At file scope it
// registers two ordinary event handlers on the hook engine, "save.game_saving"
// and "save.game_loaded", which the save seams emit. The work lives in
// mmapi_modsave_run_save and mmapi_modsave_run_load, which tests call directly.

function mmapi_modsave_register(mod_name, save_fn, load_fn) {
    if (global[$ "__mmapi_modsave"] == undefined) { global.__mmapi_modsave = []; }
    array_push(global.__mmapi_modsave, { mod_name: mod_name, save_fn: save_fn, load_fn: load_fn });
}

// "game-<prefix>-..." → the prefix. "" when the path carries no game ident.
function mmapi_modsave_prefix(save_path) {
    var path = string(save_path);
    var marker = "game-";
    var marker_pos = string_pos(marker, path);
    if (marker_pos == 0) { return ""; }
    var start = marker_pos + string_length(marker);
    var rest = string_copy(path, start, string_length(path) - start + 1);
    var dash = string_pos("-", rest);
    if (dash == 0) { return ""; }
    return string_copy(rest, 1, dash - 1);
}

function mmapi_modsave_file(mod_name, prefix) {
    return mmapi_mod_data_dir(mod_name) + "/saves/" + prefix + ".json";
}

// The mod's save_fn and load_fn run inside a plain try/catch. A failure warns
// and the loop continues to the other mods, so one bad mod cannot cost every
// other mod its save. The try/catch was once believed inert here, since
// run_save and run_load take arguments; that claim is false (see mmapi.gml).
function mmapi_modsave_run_save(save_path) {
    var prefix = mmapi_modsave_prefix(save_path);
    if (prefix == "") { return; }
    var registrations = global[$ "__mmapi_modsave"];
    if (registrations == undefined) { return; }
    var count = array_length(registrations);
    for (var i = 0; i < count; i++) {
        var entry = registrations[i];
        var data = undefined;
        try {
            data = entry.save_fn();
        } catch (err) {
            mmapi_warn_rate_limited(
                "modsave_save:" + entry.mod_name,
                entry.mod_name,
                "mmapi modsave: save_fn from " + entry.mod_name + " failed: "
                + string(err));
            // `data` stays undefined, which skips this mod's write below and
            // leaves its previous sidecar intact. Same outcome as a save_fn
            // that declines by returning nothing: a mod whose save_fn threw
            // produced no data, so its old state must not be overwritten.
        }
        if (data == undefined) { continue; }
        var dir = mmapi_mod_data_dir(entry.mod_name) + "/saves";
        if (directory_exists(dir) == false) { directory_create(dir); }
        var path = mmapi_modsave_file(entry.mod_name, prefix);
        // The last-good copy, from the old contents, before the primary write.
        // The ordering is the design (see __mmapi_backup_last_good): a failed
        // backup leaves the primary's old good state, and a failed primary
        // leaves the previous state in .bak. Guarded, so a backup failure costs
        // neither this mod's save nor, since this is a loop, anyone else's.
        __mmapi_backup_last_good(entry.mod_name, path);
        save_json_file(path, data);
    }
}

// load_fn receives undefined when no sidecar file exists yet, which is the
// case for a fresh save.
function mmapi_modsave_run_load(save_path) {
    var prefix = mmapi_modsave_prefix(save_path);
    if (prefix == "") { return; }
    var registrations = global[$ "__mmapi_modsave"];
    if (registrations == undefined) { return; }
    var count = array_length(registrations);
    for (var i = 0; i < count; i++) {
        var entry = registrations[i];
        var path = mmapi_modsave_file(entry.mod_name, prefix);
        var data = undefined;
        if (file_exists(path)) {
            data = try_read_json_file(path, undefined, false);
            // The sidecar exists but does not parse: a save interrupted partway
            // through the write. Handing load_fn `undefined` here would be
            // indistinguishable from a fresh save, silently resetting the mod's
            // state. Try the last-good copy first.
            if (data == undefined) {
                data = __mmapi_recover_last_good(entry.mod_name, path);
            }
        }
        try {
            entry.load_fn(data);
        } catch (err) {
            mmapi_warn_rate_limited(
                "modsave_load:" + entry.mod_name,
                entry.mod_name,
                "mmapi modsave: load_fn from " + entry.mod_name + " failed: "
                + string(err));
        }
    }
}

function __mmapi_modsave_handle_game_saving(ctx) {
    mmapi_modsave_run_save(ctx.save_path);
}

function __mmapi_modsave_handle_game_loaded(ctx) {
    mmapi_modsave_run_load(ctx.save_path);
}

mmapi_on("save.game_saving", __mmapi_modsave_handle_game_saving, { mod_name: "mmapi" });
mmapi_on("save.game_loaded", __mmapi_modsave_handle_game_loaded, { mod_name: "mmapi" });
