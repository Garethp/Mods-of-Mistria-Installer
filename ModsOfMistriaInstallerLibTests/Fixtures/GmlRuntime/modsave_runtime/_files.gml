// An in-memory filesystem, so the last-good-copy logic can be tested off-engine.
//
// mmapi's file surface is exactly six late-bound engine names. The compat
// dialect resolves them at runtime, so defining them here is all it takes to
// give the framework a filesystem it never knows is fake.
//
// __mmapi_backup_last_good exists for a corrupt file, a save interrupted partway
// through the write, and a real filesystem cannot produce one on demand. Here
// corruption is one line (test_fs_corrupt), so the case that matters is the easy
// case to test.

// mmapi_mod_data_dir builds every path from CONFIG_DIRECTORY, a game-only macro.
// Off-engine it does not late-bind to undefined; it throws "no such field
// CONFIG_DIRECTORY", because a macro is substituted at compile time rather than
// resolved at runtime. So it has to be stubbed.
#macro CONFIG_DIRECTORY global.__config_directory
global.__config_directory = "C:/fake_config";

global.__fs = {};              // path → the struct that was written
global.__fs_dirs = {};         // dir  → true
global.__fs_corrupt = {};      // path → true: exists, but reads back as undefined
global.__fs_throw_on = "";     // path: writing here throws, to test the guard

function test_fs_reset() {
    global.__fs = {};
    global.__fs_dirs = {};
    global.__fs_corrupt = {};
    global.__fs_throw_on = "";
}

// Make an existing file read back as undefined, exactly as try_read_json_file
// does for a truncated write. This is the failure the design is for.
function test_fs_corrupt(path) {
    global.__fs_corrupt[$ path] = true;
}

function test_fs_throw_on_write(path) {
    global.__fs_throw_on = path;
}

function test_fs_paths() {
    return struct_get_names(global.__fs);
}

// ── The engine surface mmapi calls ──────────────────────────────────────────

function save_json_file(path, value) {
    if (path == global.__fs_throw_on) { throw "disk full: " + string(path); }
    global.__fs[$ path] = value;
    // A successful write clears any prior corruption at that path.
    if (global.__fs_corrupt[$ path] != undefined) {
        global.__fs_corrupt[$ path] = undefined;
    }
    return true;
}

function try_read_json_file(path, default_value, log_error) {
    if (global.__fs_corrupt[$ path] == true) { return default_value; }
    var value = global.__fs[$ path];
    if (value == undefined) { return default_value; }
    return value;
}

function file_exists(path) {
    // A corrupt file exists, which is what makes it dangerous: the combination
    // "exists but does not parse" is what used to lose data silently.
    if (global.__fs_corrupt[$ path] == true) { return true; }
    return global.__fs[$ path] != undefined;
}

function directory_exists(dir) {
    return global.__fs_dirs[$ dir] == true;
}

function directory_create(dir) {
    global.__fs_dirs[$ dir] = true;
    return true;
}

function save_text_file(path, text) {
    if (path == global.__fs_throw_on) { throw "disk full: " + string(path); }
    global.__fs[$ path] = text;
    return true;
}
