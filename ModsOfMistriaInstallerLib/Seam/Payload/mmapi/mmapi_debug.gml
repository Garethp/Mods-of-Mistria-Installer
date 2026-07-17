// mmapi_debug.gml. The MmapiDebugger in-game agent: it stages probes and speaks
// the two-file JSON protocol the MmapiDebugger web client reads and writes,
// under <CONFIG_DIRECTORY>/mod_data/mmapi/.
//
//   control.json (client to agent) is polled at most every `control_every`
//   frames (default 10) while running, and every frame while paused:
//     { "watch_paths":  ["global.__my_mod.state.x", ...],
//       "breakpoints":  [{path, op, value, enabled, mode: "level"|"edge"}],
//       "rev":          <int>,
//       "commands":     [{"op": "pause"|"resume"|"step"
//                              |"set"  (path, value)
//                              |"call" (fn | path, args)}, ...] }
//   Commands run once, only when rev > applied_rev; the client bumps rev per
//   batch. watch_paths and breakpoints are not rev-gated, and every poll adopts
//   them wholesale. Command values arrive already typed: the client parses
//   int/float/bool/null/string before writing the JSON, and the agent writes
//   them through as-is.
//
//   state.json (agent to client) is written every `emit_every` frames
//   (default 6, about 10Hz) while running, and every frame while paused:
//     { applied_rev, paused, game_frame, engine_frame, pause_status,
//       watches: {path: value}, caps: {...}, last_break, active_break,
//       last_call, breakpoints, functions, step_request,
//       rev (engine-frame heartbeat) }
//   active_break is the breakpoint the agent is currently paused at, carrying
//   the same payload as last_break. It is cleared on resume or step, and null
//   for a manual pause. functions is the published catalog of
//   debugger-callable functions ({name, mod_name, description, args})
//   registered via mmapi_debug_register_fn, which is the Call Lab surface. The
//   `fn` method itself is never serialized.
//
// Pause mechanism: pausing sets the PauseStatus.WINDOW bit on PAUSE_STATUS, the
// engine's own cooperative pause flag, the one the window focus-loss handler
// uses. It satisfies both game_paused() and non_cutscene_pause(), so every
// pause-gated system freezes: player FSM, monsters, clock, and so on. The agent
// runs from the Game begin_step install drain, which the engine executes before
// the frame's pause-gated logic, so the flag set here freezes that same frame,
// giving a clean logic freeze, an exact 1-frame step, and no flicker. The flag
// is re-asserted every paused frame, so the focus handler cannot strand it.
// "step" removes it for exactly one frame; "resume" removes it outright.
//
// Three limits come with that mechanism:
//   * It is cooperative. begin_step itself still runs, which is what keeps this
//     agent, and every other mod's installer or tick, alive and able to
//     un-pause. draw events still run, audio keeps playing, and any engine
//     system that ignores PAUSE_STATUS keeps running.
//   * It shares the WINDOW bit with the focus-loss handler, so a debug pause is
//     indistinguishable from an unfocused window, and "resume" unpauses a
//     genuinely unfocused window until its next focus event re-asserts it.
//   * game_frame counts only frames whose logic actually ran, which is the step
//     proof. engine_frame ticks always.
//
// Enabling: the agent is off by default. Set {"debug_enabled": true} in the
// mmapi config file (<CONFIG_DIRECTORY>/mod_data/mmapi/mmapi.json), which is
// what the installer's debugger setting writes. The flag is checked lazily on
// the first tick, never at top-level boot, where file IO throws in-engine, and
// the verdict is cached for the session. Restart to toggle, or call
// mmapi_debug_set_enabled() as the runtime escape hatch. While disabled the
// per-frame cost is one struct lookup and a bool test, and nothing is ever read
// or written.
//
// Hotkeys (registered through the registry only once enabled):
//   F9 pause/resume, F10 step one frame (while paused), F8 toggle emit.
//
// Four engine constraints shape the code throughout (see mmapi.gml):
//   * Every dynamic call into watch, set or call targets is wrapped in a plain
//     try/catch: the agent reads arbitrary paths and invokes arbitrary
//     callables, so a throw is routine and must never reach the game. Functions
//     suffixed _raw are the throwing halves, each wrapped by a guarded caller.
//     These used to route through the zero-arg __mmapi_guarded_call trampoline,
//     because a try/catch in an arg-taking function was believed inert. That was
//     false - see mmapi.gml.
//   * `== undefined` is tested before `== false`, because a missing field
//     satisfies `== false`. Presence is what is being gated, not truth.
//   * string_split returns an empty array when the delimiter is absent on the
//     shipped engine, while the stub returns [whole]. Both are handled.
//   * Serialization is hand-rolled. save_json_file crash()es in-engine on
//     non-plain values, so it is never used here.
//
// One watchable extra: global.__mmapi_debug_stats carries a fresh
// mmapi_hook_stats() snapshot, republished on every control poll. Watch
// "global.__mmapi_debug_stats" (or ".hooks", ".errors", ".wiring") to observe
// hook wiring live. Hook names contain dots, so `hooks` entries cannot be
// addressed as their own path segment; the `wiring` table swaps dots for
// underscores so one hook's handler list is addressable.
// Example: ".wiring.spells_cost" → every handler on spells.cost, in dispatch
// order, with its mod, kind and priority.

// Names the mod_data subdir for control.json and state.json, plus the agent's
// log file (logs/mmapi.log). "mmapi" co-locates them with mmapi.json under one
// root.
#macro MMAPI_DEBUG_MOD "mmapi"

// ── State ─────────────────────────────────────────────────────────────
// Lazy: nothing lives at file scope but the install registration.

function __mmapi_debug_state() {
    if (global[$ "__mmapi_debug"] == undefined) {
        global.__mmapi_debug = {
            enabled_checked: false,
            enabled: false,
            paused: false,
            step_request: false,
            entry_paused: false,   // pause state at the start of the frame, used for breakpoint resume-grace
            engine_frame: 0,       // ticks always. drives the heartbeat and snapshot freshness
            game_frame: 0,         // ticks only when game logic ran. this is the step proof
            emit: true,
            emit_every: 6,         // about 10Hz while running, every frame while paused
            emit_count: 0,
            force_emit: false,     // one-shot flag that flushes state.json the frame a command batch applies, so applied_rev is durable across restarts
            control_every: 10,     // poll control.json every 10th frame, every frame while paused
            control_count: 0,
            applied_rev: 0,        // highest control.json rev whose commands already ran
            watch_paths: ["global.__mmapi_debug.game_frame", "global.__mmapi_debug.paused"],
            breakpoints: [],       // adopted wholesale from control.json
            bp_state: {},          // maps signature to {was} for edge-triggering
            label_state: {},       // maps mmapi_debug_break label to bool, one edge per label
            last_break: undefined,
            active_break: undefined,   // the breakpoint currently paused at, cleared on resume or step, null for a manual pause
            last_call: undefined,
            last_keys: undefined,      // {path, keys} from the `keys` op, used for path-autocomplete introspection
            fns: {},               // mmapi_debug_register_fn registry: maps name to {fn, mod_name, description, args}
            probed: false,
            hotkeys_installed: false,
            caps: {},
            unresolved: { __mmapi_debug_unresolved: true },  // sentinel, kept distinct from a real undefined leaf
        };
    }
    return global.__mmapi_debug;
}

// Lazy config gate on mod "mmapi", key "debug_enabled". The first call reads the
// file at gameplay time, and the verdict is cached for the session.
function __mmapi_debug_is_enabled(state) {
    if (state.enabled_checked == false) {
        state.enabled_checked = true;
        var cfg = mmapi_config_load("mmapi");
        var flag = mmapi_config_get(cfg, "debug_enabled", false);
        state.enabled = (flag == true);
    }
    return state.enabled;
}

// Runtime override, callable from mod debug tools. It bypasses the config gate.
function mmapi_debug_set_enabled(on) {
    var state = __mmapi_debug_state();
    state.enabled_checked = true;
    state.enabled = (on == true);
}

// ── Paths ─────────────────────────────────────────────────────────────
// Everything derives from mmapi_mod_data_dir, so the agent and the
// MmapiDebugger client (config.mmapi_debug_dir) agree on one directory,
// <config>/mod_data/mmapi.

function __mmapi_debug_dir() {
    var dir = mmapi_mod_data_dir(MMAPI_DEBUG_MOD);
    try {
        if (directory_exists(dir) == false) { directory_create(dir); }
    } catch (err) {}
    return dir;
}

function __mmapi_debug_control_path() { return __mmapi_debug_dir() + "/control.json"; }
function __mmapi_debug_state_path() { return __mmapi_debug_dir() + "/state.json"; }

// ── JSON encoding ─────────────────────────────────────────────────────
// Hand-rolled, guarding against cycles by capping the recursion depth.

function __mmapi_debug_bool_json(value) {
    if (value) { return "true"; }
    return "false";
}

function __mmapi_debug_json_quote(text) {
    text = string_replace_all(text, "\\", "\\\\");   // escape the backslash first
    text = string_replace_all(text, "\"", "\\\"");
    text = string_replace_all(text, "\n", "\\n");
    text = string_replace_all(text, "\r", "\\r");
    text = string_replace_all(text, "\t", "\\t");
    return "\"" + text + "\"";
}

function __mmapi_debug_json_value(value, depth) {
    if (depth > 6) { return "\"<max-depth>\""; }

    if (value == undefined) { return "null"; }

    // Check bool before real, because is_real(true) is true and would emit 1.
    if (typeof(value) == "bool") {
        if (value) { return "true"; }
        return "false";
    }

    if (is_real(value)) {
        if (value != value) { return "null"; }              // NaN
        if (value == infinity) { return "\"Infinity\""; }
        if (value == -infinity) { return "\"-Infinity\""; }
        return string(value);
    }

    if (is_string(value)) { return __mmapi_debug_json_quote(value); }

    if (is_array(value)) {
        var length = array_length(value);
        var out = "[";
        for (var i = 0; i < length; i++) {
            if (i > 0) { out += ","; }
            out += __mmapi_debug_json_value(value[i], depth + 1);
        }
        return out + "]";
    }

    if (is_struct(value)) {
        var keys = struct_get_names(value);
        var key_count = array_length(keys);
        var out = "{";
        for (var j = 0; j < key_count; j++) {
            if (j > 0) { out += ","; }
            var key = keys[j];
            out += __mmapi_debug_json_quote(key) + ":" + __mmapi_debug_json_value(value[$ key], depth + 1);
        }
        return out + "}";
    }

    return "\"<unserializable>\"";   // a method, an instance id, a ptr, and so on
}

// The throwing half, wrapped by the guarded caller below.
function __mmapi_debug_json_raw(value) {
    return __mmapi_debug_json_value(value, 0);
}

// Guarded encode, so one bad value can never blank the whole snapshot.
function __mmapi_debug_safe_json(value) {
    try {
        return __mmapi_debug_json_raw(value);
    } catch (err) {
        return "\"<error>\"";
    }
}

// ── Dotted-path resolver ──────────────────────────────────────────────
// A path walks from global through struct fields via [$ ], with numeric
// segments indexing arrays.
// Example: "global.__my_mod.state.x"
// Returns the unresolved sentinel when any step fails, and never throws.

function mmapi_debug_is_unresolved(value) {
    if (is_struct(value) == false) { return false; }
    return struct_exists(value, "__mmapi_debug_unresolved");
}

function __mmapi_debug_is_uint(token) {
    if (token == "") { return false; }
    var i = 1;
    while (true) {
        var ch = string_char_at(token, i);
        if (ch == "") { break; }
        if (string_pos(ch, "0123456789") == 0) { return false; }
        i += 1;
    }
    return true;
}

// Reach an arbitrary top-level global by string, through the global-scope
// struct accessor. The accessor works for both read and write, which is what
// makes any mod state watchable with no registration.
function __mmapi_debug_global_root(name) {
    var value = global[$ name];
    if (value != undefined) { return value; }
    return __mmapi_debug_state().unresolved;
}

// The throwing half, wrapped by the guarded caller below. Resolves a bare path
// head: the curated instance roots first, then a top-level global of that name,
// so "__my_mod.x" works without the "global." prefix.
function __mmapi_debug_head_root_raw(head) {
    if (head == "obj_ari") {
        if (instance_number(obj_ari) > 0) { return instance_find(obj_ari, 0); }
        return __mmapi_debug_state().unresolved;
    }
    return __mmapi_debug_global_root(head);
}

function __mmapi_debug_head_root(head) {
    try {
        return __mmapi_debug_head_root_raw(head);
    } catch (err) {
        return __mmapi_debug_state().unresolved;
    }
}

// The throwing half, wrapped by the guarded caller below. Resolves one
// dotted-path step: arrays index by numeric segment, structs require the field
// to exist, and anything else gets the raw [$ ] accessor, which handles
// instances in-engine. A primitive receiver throws, and the guarded caller
// reports it.
function __mmapi_debug_member_raw(container, key) {
    if (is_array(container)) {
        if (__mmapi_debug_is_uint(key) == false) { return __mmapi_debug_state().unresolved; }
        var index = real(key);
        if (index < 0 || index >= array_length(container)) { return __mmapi_debug_state().unresolved; }
        return container[index];
    }
    if (is_struct(container)) {
        if (struct_exists(container, key)) { return container[$ key]; }
        return __mmapi_debug_state().unresolved;
    }
    return container[$ key];
}

function __mmapi_debug_member(container, key) {
    try {
        return __mmapi_debug_member_raw(container, key);
    } catch (err) {
        return __mmapi_debug_state().unresolved;
    }
}

function mmapi_debug_resolve(path) {
    var state = __mmapi_debug_state();
    if (is_string(path) == false || path == "") { return state.unresolved; }

    var parts = string_split(path, ".");
    var part_count = array_length(parts);
    // On the shipped engine string_split returns an empty array when the
    // delimiter is absent, so a bare single-token path reads as one token.
    if (part_count == 0) { parts = [path]; part_count = 1; }
    for (var i = 0; i < part_count; i++) {
        if (parts[i] == "") { return state.unresolved; }
    }

    var current;
    var start_index;
    var head = parts[0];
    if (head == "global") {
        if (part_count < 2) { return state.unresolved; }
        current = __mmapi_debug_global_root(parts[1]);
        start_index = 2;
    } else {
        current = __mmapi_debug_head_root(head);
        start_index = 1;
    }
    if (mmapi_debug_is_unresolved(current)) { return state.unresolved; }

    for (var i = start_index; i < part_count; i++) {
        current = __mmapi_debug_member(current, parts[i]);
        if (mmapi_debug_is_unresolved(current)) { return state.unresolved; }
    }
    return current;
}

// ── set ───────────────────────────────────────────────────────────────
// Write a client-typed value into the location named by a dotted path: resolve
// the parent container, then write the last token through the guarded caller. A
// struct or instance is written via [$ ], an array by index.

// The throwing half, wrapped by the guarded caller below.
// request = {container, key, value}
function __mmapi_debug_write_raw(request) {
    var container = request.container;
    var key = request.key;
    if (is_array(container)) {
        if (__mmapi_debug_is_uint(key) == false) { return false; }
        var index = real(key);
        if (index < 0 || index >= array_length(container)) { return false; }
        container[index] = request.value;
        return true;
    }
    container[$ key] = request.value;
    return true;
}

function __mmapi_debug_apply_set(path, value) {
    if (is_string(path) == false || path == "") {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] bad path");
        return;
    }
    var parts = string_split(path, ".");
    var part_count = array_length(parts);
    if (part_count == 0) { parts = [path]; part_count = 1; }
    if (part_count < 2) {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] need parent.key form: " + path);
        return;
    }
    var last_key = parts[part_count - 1];
    if (last_key == "") {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] empty key: " + path);
        return;
    }

    // "global.foo" → the parent is the global scope, written via the accessor.
    if (part_count == 2 && parts[0] == "global") {
        global[$ last_key] = value;
        mmapi_log_info(MMAPI_DEBUG_MOD, "[set] " + path + " = " + __mmapi_debug_safe_json(value));
        return;
    }

    var parent_path = parts[0];
    for (var i = 1; i < part_count - 1; i++) { parent_path += "." + parts[i]; }
    var parent = mmapi_debug_resolve(parent_path);
    if (mmapi_debug_is_unresolved(parent)) {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] parent unresolved: " + parent_path);
        return;
    }

    var wrote = undefined;
    var threw = false;
    try {
        wrote = __mmapi_debug_write_raw({ container: parent, key: last_key, value: value });
    } catch (err) {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] write threw: " + path + ": " + string(err));
        threw = true;
    }
    if (threw) { return; }
    if (wrote == undefined) {        // presence before truth, as everywhere here
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] write failed: " + path);
        return;
    }
    if (wrote == false) {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[set] bad array index: " + path);
        return;
    }
    mmapi_log_info(MMAPI_DEBUG_MOD, "[set] " + path + " = " + __mmapi_debug_safe_json(value));
}

// ── call ──────────────────────────────────────────────────────────────
// Invoke a registered function ({"fn": name}) or a method on a resolvable
// receiver ({"path": "recv.method"}). The result is echoed in
// state.json.last_call as {target, result | error}.

// A mod registers a standalone global function so the debugger can call it by
// name. Standalone functions are not reachable via global[$ ], so this is how
// they become callable.
// Example: mmapi_debug_register_fn("warp_to_room", my_mod_debug_warp_to_room);
//
// opts is optional:
//   { mod_name }    attribution, defaulting to the current mod.
//   { description } one-line summary surfaced in the Call Lab.
//   { args }        array of { name, type, default } arg descriptors, so the
//                   client can render a typed argument form.
// description and args are published in state.json.functions, the catalog. The
// `fn` method itself is never serialized.
function mmapi_debug_register_fn(name, fn, opts) {
    var state = __mmapi_debug_state();
    var mod_name = mmapi_current_mod();
    var description = "";
    var args = [];
    if (opts != undefined) {
        if (opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }
        if (opts[$ "description"] != undefined) { description = opts.description; }
        if (is_array(opts[$ "args"])) { args = opts.args; }
    }
    state.fns[$ name] = { fn: fn, mod_name: mod_name, description: description, args: args };
}

// Call args pass through as JSON literals. A {"$ref": "path"} arg resolves to
// the live value first; an unresolved path becomes undefined.
function __mmapi_debug_resolve_args(raw_args) {
    if (is_array(raw_args) == false) { return []; }
    var out = [];
    for (var i = 0; i < array_length(raw_args); i++) {
        var value = raw_args[i];
        if (is_struct(value) && struct_exists(value, "$ref")) {
            var resolved = mmapi_debug_resolve(value[$ "$ref"]);
            if (mmapi_debug_is_unresolved(resolved)) {
                array_push(out, undefined);
            } else {
                array_push(out, resolved);
            }
        } else {
            array_push(out, value);
        }
    }
    return out;
}

// The throwing half, wrapped by the guarded caller below.
// request = {callable, args}, dispatched variadically through method_call.
function __mmapi_debug_invoke_raw(request) {
    return method_call(request.callable, request.args);
}

function __mmapi_debug_apply_call(command) {
    var state = __mmapi_debug_state();
    var args = __mmapi_debug_resolve_args(command[$ "args"]);
    var fn_name = command[$ "fn"];
    var path = command[$ "path"];
    var callable = undefined;
    var label = "?";

    if (fn_name != undefined) {
        label = "fn:" + string(fn_name);
        var record = state.fns[$ fn_name];
        if (record != undefined) { callable = record.fn; }
    } else if (is_string(path) && path != "") {
        label = path;
        var parts = string_split(path, ".");
        var part_count = array_length(parts);
        if (part_count == 0) { parts = [path]; part_count = 1; }
        if (part_count < 2) {
            mmapi_log_warn(MMAPI_DEBUG_MOD, "[call] need receiver.method (or fn): " + path);
            return;
        }
        var method_name = parts[part_count - 1];
        var receiver_path = parts[0];
        for (var i = 1; i < part_count - 1; i++) { receiver_path += "." + parts[i]; }
        var receiver = mmapi_debug_resolve(receiver_path);
        if (mmapi_debug_is_unresolved(receiver)) {
            mmapi_log_warn(MMAPI_DEBUG_MOD, "[call] receiver unresolved: " + receiver_path);
            return;
        }
        var member = __mmapi_debug_member(receiver, method_name);
        if (mmapi_debug_is_unresolved(member) == false) { callable = member; }
    } else {
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[call] needs `path` or `fn`");
        return;
    }

    if (callable == undefined || typeof(callable) != "method") {
        state.last_call = { target: label, args: args, error: "not callable", game_frame: state.game_frame };
        mmapi_log_warn(MMAPI_DEBUG_MOD, "[call] not callable: " + label);
        return;
    }

    var result = undefined;
    var threw = false;
    try {
        result = __mmapi_debug_invoke_raw({ callable: callable, args: args });
    } catch (err) {
        state.last_call = { target: label, args: args, error: string(err), game_frame: state.game_frame };
        mmapi_log_error(MMAPI_DEBUG_MOD, "[call] " + label + " THREW: " + string(err));
        threw = true;
    }
    if (threw) { return; }
    state.last_call = { target: label, args: args, result: result, game_frame: state.game_frame };
    mmapi_log_info(MMAPI_DEBUG_MOD, "[call] " + label + " -> " + __mmapi_debug_safe_json(result));
}

// ── control.json ──────────────────────────────────────────────────────
// Adopts the watch list and breakpoints, and runs the rev-gated commands.

function __mmapi_debug_resume(state) {
    state.paused = false;
    state.step_request = false;
    state.active_break = undefined;   // resumed, so no longer paused at a break
    PAUSE_STATUS = remove_flag(PAUSE_STATUS, PauseStatus.WINDOW);
}

// ── keys ──────────────────────────────────────────────────────────────
// Enumerate a container's child names at a dotted path, powering the client's
// path autocomplete. The result is published in state.json.last_keys.

// The throwing half, wrapped by the guarded caller below. Returns the child
// names of the container at `path`.
function __mmapi_debug_keys_raw(path) {
    var value = mmapi_debug_resolve(path);
    if (mmapi_debug_is_unresolved(value)) { return []; }
    if (is_array(value)) {
        var out = [];
        var n = array_length(value);
        for (var i = 0; i < n && i < 500; i++) { array_push(out, string(i)); }
        return out;
    }
    if (is_struct(value)) { return struct_get_names(value); }
    return [];   // instances and primitives have no struct-enumerable keys
}

function __mmapi_debug_apply_keys(command) {
    var state = __mmapi_debug_state();
    var path = command[$ "path"];
    if (is_string(path) == false || path == "") { return; }
    var keys = [];
    try {
        var raw_keys = __mmapi_debug_keys_raw(path);
        if (is_array(raw_keys)) { keys = raw_keys; }
    } catch (err) {
        // keys stays []: an unreadable path lists nothing
    }
    if (array_length(keys) > 300) {
        var capped = [];
        for (var i = 0; i < 300; i++) { array_push(capped, keys[i]); }
        keys = capped;
    }
    state.last_keys = { path: path, keys: keys };
}

function __mmapi_debug_apply_commands(state, data) {
    var rev = data[$ "rev"];
    if (rev == undefined) { return; }
    if (rev <= state.applied_rev) { return; }   // already applied, or stale
    state.applied_rev = rev;
    state.force_emit = true;   // flush applied_rev this frame, so a command whose
                               // effect crashes a later frame does not replay on restart

    var commands = data[$ "commands"];
    if (is_array(commands) == false) { return; }
    for (var i = 0; i < array_length(commands); i++) {
        var command = commands[i];
        if (is_struct(command) == false) { continue; }
        var op = command[$ "op"];
        if (op == "pause") {
            state.paused = true;
            state.step_request = false;
        } else if (op == "resume") {
            __mmapi_debug_resume(state);
        } else if (op == "step") {
            if (state.paused) { state.step_request = true; }
        } else if (op == "set") {
            __mmapi_debug_apply_set(command[$ "path"], command[$ "value"]);
        } else if (op == "call") {
            __mmapi_debug_apply_call(command);
        } else if (op == "keys") {
            __mmapi_debug_apply_keys(command);
        } else {
            mmapi_log_warn(MMAPI_DEBUG_MOD, "[cmd] unknown op: " + string(op));
            continue;
        }
        mmapi_log_info(MMAPI_DEBUG_MOD, "[cmd rev=" + string(rev) + "] applied " + string(op));
    }
}

function __mmapi_debug_poll_control(state) {
    var path = __mmapi_debug_control_path();
    if (file_exists(path) == false) { return; }
    var data = try_read_json_file(path, undefined, false);
    if (is_struct(data) == false) { return; }
    var watch_paths = data[$ "watch_paths"];
    if (is_array(watch_paths)) { state.watch_paths = watch_paths; }
    var breakpoints = data[$ "breakpoints"];
    if (is_array(breakpoints)) { state.breakpoints = breakpoints; }
    __mmapi_debug_apply_commands(state, data);
}

// ── Breakpoints ───────────────────────────────────────────────────────
// Auto-pause on a condition. "edge", the client default, fires on the
// false→true transition only: a newly-added edge breakpoint primes to the
// current condition on first sight, so one already true at creation arms rather
// than firing, then fires when the value leaves the condition and re-enters it.
// "level" fires every frame the condition holds, the way a visual debugger
// does, where Resume advances one frame.

// The throwing half, wrapped by the guarded caller below.
// request = {current, op, target}. A mixed-type comparison can throw, and the
// guarded caller reports that as not-satisfied.
function __mmapi_debug_compare_raw(request) {
    var current = request.current;
    var op = request.op;
    var target = request.target;
    if (op == "==") { return current == target; }
    if (op == "!=") { return current != target; }
    if (op == "<")  { return current <  target; }
    if (op == "<=") { return current <= target; }
    if (op == ">")  { return current >  target; }
    if (op == ">=") { return current >= target; }
    return false;
}

function __mmapi_debug_condition_holds(current, op, target) {
    if (mmapi_debug_is_unresolved(current)) { return false; }
    try {
        return __mmapi_debug_compare_raw({ current: current, op: op, target: target }) == true;
    } catch (err) {
        return false;
    }
}

// A stable per-breakpoint key for edge-state tracking: path|op|value
function __mmapi_debug_bp_signature(bp) {
    return string(bp[$ "path"]) + "|" + string(bp[$ "op"]) + "|" + string(bp[$ "value"]);
}

function __mmapi_debug_check_breakpoints(state) {
    // Resume-grace: let a manual step take its frame, and let the just-resumed
    // frame's logic run, before evaluating again. Otherwise a "level"
    // breakpoint re-pauses before any frame advances, stranding the user, and a
    // step would be cancelled.
    if (state.step_request) { return; }
    if (state.entry_paused == true && state.paused == false) { return; }

    var breakpoints = state.breakpoints;
    if (is_array(breakpoints) == false) { return; }
    for (var i = 0; i < array_length(breakpoints); i++) {
        var bp = breakpoints[i];
        if (is_struct(bp) == false) { continue; }
        // A breakpoint is enabled unless it is explicitly false. A missing field
        // compares `== false` as true, so presence is gated first; otherwise
        // every breakpoint without an explicit "enabled" key would be skipped.
        var enabled = bp[$ "enabled"];
        if (enabled != undefined && enabled == false) { continue; }

        var path = bp[$ "path"];
        var op = bp[$ "op"];
        var target = bp[$ "value"];
        var current = mmapi_debug_resolve(path);
        var holds = __mmapi_debug_condition_holds(current, op, target);

        var signature = __mmapi_debug_bp_signature(bp);
        var bp_record = state.bp_state[$ signature];
        var first_sight = (is_struct(bp_record) == false);
        var previous = false;
        if (first_sight == false) { previous = bp_record.was; }

        // Presence-guarded, the same as `enabled` above.
        var mode = bp[$ "mode"];
        var is_edge = (mode != undefined && mode == "edge");
        var should_fire;
        if (is_edge) {
            // Fire only on a genuine false→true transition. On first sight of a
            // newly-added breakpoint, prime the baseline to the current
            // condition instead of firing: an already-true condition is not a
            // transition, so it arms and fires on re-entry.
            should_fire = (holds && previous != true && first_sight == false);
        } else {
            should_fire = holds;   // "level" fires every frame the condition holds
        }

        if (should_fire) {
            state.paused = true;
            state.step_request = false;
            var got;
            if (mmapi_debug_is_unresolved(current)) { got = "<unresolved>"; } else { got = current; }
            var kind;
            if (is_edge) { kind = "edge"; } else { kind = "level"; }
            state.last_break = {
                kind: kind,
                path: path, op: op, value: target,
                got: got,
                game_frame: state.game_frame,
            };
            state.active_break = state.last_break;   // paused here, and the client highlights it
            mmapi_log_info(MMAPI_DEBUG_MOD,
                "[break] " + string(path) + " " + string(op) + " " + __mmapi_debug_safe_json(target)
                + " (got " + __mmapi_debug_safe_json(got) + ") @ game_frame " + string(state.game_frame));
        }
        state.bp_state[$ signature] = { was: holds };
    }

    // Prune edge-state for breakpoints that are no longer present, so a
    // deleted-then-recreated breakpoint primes fresh on first sight instead of
    // inheriting a stale baseline. A breakpoint that is disabled but still
    // present keeps its state: disable is a pause, not a reset.
    var live = {};
    for (var li = 0; li < array_length(breakpoints); li++) {
        var lbp = breakpoints[li];
        if (is_struct(lbp)) { live[$ __mmapi_debug_bp_signature(lbp)] = true; }
    }
    var known = struct_get_names(state.bp_state);
    for (var ki = 0; ki < array_length(known); ki++) {
        if (live[$ known[ki]] == undefined) { struct_remove(state.bp_state, known[ki]); }
    }
}

// Drop this into mod code to instrument it: it pauses, edge-triggered per
// label, when reached with cond true.
// Example: mmapi_debug_break("level_start", my_mod_current_level() == 3);
// Inert until the agent is enabled, so it does no config IO from hot paths.
function mmapi_debug_break(label, cond) {
    var state = __mmapi_debug_state();
    if (state.enabled_checked == false || state.enabled == false) { return; }
    if (cond != true) {
        state.label_state[$ label] = false;
        return;
    }
    var previous = state.label_state[$ label];
    if (previous != true) {
        state.paused = true;
        state.step_request = false;
        state.last_break = { kind: "mmapi_debug_break", label: label, game_frame: state.game_frame };
        state.active_break = state.last_break;
        mmapi_log_info(MMAPI_DEBUG_MOD, "[break] mmapi_debug_break('" + string(label) + "') @ game_frame " + string(state.game_frame));
    }
    state.label_state[$ label] = true;
}

// Like mmapi_debug_break, but it fires every time it is reached, with no edge
// and no condition: stop each time this event happens. Not for a per-frame
// line, which would re-pause every frame after resume.
function mmapi_debug_break_each(label) {
    var state = __mmapi_debug_state();
    if (state.enabled_checked == false || state.enabled == false) { return; }
    state.paused = true;
    state.step_request = false;
    state.last_break = { kind: "mmapi_debug_break_each", label: label, game_frame: state.game_frame };
    state.active_break = state.last_break;
    mmapi_log_info(MMAPI_DEBUG_MOD, "[break] mmapi_debug_break_each('" + string(label) + "') @ game_frame " + string(state.game_frame));
}

// ── state.json emit ───────────────────────────────────────────────────
// Each section is isolated: every watch and every aggregate field encodes
// through __mmapi_debug_safe_json, so a single bad value cannot blank the whole
// snapshot.

function __mmapi_debug_watches_json(state) {
    var paths = state.watch_paths;
    var count = array_length(paths);
    var out = "{";
    for (var i = 0; i < count; i++) {
        var path = string(paths[i]);
        var value = mmapi_debug_resolve(path);
        var encoded;
        if (mmapi_debug_is_unresolved(value)) {
            encoded = "\"<unresolved>\"";
        } else {
            encoded = __mmapi_debug_safe_json(value);
        }
        if (i > 0) { out += ","; }
        out += __mmapi_debug_json_quote(path) + ":" + encoded;
    }
    return out + "}";
}

// The published callable catalog, carrying name, mod_name, description and args
// as plain values. The `fn` method is excluded: it serializes as
// <unserializable> and is not transportable anyway. Encoded through the guarded
// serializer, so a single odd arg descriptor can never blank the snapshot.
function __mmapi_debug_functions_json(state) {
    var names = struct_get_names(state.fns);
    var count = array_length(names);
    var list = [];
    for (var i = 0; i < count; i++) {
        var name = names[i];
        var record = state.fns[$ name];
        if (is_struct(record) == false) { continue; }
        var description = record[$ "description"];
        if (description == undefined) { description = ""; }
        var args = record[$ "args"];
        if (is_array(args) == false) { args = []; }
        array_push(list, {
            name: name,
            mod_name: record[$ "mod_name"],
            description: description,
            args: args,
        });
    }
    return __mmapi_debug_safe_json(list);
}

// The throwing half, wrapped by the guarded caller below: the state.json write.
function __mmapi_debug_write_state_raw(json) {
    save_text_file(__mmapi_debug_state_path(), json);
    return true;
}

function __mmapi_debug_emit_state(state, force) {
    // `force` marks a durability flush of a freshly-bumped applied_rev, which
    // must persist even when the user toggled cadence snapshots off with F8. A
    // normal cadence emit (force != true) still honours state.emit. Without
    // this, a command applied while emit is off would never flush applied_rev
    // and would replay on restart, which can become a crash loop.
    if (force != true && state.emit == false) { return; }

    var json = "{"
        + "\"rev\":" + string(state.engine_frame)
        + ",\"applied_rev\":" + string(state.applied_rev)
        + ",\"paused\":" + __mmapi_debug_bool_json(state.paused)
        + ",\"game_frame\":" + string(state.game_frame)
        + ",\"engine_frame\":" + string(state.engine_frame)
        + ",\"step_request\":" + __mmapi_debug_bool_json(state.step_request)
        + ",\"pause_status\":" + string(PAUSE_STATUS)
        + ",\"last_break\":" + __mmapi_debug_safe_json(state.last_break)
        + ",\"active_break\":" + __mmapi_debug_safe_json(state.active_break)
        + ",\"last_call\":" + __mmapi_debug_safe_json(state.last_call)
        + ",\"last_keys\":" + __mmapi_debug_safe_json(state.last_keys)
        + ",\"breakpoints\":" + __mmapi_debug_safe_json(state.breakpoints)
        + ",\"caps\":" + __mmapi_debug_safe_json(state.caps)
        + ",\"functions\":" + __mmapi_debug_functions_json(state)
        + ",\"watches\":" + __mmapi_debug_watches_json(state)
        + "}";

    try {
        __mmapi_debug_write_state_raw(json);
    } catch (err) {
        mmapi_warn_rate_limited("debug:state_write", MMAPI_DEBUG_MOD,
            "debug state.json write failed: " + string(err));
    }
}

// ── Rehydrate applied_rev across a restart ────────────────────────────
// The in-memory applied_rev resets to 0 each launch, but control.json persists
// on disk, so without this the agent re-runs the last command batch every
// launch and a command whose effect crashes becomes a crash loop. This restores
// the high-water applied_rev from the agent's own last-emitted state.json, so a
// command runs at most once across restarts. It is crash-safe because
// apply_commands force-emits the bumped rev the same frame it applies, so the
// rev on disk reflects the command even if a later frame dies. A genuinely new
// command always carries a higher rev, since the client bumps past applied_rev,
// so this never suppresses fresh work.
function __mmapi_debug_rehydrate(state) {
    var path = __mmapi_debug_state_path();
    if (file_exists(path) == false) { return; }
    var prev = try_read_json_file(path, undefined, false);
    if (is_struct(prev) == false) { return; }
    var prev_applied = prev[$ "applied_rev"];
    if (is_real(prev_applied) && prev_applied > state.applied_rev) {
        state.applied_rev = prev_applied;
        mmapi_log_info(MMAPI_DEBUG_MOD,
            "[init] rehydrated applied_rev=" + string(prev_applied) + " from prior session");
    }
}

// ── Capability probe ──────────────────────────────────────────────────
// Run once at enable, recording what this engine build actually supports and
// surfacing it via state.json.caps.

// The identity probe, called through __mmapi_debug_invoke_raw to confirm
// dynamic invocation works at all on this engine.
function __mmapi_debug_probe_identity(value) {
    return value;
}

function __mmapi_debug_probe() {
    var state = __mmapi_debug_state();
    var caps = {
        global_accessor: false,
        struct_get: false,
        method_call: false,
        split_no_delim_empty: false,
    };
    try { caps.global_accessor = (global[$ "__mmapi_debug"] == state); } catch (err) {}
    try { caps.struct_get = is_real(struct_get(state, "engine_frame")); } catch (err) {}
    try {
        var split_result = string_split("abc", ".");
        caps.split_no_delim_empty = (array_length(split_result) == 0);
    } catch (err) {}
    try {
        var probe = __mmapi_debug_invoke_raw(
            { callable: __mmapi_debug_probe_identity, args: [42] });
        caps.method_call = (probe == 42);
    } catch (err) {
        caps.method_call = false;
    }
    state.caps = caps;
}

// ── Hotkeys ───────────────────────────────────────────────────────────
// Registered through the registry, installed only once the agent is enabled, so
// a disabled debugger never claims F8, F9 or F10.

function __mmapi_debug_hotkey_toggle_pause() {
    var state = __mmapi_debug_state();
    if (state.paused) {
        __mmapi_debug_resume(state);
    } else {
        state.paused = true;
        state.step_request = false;
    }
    mmapi_log_info(MMAPI_DEBUG_MOD, "[F9] paused=" + string(state.paused)
        + " game_frame=" + string(state.game_frame));
}

function __mmapi_debug_hotkey_step() {
    var state = __mmapi_debug_state();
    if (state.paused) { state.step_request = true; }
}

function __mmapi_debug_hotkey_toggle_emit() {
    var state = __mmapi_debug_state();
    state.emit = (state.emit == false);
    mmapi_log_info(MMAPI_DEBUG_MOD, "[F8] state emit=" + string(state.emit));
}

function __mmapi_debug_install_hotkeys(state) {
    if (state.hotkeys_installed) { return; }
    state.hotkeys_installed = true;
    mmapi_hotkey_register(vk_f9, __mmapi_debug_hotkey_toggle_pause, { mod_name: "mmapi" });
    mmapi_hotkey_register(vk_f10, __mmapi_debug_hotkey_step, { mod_name: "mmapi" });
    mmapi_hotkey_register(vk_f8, __mmapi_debug_hotkey_toggle_emit, { mod_name: "mmapi" });
}

// ── The per-frame tick ────────────────────────────────────────────────
// Runs from Game begin_step via the install drain, which calls it with no
// arguments and contains any throw. Its position lets it both freeze the coming
// frame and always run to un-freeze it.

function mmapi_debug_tick() {
    var state = __mmapi_debug_state();
    if (__mmapi_debug_is_enabled(state) == false) { return; }

    state.engine_frame += 1;
    state.entry_paused = state.paused;

    if (state.probed == false) {
        state.probed = true;
        __mmapi_debug_rehydrate(state);   // applied_rev before the first poll, so nothing replays across restarts
        __mmapi_debug_probe();
        __mmapi_debug_install_hotkeys(state);
        global.__mmapi_debug_stats = mmapi_hook_stats();
        mmapi_log_info(MMAPI_DEBUG_MOD,
            "MmapiDebugger agent enabled. F9 pause/resume, F10 step, F8 toggle emit. "
            + "Files under " + __mmapi_debug_dir());
    }

    // control.json carries the watch list and the command channel. Polled
    // before the pause drive, so a pause, resume, step, set or call command
    // takes effect this same frame; every frame while paused, every
    // control_every frames otherwise.
    state.control_count += 1;
    var poll = state.paused;
    if (state.control_count >= state.control_every) {
        state.control_count = 0;
        poll = true;
    }
    if (poll) {
        __mmapi_debug_poll_control(state);
        // A fresh stats snapshot under a watchable global on every poll, so
        // hook wiring stays observable live.
        global.__mmapi_debug_stats = mmapi_hook_stats();
    }

    // Conditional breakpoints before the pause drive, with resume-grace.
    __mmapi_debug_check_breakpoints(state);

    // Drive the pause. game_frame advances only on frames whose logic runs.
    if (state.paused) {
        if (state.step_request) {
            PAUSE_STATUS = remove_flag(PAUSE_STATUS, PauseStatus.WINDOW);   // release exactly one frame
            state.step_request = false;
            state.game_frame += 1;
            state.active_break = undefined;   // stepped past it; a still-holding level bp re-fires next frame
        } else {
            PAUSE_STATUS = set_flag(PAUSE_STATUS, PauseStatus.WINDOW);      // hold, so logic stays frozen
        }
    } else {
        state.game_frame += 1;
    }

    // Snapshot every frame while paused, every emit_every frames otherwise.
    var should_emit = state.paused;
    state.emit_count += 1;
    if (state.emit_count >= state.emit_every) {
        state.emit_count = 0;
        should_emit = true;
    }
    // A command batch that applied this frame flushes now rather than on the
    // next cadence tick, so the bumped applied_rev is durable before a command
    // whose effect crashes a later frame can take the game down. The durability
    // flush passes force=true and ignores the F8 emit toggle; a normal cadence
    // emit honours it.
    var force = state.force_emit;
    state.force_emit = false;
    if (force) {
        __mmapi_debug_emit_state(state, true);
    } else if (should_emit) {
        __mmapi_debug_emit_state(state, false);
    }
}

__mmapi_register_as(mmapi_debug_tick, "mmapi");
