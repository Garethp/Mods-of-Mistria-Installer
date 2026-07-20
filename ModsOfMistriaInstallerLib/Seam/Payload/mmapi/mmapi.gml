// mmapi.gml. Mods of Mistria API: mod identity, lifecycle, logging, config.
//
// Compat dialect (.gml). There are no closures, and all top-level functions
// are hoisted and global, so entry points are callable from any mod regardless
// of load order. Three engine constraints shape the code throughout:
//   * variable_global_exists does not exist on this runtime. Lazily-created
//     globals are guarded with the [$ ] accessor (global[$ "x"] == undefined).
//   * `mod` is a reserved keyword (modulo), so mod identifiers are `mod_name`.
//   * CONFIG_DIRECTORY is the only location GML can reliably write to,
//     confirmed in-engine. All mod data lives under it.

enum MmapiLogLevel {
    Trace,
    Debug,
    Info,
    Warn,
    Error,
}

// ── Mod identity ──────────────────────────────────────────────────────
// Attribution is temporal: registration functions read the current mod at call
// time. mmapi_mod_declare is called at a mod's top-level boot.
// mmapi_run_installs sets and restores the current mod around each installer,
// so registrations made inside installers attribute correctly.

function mmapi_mod_declare(mod_name, version) {
    if (global[$ "__mmapi_mods"] == undefined) { global.__mmapi_mods = {}; }
    global.__mmapi_mods[$ mod_name] = { mod_name: mod_name, version: version };
    global.__mmapi_current_mod = mod_name;
}

function mmapi_current_mod() {
    var current = global[$ "__mmapi_current_mod"];
    if (current == undefined) { return "unknown"; }
    return current;
}

// ── Lifecycle ─────────────────────────────────────────────────────────
// Mods queue installers, and the Game begin_step seam drains the queue every
// frame. Installers must be idempotent, guarded with a marker on whatever they
// wrap. The queue is never cleared, so wrappers re-apply to instances rebuilt
// mid-game (e.g. clock on a new day) at about zero cost once installed.

function mmapi_register(install_fn) {
    __mmapi_register_as(install_fn, mmapi_current_mod());
}

// Queue an installer with explicit attribution. mmapi's own modules
// self-register through this, so they never read or disturb the current-mod
// global at file scope.
function __mmapi_register_as(install_fn, mod_name) {
    if (global[$ "__mmapi_installs"] == undefined) { global.__mmapi_installs = []; }
    array_push(global.__mmapi_installs, { fn: install_fn, mod_name: mod_name });
}

function mmapi_run_installs() {
    // The drain runs from the Game begin_step seam, inside a frame, where file
    // IO is safe. Top-level boot code (mod declare and registration) runs
    // before the first drain, and file IO there throws in-engine, so IO is
    // marked ready here. The log file sink defers every flush until this first
    // drain and buffers lines in memory; the buffer keeps every session line,
    // so nothing is lost. Config and modsave use the same lazy-IO discipline.
    if (global[$ "__mmapi_io_ready"] != true) {
        global.__mmapi_io_ready = true;
        __mmapi_log_flush_all_pending();
    }

    var installs = global[$ "__mmapi_installs"];
    if (installs == undefined) { return; }
    var count = array_length(installs);
    for (var i = 0; i < count; i++) {
        var record = installs[i];
        var previous_mod = global[$ "__mmapi_current_mod"];
        global.__mmapi_current_mod = record.mod_name;
        try {
            record.fn();
        } catch (err) {
            mmapi_warn_rate_limited(
                "install:" + record.mod_name,
                record.mod_name,
                "mmapi installer from " + record.mod_name + " failed: " + string(err));
        }
        global.__mmapi_current_mod = previous_mod;
    }
}

// ── Calling into mod code ─────────────────────────────────────────────
// Every call into mod-supplied code sits in a plain try/catch at the call
// site, containing the failure per kind.
//
// The shape this replaced is still out in the wild, so it is worth recording
// why it is gone. Every such call used to route through __mmapi_guarded_call,
// a zero-argument trampoline whose inputs and outputs rode one reused global
// scratch struct (fn/argc/arg0/arg1 → ok/result/error_value). It worked around
// a claimed engine quirk: "a try/catch inside a function that was itself called
// with arguments fails to catch exceptions raised in callees; only a
// zero-argument function catches reliably".
//
// That claim is false. It was probed under the fabricator VM at the rev the
// checker pins (10 shapes, including this framework's own dispatch written
// without the trampoline; fabricator's own try_catch.gml asserts the same) and
// then in the shipped game (6 shapes, all caught, with a control confirming the
// probe measured the engine). The trampoline's 2-argument cap on handlers went
// with it: argc/arg0/arg1 was how many slots the scratch had, never a design
// decision.
//
// The claims are checked rather than remembered. Both quirks stay probed on
// the pinned VM, and a throwing handler is isolated per kind: the chain
// continues, and the error is counted against the mod that raised it. That
// keeps the plain try/catch safe.
//
// Do not add new workarounds on the old quirk's authority.
//
// Quirk 2, also no longer reproducing. The claim was that a value returned
// through a call frame can compare equal to both undefined and false (an
// implicit or explicit undefined return satisfying `== false`), so callers
// must test `result == undefined` before `result == false`. Probed on the
// pinned VM and in-engine: undefined compares equal to undefined and not to
// false, and mmapi_check_guards' logic with the ordering deliberately reversed
// still allows. That ordering is kept as cheap defensiveness, not because the
// engine requires it.

// ── Rate-limited warnings ─────────────────────────────────────────────
// Shared by hook dispatch, installers, hotkeys and modsave. The first
// occurrence per key logs immediately, then one log every 60th occurrence.
// Returns the occurrence count for the key.

function mmapi_warn_rate_limited(key, mod_name, msg) {
    if (global[$ "__mmapi_warn_counts"] == undefined) { global.__mmapi_warn_counts = {}; }
    var counts = global.__mmapi_warn_counts;
    var count = counts[$ key];
    if (count == undefined) { count = 0; }
    count += 1;
    counts[$ key] = count;
    if ((count - 1) % 60 == 0) {
        mmapi_log_warn(mod_name, msg);
    }
    return count;
}

// ── Logging ───────────────────────────────────────────────────────────
// Levels Trace to Error, with Trace file-only by policy. The console sink is
// show_debug_message coloured via the runner's text_color_logging, falling back
// to a plain line. The file sink is a per-mod in-memory buffer, flushed as one
// save_text_file of the accumulated session log every 20 buffered lines, or
// immediately at level >= Warn.
//
// [PROBE] lines flush immediately while the debug agent is enabled: probe
// consumers tail the file, so capture must not depend on where a probe lands
// in a 20-line batch. Each flush rewrites the whole session file, so per-line
// flushing hitches frames harder as the session grows.

function mmapi_log_set_level(level) {
    global.__mmapi_log_level = level;
}

function mmapi_log_set_sinks(console_on, file_on) {
    global.__mmapi_log_console = console_on;
    global.__mmapi_log_file = file_on;
}

// Map a config "log_level" string onto the enum (default Info).
function mmapi_log_level_from_string(text) {
    switch (string_lower(string(text))) {
        case "trace":   return MmapiLogLevel.Trace;
        case "debug":   return MmapiLogLevel.Debug;
        case "warn":    return MmapiLogLevel.Warn;
        case "warning": return MmapiLogLevel.Warn;
        case "error":   return MmapiLogLevel.Error;
        default:        return MmapiLogLevel.Info;
    }
}

// Lazy and cached. An explicit mmapi_log_set_level() override wins, otherwise
// the "log_level" key is read from the mmapi config once IO is ready.
// Boot-time file IO throws, so until then this reports Info without caching and
// the real verdict is read once gameplay-time IO is live. Trace is the verbose
// tier, carrying probes and noisy diagnostics; the default Info keeps a normal
// build quiet.
function mmapi_log_get_level() {
    var level = global[$ "__mmapi_log_level"];
    if (level != undefined) { return level; }
    if (mmapi_io_is_ready() == false) { return MmapiLogLevel.Info; }
    var resolved = MmapiLogLevel.Info;
    try {
        var cfg = mmapi_config_load("mmapi");
        resolved = mmapi_log_level_from_string(mmapi_config_get(cfg, "log_level", "info"));
    } catch (err) {}
    global.__mmapi_log_level = resolved;
    return resolved;
}

function mmapi_log_console_on() {
    var console_on = global[$ "__mmapi_log_console"];
    if (console_on == undefined) { return true; }
    return console_on;
}

function mmapi_log_file_on() {
    // File sink on by default: console output is unattached for a windowed
    // launch, so the file is the reliable log view on this engine.
    var file_on = global[$ "__mmapi_log_file"];
    if (file_on == undefined) { return true; }
    return file_on;
}

function mmapi_log_level_tag(level) {
    switch (level) {
        case MmapiLogLevel.Trace: return "TRACE";
        case MmapiLogLevel.Debug: return "DEBUG";
        case MmapiLogLevel.Info:  return "INFO ";
        case MmapiLogLevel.Warn:  return "WARN ";
        default:                  return "ERROR";
    }
}

// MmapiLogLevel → the game's LogLevel enum, so text_color_logging colours
// these lines exactly like the game's own Logger.
function mmapi_log_game_level(level) {
    switch (level) {
        case MmapiLogLevel.Trace: return LogLevel.Trace;
        case MmapiLogLevel.Warn:  return LogLevel.Warn;
        case MmapiLogLevel.Error: return LogLevel.Error;
        default:                  return LogLevel.Info; // Debug + Info
    }
}

// Per-mod log buffer { lines, text, pending }. `lines` keeps every session line
// and is the testability seam. `text` is the pre-joined session log, so a flush
// is a single save_text_file. `pending` counts lines since the last flush.
function __mmapi_log_buffer(mod_name) {
    if (global[$ "__mmapi_log_buffers"] == undefined) { global.__mmapi_log_buffers = {}; }
    var buffers = global.__mmapi_log_buffers;
    var buffer = buffers[$ mod_name];
    if (buffer == undefined) {
        buffer = { lines: [], text: "", pending: 0 };
        buffers[$ mod_name] = buffer;
    }
    return buffer;
}

// Testability seam: every line buffered for a mod this session, flushed or not.
function mmapi_log_buffer_lines(mod_name) {
    return __mmapi_log_buffer(mod_name).lines;
}

// Write the accumulated session log to <mod-data>/logs/<mod>.log. Failures are
// swallowed: a bad path must not take down the mod, and the console sink still
// carries the messages.
function mmapi_log_flush(mod_name) {
    var buffer = __mmapi_log_buffer(mod_name);
    buffer.pending = 0;
    try {
        var dir = mmapi_mod_data_dir(mod_name) + "/logs";
        if (directory_exists(dir) == false) { directory_create(dir); }
        save_text_file(dir + "/" + mod_name + ".log", buffer.text);
    } catch (err) {
    }
}

// True once the first begin_step install drain has run (mmapi_run_installs).
// Before that it is still top-level boot, where file IO throws in-engine, so
// the log file sink defers flushing and lines buffer in memory.
function mmapi_io_is_ready() {
    return global[$ "__mmapi_io_ready"] == true;
}

// Flush every mod's pending buffer once, when IO first becomes ready, so the
// boot-time lines that could not be written safely land on the first frame.
function __mmapi_log_flush_all_pending() {
    var buffers = global[$ "__mmapi_log_buffers"];
    if (buffers == undefined) { return; }
    var names = struct_get_names(buffers);
    for (var i = 0; i < array_length(names); i++) {
        var buffer = buffers[$ names[i]];
        if (buffer != undefined && buffer.pending > 0) {
            mmapi_log_flush(names[i]);
        }
    }
}

// True when the debug agent is enabled and the line is a probe, so the file
// sink flushes it immediately. The guarded-global read keeps this safe when
// mmapi_debug is absent, e.g. unit tests or a bare engine. `enabled` only turns
// true once the agent's lazy config gate has run, so plain boot keeps the
// batched policy.
function __mmapi_log_flush_now(line) {
    var debug_state = global[$ "__mmapi_debug"];
    if (debug_state == undefined) { return false; }
    if (debug_state[$ "enabled"] != true) { return false; }
    return string_pos("[PROBE] ", line) > 0;
}

function __mmapi_log_buffer_append(mod_name, line, level) {
    var buffer = __mmapi_log_buffer(mod_name);
    array_push(buffer.lines, line);
    buffer.text += line + "\n";
    buffer.pending += 1;
    // Defer all file IO until boot is over, at the first install drain:
    // boot-time file IO throws in-engine, so lines keep buffering and the first
    // frame flushes them. After boot the usual policy applies: flush at Warn+
    // or every 20, or immediately for a probe line while the agent is on.
    if (mmapi_io_is_ready() == false) {
        return;
    }
    if (level >= MmapiLogLevel.Warn || buffer.pending >= 20
            || __mmapi_log_flush_now(line)) {
        mmapi_log_flush(mod_name);
    }
}

function mmapi_log(level, mod_name, msg) {
    if (level < mmapi_log_get_level()) {
        return;
    }
    var line = "[" + mmapi_log_level_tag(level) + "] " + string(msg);

    if (level != MmapiLogLevel.Trace && mmapi_log_console_on()) {
        // This catch was believed inert (mmapi_log takes arguments, see the
        // guarded-call quirk note above). It is not: a try/catch in an
        // arg-taking function catches normally on the pinned VM and in-engine,
        // so a text_color_logging failure really does fall back to the plain
        // line below.
        try {
            show_debug_message(
                text_color_logging(mmapi_log_game_level(level))
                + " [" + string(mod_name) + "] " + string(msg));
        } catch (err) {
            show_debug_message("[" + string(mod_name) + "] " + line);
        }
    }

    if (mmapi_log_file_on()) {
        try {
            __mmapi_log_buffer_append(mod_name, line, level);
        } catch (err) {
        }
    }
}

function mmapi_log_trace(mod_name, msg) { mmapi_log(MmapiLogLevel.Trace, mod_name, msg); }
function mmapi_log_debug(mod_name, msg) { mmapi_log(MmapiLogLevel.Debug, mod_name, msg); }
function mmapi_log_info(mod_name, msg)  { mmapi_log(MmapiLogLevel.Info,  mod_name, msg); }
function mmapi_log_warn(mod_name, msg)  { mmapi_log(MmapiLogLevel.Warn,  mod_name, msg); }
function mmapi_log_error(mod_name, msg) { mmapi_log(MmapiLogLevel.Error, mod_name, msg); }

// ── Config and mod-data directories ───────────────────────────────────
// CONFIG_DIRECTORY (= game_save_id, the game's writable save and config dir) is
// the only location GML can reliably write to. Confirmed in-engine: a bare
// relative path reads from the install dir but does not successfully write
// anywhere, and GML exposes no working_directory to build an absolute
// install-dir path. So mod data sits beside the game's own saves and settings.

function mmapi_mod_data_dir(mod_name) {
    return CONFIG_DIRECTORY + "/mod_data/" + mod_name;
}

function mmapi_config_dir(mod_name) {
    var dir = mmapi_mod_data_dir(mod_name);
    try {
        if (directory_exists(dir) == false) {
            directory_create(dir);
        }
    } catch (err) {}
    return dir;
}

function mmapi_config_path(mod_name) {
    return mmapi_config_dir(mod_name) + "/" + mod_name + ".json";
}

// ── Last-good copies ──────────────────────────────────────────────────
// save_json_file is single-shot: a crash partway through leaves a truncated
// file, try_read_json_file then yields undefined, and the caller silently falls
// back to "no data", so a mod's per-save state resets with nothing said. These
// two make that recoverable.
//
// Both use only primitives proven inside the events that call them. Reading
// inside the save window was the one untested combination, and is verified on
// the live install.

// Copy `path`'s current contents to `<path>.bak`, before the caller overwrites
// it. The ordering is the design:
//
//   - a failed .bak write leaves the primary's old good state on disk (the save
//     is skipped, not corrupted), and
//   - a failed primary write leaves the previous state in .bak.
//
// Either way one good copy survives. Guarded with a plain try/catch, so a
// backup failure can never cost the real save. The trampoline this once seemed
// to need was for a quirk that does not exist (see the quirk note above).
function __mmapi_backup_last_good(mod_name, path) {
    try {
        if (file_exists(path) == false) { return; }
        var old_data = try_read_json_file(path, undefined, false);
        // The primary is already corrupt, so a copy of it would overwrite the
        // last good .bak there is with garbage.
        if (old_data == undefined) { return; }
        save_json_file(path + ".bak", old_data);
    } catch (err) {
        mmapi_warn_rate_limited(
            "backup:" + string(path), mod_name,
            "mmapi: could not write a last-good copy of " + string(path) + ": "
            + string(err) + ". The primary write is unaffected");
    }
}

// The load-side half, consulted only when the primary exists and does not
// parse. That is the case that used to lose the data silently, and the
// alternative at this point is already "return nothing".
//
// Not consulted when the primary is merely absent: that is a fresh save, whose
// contract is that load_fn receives undefined. Resurrecting a stale .bak there
// would hand a mod another save's state.
function __mmapi_recover_last_good(mod_name, path) {
    try {
        var backup = string(path) + ".bak";
        if (file_exists(backup) == false) { return undefined; }
        var data = try_read_json_file(backup, undefined, false);
        if (data == undefined) {
            mmapi_log_warn(mod_name, "mmapi: " + string(path) + " did not parse, and "
                + "neither did " + backup + " - this data is lost");
            return undefined;
        }
        mmapi_log_warn(mod_name, "mmapi: " + string(path) + " did not parse; recovered "
            + "the last-good copy from " + backup);
        return data;
    } catch (err) {
        return undefined;
    }
}

// Load a mod's config as a struct. Returns {} if the file is missing or
// unparseable.
function mmapi_config_load(mod_name) {
    var path = mmapi_config_path(mod_name);
    if (file_exists(path)) {
        var data = try_read_json_file(path, undefined, false);
        if (data != undefined) {
            return data;
        }
        // The primary is corrupt. Try the last-good copy before giving up.
        var recovered = __mmapi_recover_last_good(mod_name, path);
        if (recovered != undefined) {
            return recovered;
        }
        mmapi_log_warn(mod_name, "config exists but failed to parse: " + path);
    }
    return {};
}

function mmapi_config_save(mod_name, cfg) {
    try {
        mmapi_config_dir(mod_name);
        var path = mmapi_config_path(mod_name);
        // Last-good copy first, from the old contents. See
        // __mmapi_backup_last_good for why the ordering is the design.
        __mmapi_backup_last_good(mod_name, path);
        save_json_file(path, cfg);
    } catch (err) {
        mmapi_log_warn(mod_name, "config save failed: " + mmapi_config_path(mod_name));
    }
}

// Typed get with default (returns default if key absent).
function mmapi_config_get(cfg, key, default_value) {
    if (cfg == undefined) { return default_value; }
    var value = cfg[$ key];
    if (value == undefined) { return default_value; }
    return value;
}

// Typed get with default and inclusive [lo, hi] range guard.
function mmapi_config_get_range(cfg, key, default_value, lo, hi) {
    var value = mmapi_config_get(cfg, key, default_value);
    if (value < lo || value > hi) { return default_value; }
    return value;
}

// ── Standard config-load kit ──────────────────────────────────────────
// The one way to load a mod config, so no mod re-implements the version gate,
// the materialise-on-load write, or the value validators. A mod's
// <mod>_config_ensure_loaded reads:
//
//     var src = mmapi_config_read_valid("<mod>", <MOD>_CONFIG_VERSION);   // {} if missing/invalid
//     var cfg = {
//         some_flag: mmapi_config_bool(src, "some_flag", DEFAULT_FLAG),
//         some_num:  mmapi_config_number(src, "some_num", DEFAULT_NUM, LO, HI),
//         // mod-specific validators (allow-lists, arrays) stay explicit here
//     };
//     mmapi_config_write("<mod>", <MOD>_CONFIG_VERSION, cfg);            // stamps version + materialises
//     state.config = cfg;

// True when `loaded` carries __config_version == version. Type-tolerant: a JSON
// int reads back as int64 (the fabricator harness) or number (live json_parse),
// so both are accepted. An int64-only check would reject a written version live
// and reset the config to defaults every load.
function mmapi_config_version_ok(loaded, version) {
    if (!is_struct(loaded)) { return false; }
    var v = loaded[$ "__config_version"];
    if (typeof(v) != "int64" && typeof(v) != "number") { return false; }
    return v == version;
}

// Read a mod's config file, returning it only when its __config_version
// matches, else {} so every key falls back to its default. A missing or
// unparseable file also yields {}.
function mmapi_config_read_valid(mod_name, version) {
    var loaded = mmapi_config_load(mod_name);   // {} if missing/unparseable
    if (mmapi_config_version_ok(loaded, version)) { return loaded; }
    return {};
}

// Stamp __config_version onto the built config and write it back, so the
// on-disk file always exists and carries every key for the user to edit.
function mmapi_config_write(mod_name, version, cfg) {
    cfg.__config_version = version;
    mmapi_config_save(mod_name, cfg);
}

// Read a struct member as a bool, defaulting when the container or key is
// missing or the value is not a bool. Fabricator coerces a bool bound to a
// local to a number, so the typeof check reads inline.
function mmapi_config_bool(container, key, dflt) {
    if (!is_struct(container)) { return dflt; }
    if (typeof(container[$ key]) == "bool") { return container[$ key]; }
    return dflt;
}

// Read a struct member as a number in [lo, hi], else the default.
// Type-tolerant: a JSON number reads back as number (live) or int64 (harness),
// so both are accepted and normalised through real(). An int64-only check would
// reject a written value live and reset it to the default every load.
function mmapi_config_number(container, key, dflt, lo, hi) {
    if (!is_struct(container)) { return dflt; }
    var v = container[$ key];
    if ((typeof(v) == "number" || typeof(v) == "int64") && v >= lo && v <= hi) { return real(v); }
    return dflt;
}
