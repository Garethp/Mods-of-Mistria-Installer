// mmapi_hotkeys.gml. The hotkey registry: mods register a keyboard vk (or a
// gamepad button) → callback, and the module polls once per frame through its
// own lifecycle install - keyboard_check_pressed for keyboard entries,
// gamepad_button_check_pressed across connected pads for gamepad entries. A
// binding registered by more than one mod logs a conflict Warn and both stay
// registered, so a collision never silently drops one.

// Button name → keyboard virtual-key code, undefined when the name is not a
// supported keyboard key. This is the KEYBOARD vocabulary a mod's config
// validates against: F1-F12, NUMPAD_0-9, single digits 0-9, single letters
// A-Z, and the named specials. Gamepad names (GAMEPAD_*) return undefined
// here BY DESIGN - they live in mmapi_hotkey_pad_from_name, so a config
// accepting both families validates with
// (vk_from_name(x) != undefined || pad_from_name(x) != undefined).
function mmapi_hotkey_vk_from_name(name) {
    if (!is_string(name)) { return undefined; }

    // A single digit or letter maps to its ASCII code (keyboard_check uses those).
    if (string_length(name) == 1) {
        var code = ord(name);
        if (code >= ord("0") && code <= ord("9")) { return code; }
        if (code >= ord("A") && code <= ord("Z")) { return code; }
        return undefined;
    }

    switch (name) {
        case "F1":  return vk_f1;  case "F2":  return vk_f2;  case "F3":  return vk_f3;
        case "F4":  return vk_f4;  case "F5":  return vk_f5;  case "F6":  return vk_f6;
        case "F7":  return vk_f7;  case "F8":  return vk_f8;  case "F9":  return vk_f9;
        case "F10": return vk_f10; case "F11": return vk_f11; case "F12": return vk_f12;

        case "INSERT":      return vk_insert;
        case "DELETE":      return vk_delete;
        case "HOME":        return vk_home;
        case "PAGE_UP":     return vk_pageup;
        case "PAGE_DOWN":   return vk_pagedown;
        case "SHIFT":       return vk_shift;
        case "CONTROL":     return vk_control;

        // The engine's KeyCode table only covers letters, digits, F1-F12, the
        // navigation cluster (INSERT/DELETE/HOME/PAGE_UP/PAGE_DOWN), SHIFT, and
        // CONTROL. Everything below is NOT in it: the vk_ constants are unbound and
        // keyboard_check rejects the raw Windows codes (18/19, 96-105, 20/144/145) as
        // "a number out of range". Resolve them to undefined so mods take their
        // designed invalid-name path (warn + default binding). The boot capability
        // sweep (mmapi_hotkey_capability_report) verifies this table every session and
        // warns if the engine ever starts accepting or rejecting differently.
        case "ALT":         return undefined;
        case "PAUSE_BREAK": return undefined;
        case "NUMPAD_0": case "NUMPAD_1": case "NUMPAD_2": case "NUMPAD_3":
        case "NUMPAD_4": case "NUMPAD_5": case "NUMPAD_6": case "NUMPAD_7":
        case "NUMPAD_8": case "NUMPAD_9":
            return undefined;
        case "CAPS_LOCK":   return undefined;
        case "NUM_LOCK":    return undefined;
        case "SCROLL_LOCK": return undefined;
    }

    return undefined;
}

// Reverse of mmapi_hotkey_vk_from_name: the friendly button name a vk resolves to,
// for human-readable diagnostics (the conflict Warn / poll-failure Warn) instead of
// a bare ordinal. A single digit/letter reverses straight to its character; the named
// keys (F1-F12, NUMPAD_*, specials) probe the forward map so the two never drift.
// Falls back to "vk <ordinal>" for a code with no supported name.
function mmapi_hotkey_name_from_vk(vk) {
    if (!is_real(vk)) { return "vk " + string(vk); }

    // Digit or letter: the forward map used the ASCII code directly (ord). Reverse it
    // by indexing the contiguous vocabulary (via string_char_at + ord rather than chr,
    // which the live runtime has but a headless VM's stdlib may not).
    if (vk >= ord("0") && vk <= ord("9")) {
        return string_char_at("0123456789", vk - ord("0") + 1);
    }
    if (vk >= ord("A") && vk <= ord("Z")) {
        return string_char_at("ABCDEFGHIJKLMNOPQRSTUVWXYZ", vk - ord("A") + 1);
    }

    // Named keys: find the name whose forward lookup yields this vk. This only runs on
    // a conflict (or a failed callback), so a linear scan of the vocabulary is fine.
    // Each probe is guarded: the forward map reads bare vk_* constants, which the live
    // runtime defines and a headless VM does not, and a diagnostics path must never
    // throw - a name that does not resolve just falls through to the "vk <ordinal>" form.
    var names = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "NUMPAD_0", "NUMPAD_1", "NUMPAD_2", "NUMPAD_3", "NUMPAD_4",
        "NUMPAD_5", "NUMPAD_6", "NUMPAD_7", "NUMPAD_8", "NUMPAD_9",
        "INSERT", "DELETE", "HOME", "PAGE_UP", "PAGE_DOWN", "SHIFT", "ALT",
        "CONTROL", "PAUSE_BREAK", "CAPS_LOCK", "NUM_LOCK", "SCROLL_LOCK",
    ];
    for (var i = 0; i < array_length(names); i++) {
        var candidate = undefined;
        try { candidate = mmapi_hotkey_vk_from_name(names[i]); } catch (__mmapi_hotkey_vk_probe) {}
        if (candidate == vk) { return names[i]; }
    }
    return "vk " + string(vk);
}

// The GAMEPAD half of the vocabulary: button name → gp_* button code, undefined
// when the name is not a supported pad button. This is the pad vocabulary a
// mod's config validates against: face buttons, shoulders/triggers, dpad,
// stick clicks, SELECT, and START.
// The gp_* constants are live-engine bindings a headless VM does not define, the
// same way the vk_* constants are: callers on a diagnostics path guard the call.
// Shoulder naming follows the engine's own keycode_to_string: gp_shoulderl/r are
// the bumpers, gp_shoulderlb/rb the triggers.
function mmapi_hotkey_pad_from_name(name) {
    if (!is_string(name)) { return undefined; }
    switch (name) {
        case "GAMEPAD_A": return gp_face1;
        case "GAMEPAD_B": return gp_face2;
        case "GAMEPAD_X": return gp_face3;
        case "GAMEPAD_Y": return gp_face4;
        case "GAMEPAD_LEFT_SHOULDER":  return gp_shoulderl;
        case "GAMEPAD_RIGHT_SHOULDER": return gp_shoulderr;
        case "GAMEPAD_LEFT_TRIGGER":   return gp_shoulderlb;
        case "GAMEPAD_RIGHT_TRIGGER":  return gp_shoulderrb;
        case "GAMEPAD_DPAD_UP":    return gp_padu;
        case "GAMEPAD_DPAD_DOWN":  return gp_padd;
        case "GAMEPAD_DPAD_LEFT":  return gp_padl;
        case "GAMEPAD_DPAD_RIGHT": return gp_padr;
        case "GAMEPAD_LEFT_STICK":  return gp_stickl;
        case "GAMEPAD_RIGHT_STICK": return gp_stickr;
        case "GAMEPAD_SELECT": return gp_select;
        case "GAMEPAD_START":  return gp_start;
    }
    return undefined;
}

// The pad-name vocabulary, shared by the reverse lookup and the capability sweep.
function __mmapi_hotkey_pad_names() {
    return [
        "GAMEPAD_A", "GAMEPAD_B", "GAMEPAD_X", "GAMEPAD_Y",
        "GAMEPAD_LEFT_SHOULDER", "GAMEPAD_RIGHT_SHOULDER",
        "GAMEPAD_LEFT_TRIGGER", "GAMEPAD_RIGHT_TRIGGER",
        "GAMEPAD_DPAD_UP", "GAMEPAD_DPAD_DOWN", "GAMEPAD_DPAD_LEFT", "GAMEPAD_DPAD_RIGHT",
        "GAMEPAD_LEFT_STICK", "GAMEPAD_RIGHT_STICK",
        "GAMEPAD_SELECT", "GAMEPAD_START",
    ];
}

// Reverse of mmapi_hotkey_pad_from_name, for the pad conflict / poll-failure
// Warns. Same guarded-probe shape as mmapi_hotkey_name_from_vk: a diagnostics
// path must never throw, so an unresolvable code (a headless VM, or a raw code
// no name maps to) falls back to "pad button <ordinal>".
function mmapi_hotkey_name_from_pad(button) {
    if (!is_real(button)) { return "pad button " + string(button); }
    var names = __mmapi_hotkey_pad_names();
    for (var i = 0; i < array_length(names); i++) {
        var candidate = undefined;
        try { candidate = mmapi_hotkey_pad_from_name(names[i]); } catch (__mmapi_hotkey_pad_probe) {}
        if (candidate == button) { return names[i]; }
    }
    return "pad button " + string(button);
}

function mmapi_hotkey_register(vk, callback, opts) {
    if (global[$ "__mmapi_hotkeys"] == undefined) { global.__mmapi_hotkeys = []; }
    var hotkeys = global.__mmapi_hotkeys;

    var mod_name = mmapi_current_mod();
    if (opts != undefined && opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }

    // The engine validates KeyCodes: keyboard_check throws "expected a valid numerical
    // KeyCode" for codes outside its key table. Probe ONCE at registration so a bad
    // binding is one clear warn and a cleanly absent hotkey, never a per-tick failure
    // storm from the poll. The rejection only counts when
    // keyboard_check demonstrably WORKS in this environment (a known-good code succeeds):
    // headless test VMs without the keyboard builtins must not reject every registration -
    // there the poll's own per-entry guard remains the backstop.
    var probe_failed = false;
    try { keyboard_check(vk); } catch (__mmapi_hotkey_probe) { probe_failed = true; }
    if (probe_failed) {
        var env_has_keyboard = false;
        try { keyboard_check(vk_shift); env_has_keyboard = true; } catch (__mmapi_hotkey_env) {}
        if (env_has_keyboard) {
            mmapi_log_warn(mod_name,
                "mmapi hotkey " + mmapi_hotkey_name_from_vk(vk) + " (vk " + string(vk)
                + ") from " + mod_name + " rejected: the engine has no KeyCode for it. "
                + "The hotkey is disabled");
            return;
        }
    }

    for (var i = 0; i < array_length(hotkeys); i++) {
        if (hotkeys[i].vk == vk) {
            mmapi_log_warn(mod_name,
                "mmapi hotkey conflict: " + mmapi_hotkey_name_from_vk(vk) + " is registered by "
                + hotkeys[i].mod_name + " and now also by " + mod_name
                + ". Both will fire");
        }
    }

    array_push(hotkeys, { vk: vk, callback: callback, mod_name: mod_name });
}

// The gamepad twin of mmapi_hotkey_register: button is a gp_* code (resolve a
// config name through mmapi_hotkey_pad_from_name first). A separate registry
// keeps the namespaces distinct - F1 and GAMEPAD_A can never conflict - and the
// keyboard path byte-identical for existing registrants. No register-time engine
// probe: the keyboard probe leans on keyboard_check being side-effect-free and
// always answerable, but a pad code's validity is only observable against a
// CONNECTED device, and none may be plugged in at registration. The poll's
// per-entry dead-marking is the backstop for a genuinely bad code, and an
// unplugged controller is NOT an error - the entry just waits for one.
function mmapi_hotkey_register_pad(button, callback, opts) {
    if (global[$ "__mmapi_pad_hotkeys"] == undefined) { global.__mmapi_pad_hotkeys = []; }
    var hotkeys = global.__mmapi_pad_hotkeys;

    var mod_name = mmapi_current_mod();
    if (opts != undefined && opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }

    for (var i = 0; i < array_length(hotkeys); i++) {
        if (hotkeys[i].button == button) {
            mmapi_log_warn(mod_name,
                "mmapi hotkey conflict: " + mmapi_hotkey_name_from_pad(button) + " is registered by "
                + hotkeys[i].mod_name + " and now also by " + mod_name
                + ". Both will fire");
        }
    }

    array_push(hotkeys, { button: button, callback: callback, mod_name: mod_name });
}

// One-shot boot sweep: probe EVERY name in the hotkey vocabulary (plus the raw codes
// of every resolver-unsupported key) against the live engine's KeyCode table and log
// the verdict. Quiet on
// the expected outcome (one TRACE-gated [PROBE] line, flushed immediately while the
// debug agent is on); a WARN per name that RESOLVES to a
// code the engine then rejects (a resolver bug). Probes
// the CODE SPACE only: a supported code does not guarantee the physical key delivers
// (numpad-as-navigation with Num Lock off, for example). Skips silently in
// environments without the keyboard builtins (headless test VMs). Re-runnable on
// demand through the debug agent as mmapi_debug_hotkey_capability.
function mmapi_hotkey_capability_report() {
    var env_ok = false;
    try { keyboard_check(vk_shift); env_ok = true; } catch (__mmapi_cap_env) {}
    if (!env_ok) { return "no_keyboard_env"; }

    var names = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "NUMPAD_0", "NUMPAD_1", "NUMPAD_2", "NUMPAD_3", "NUMPAD_4",
        "NUMPAD_5", "NUMPAD_6", "NUMPAD_7", "NUMPAD_8", "NUMPAD_9",
        "INSERT", "DELETE", "HOME", "PAGE_UP", "PAGE_DOWN", "SHIFT", "ALT",
        "CONTROL", "PAUSE_BREAK", "CAPS_LOCK", "NUM_LOCK", "SCROLL_LOCK",
        "0", "9", "A", "Z",
    ];
    var ok_count = 0;
    var no_code = "";
    var rejected = "";
    for (var i = 0; i < array_length(names); i++) {
        var vk = undefined;
        try { vk = mmapi_hotkey_vk_from_name(names[i]); } catch (__mmapi_cap_resolve) {}
        if (vk == undefined) {
            no_code += (no_code == "" ? "" : ", ") + names[i];
            continue;
        }
        var accepted = false;
        try { keyboard_check(vk); accepted = true; } catch (__mmapi_cap_probe) {}
        if (accepted) {
            ok_count += 1;
        } else {
            rejected += (rejected == "" ? "" : ", ") + names[i] + "(vk " + string(vk) + ")";
        }
    }
    // Engine-side sentinel for every key the RESOLVER declares unsupported: probe the
    // raw Windows codes directly (the resolver has no path to them, so nothing else
    // would notice the engine's key table gaining one). The expected outcome is
    // compact ("none"); any code the engine now ACCEPTS is named - that is the signal
    // to re-add resolver support for its key (acceptance means the code is valid,
    // not necessarily that the physical key delivers - re-test before re-adding).
    var raw_codes = [18, 19, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 20, 144, 145];
    var raw_names = ["alt", "pause",
        "numpad0", "numpad1", "numpad2", "numpad3", "numpad4",
        "numpad5", "numpad6", "numpad7", "numpad8", "numpad9",
        "capslock", "numlock", "scrolllock"];
    var raw_accepted = "";
    for (var r = 0; r < array_length(raw_codes); r++) {
        var raw_ok = false;
        try { keyboard_check(raw_codes[r]); raw_ok = true; } catch (__mmapi_cap_raw) {}
        if (raw_ok) {
            raw_accepted += (raw_accepted == "" ? "" : ", ")
                + raw_names[r] + "(" + string(raw_codes[r]) + ")";
        }
    }
    if (raw_accepted == "") { raw_accepted = "none"; }

    // Gamepad leg: resolve every pad name (each probe guarded - the gp_* constants
    // are live-engine bindings) and count connected devices. A pad name that fails
    // to resolve on the LIVE engine is the pad-side resolver bug, warned exactly
    // like the keyboard's rejected list. pads_connected=0 is a normal outcome, not
    // an error - bindings wait for a controller.
    var pad_names = __mmapi_hotkey_pad_names();
    var pad_ok = 0;
    var pad_unresolved = "";
    for (var g = 0; g < array_length(pad_names); g++) {
        var pad_code = undefined;
        try { pad_code = mmapi_hotkey_pad_from_name(pad_names[g]); } catch (__mmapi_cap_pad) {}
        if (pad_code == undefined) {
            pad_unresolved += (pad_unresolved == "" ? "" : ", ") + pad_names[g];
        } else {
            pad_ok += 1;
        }
    }
    var pads_connected = 0;
    var pad_env = "ok";
    try {
        var pad_cap = undefined;
        try { pad_cap = GAMEPADS_COUNT; } catch (__mmapi_cap_pad_cap) {}
        if (pad_cap == undefined) { pad_cap = 12; }
        for (var pd = 0; pd < pad_cap; pd++) {
            if (gamepad_is_connected(pd)) { pads_connected += 1; }
        }
    } catch (__mmapi_cap_pad_env) {
        pad_env = "no_gamepad_env";
    }
    if (pad_unresolved != "" && pad_env == "ok") {
        mmapi_log_warn("mmapi", "gamepad hotkey names failing to resolve on the live engine: " + pad_unresolved);
    }

    var summary = "hotkey keycode capability: " + string(ok_count) + " name(s) supported"
        + (no_code == "" ? "" : "; no keycode (by design): " + no_code)
        + "; raw_accepted=" + raw_accepted
        + "; pad: " + string(pad_ok) + "/" + string(array_length(pad_names)) + " name(s) resolved"
        + ", env=" + pad_env + ", pads_connected=" + string(pads_connected);
    // A development diagnostic in the standard [PROBE] idiom: TRACE-gated, and the log
    // sink flushes [PROBE] lines immediately while the debug agent is on, so the line
    // is on disk right after boot in a --debug deploy with no forced flush of its own.
    // The WARN below is the user-facing signal.
    if (mmapi_log_get_level() <= MmapiLogLevel.Trace) {
        mmapi_log_trace("mmapi", "[PROBE] hotkeys|capability|supported=" + string(ok_count)
            + "|no_keycode=" + (no_code == "" ? "none" : no_code)
            + "|raw_accepted=" + raw_accepted
            + "|pad_supported=" + string(pad_ok)
            + "|pad_env=" + pad_env
            + "|pads_connected=" + string(pads_connected));
        mmapi_log_flush("mmapi");
    }
    if (rejected != "") {
        // A name the resolver maps to a code the engine refuses is a resolver bug -
        // loud, so it reaches user reports.
        mmapi_log_warn("mmapi", "hotkey names resolving to ENGINE-REJECTED keycodes: " + rejected);
    }
    return summary;
}

function mmapi_hotkeys_poll() {
    if (global[$ "__mmapi_hotkey_caps_done"] != true) {
        global.__mmapi_hotkey_caps_done = true;
        mmapi_hotkey_capability_report();
        try {
            mmapi_debug_register_fn("mmapi_debug_hotkey_capability", mmapi_hotkey_capability_report,
                { description: "Re-run the hotkey keycode capability sweep and return the summary line.", mod_name: "mmapi" });
        } catch (__mmapi_cap_reg) {}
    }
    var hotkeys = global[$ "__mmapi_hotkeys"];
    if (hotkeys == undefined) { return; }
    var count = array_length(hotkeys);
    for (var i = 0; i < count; i++) {
        var entry = hotkeys[i];
        if (entry[$ "dead"] == true) { continue; }
        // Belt-and-suspenders for the register-time probe: if the engine rejects this
        // entry's KeyCode at poll time anyway, disable the ENTRY (one warn), never the
        // whole poll - an unguarded throw here fails every registrant every tick.
        var pressed = false;
        try {
            pressed = keyboard_check_pressed(entry.vk);
        } catch (err) {
            entry.dead = true;
            mmapi_log_warn(entry.mod_name,
                "mmapi hotkey " + mmapi_hotkey_name_from_vk(entry.vk) + " from "
                + entry.mod_name + " disabled: the engine rejected its KeyCode: " + string(err));
            continue;
        }
        if (pressed) {
            try {
                entry.callback();
            } catch (err) {
                mmapi_warn_rate_limited(
                    "hotkey:" + string(entry.vk) + ":" + entry.mod_name,
                    entry.mod_name,
                    "mmapi hotkey " + mmapi_hotkey_name_from_vk(entry.vk) + " from "
                    + entry.mod_name + " failed: " + string(err));
            }
        }
    }

    // Gamepad entries: the engine's own idiom (Input.begin_frame) - scan device
    // slots up to GAMEPADS_COUNT, skip disconnected, edge-check the button. The
    // slot cap is a live-engine constant; where it is unbound (a headless VM)
    // fall back to 12, the conventional slot count. Skipped entirely while no
    // pad entry exists, so keyboard-only environments never touch a gamepad builtin.
    var pad_hotkeys = global[$ "__mmapi_pad_hotkeys"];
    if (pad_hotkeys == undefined) { return; }
    var pad_count = array_length(pad_hotkeys);
    if (pad_count == 0) { return; }
    var device_cap = undefined;
    try { device_cap = GAMEPADS_COUNT; } catch (__mmapi_pad_cap) {}
    if (device_cap == undefined) { device_cap = 12; }
    for (var p = 0; p < pad_count; p++) {
        var pad_entry = pad_hotkeys[p];
        if (pad_entry[$ "dead"] == true) { continue; }
        // Same belt-and-suspenders as the keyboard loop: a throw (missing gamepad
        // builtins, or a code the engine refuses) disables the ENTRY, never the
        // poll. A disconnected pad is no error - the entry stays live and waits.
        var pad_pressed = false;
        try {
            for (var d = 0; d < device_cap; d++) {
                if (gamepad_is_connected(d) == false) { continue; }
                if (gamepad_button_check_pressed(d, pad_entry.button)) {
                    pad_pressed = true;
                    break;
                }
            }
        } catch (pad_err) {
            pad_entry.dead = true;
            mmapi_log_warn(pad_entry.mod_name,
                "mmapi hotkey " + mmapi_hotkey_name_from_pad(pad_entry.button) + " from "
                + pad_entry.mod_name + " disabled: the engine rejected its pad poll: " + string(pad_err));
            continue;
        }
        if (pad_pressed) {
            try {
                pad_entry.callback();
            } catch (pad_err) {
                mmapi_warn_rate_limited(
                    "hotkey_pad:" + string(pad_entry.button) + ":" + pad_entry.mod_name,
                    pad_entry.mod_name,
                    "mmapi hotkey " + mmapi_hotkey_name_from_pad(pad_entry.button) + " from "
                    + pad_entry.mod_name + " failed: " + string(pad_err));
            }
        }
    }
}

__mmapi_register_as(mmapi_hotkeys_poll, "mmapi");
