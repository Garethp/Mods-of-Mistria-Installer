// mmapi_hotkeys.gml. The hotkey registry: mods register a vk → callback, and
// the module polls keyboard_check_pressed once per frame through its own
// lifecycle install. A vk registered by more than one mod logs a conflict Warn
// and both stay registered, so a collision never silently drops one.

// Button name → keyboard virtual-key code, undefined when the name is not a
// supported keyboard key. This is the vocabulary a mod's config validates
// against: F1-F12, NUMPAD_0-9, single digits 0-9, single letters A-Z, and the
// named specials. Gamepad names (GAMEPAD_*) return undefined: the poll reads
// keyboard_check_pressed only.
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
    // which the live runtime has but the tier-1 VM's stdlib does not).
    if (vk >= ord("0") && vk <= ord("9")) {
        return string_char_at("0123456789", vk - ord("0") + 1);
    }
    if (vk >= ord("A") && vk <= ord("Z")) {
        return string_char_at("ABCDEFGHIJKLMNOPQRSTUVWXYZ", vk - ord("A") + 1);
    }

    // Named keys: find the name whose forward lookup yields this vk. This only runs on
    // a conflict (or a failed callback), so a linear scan of the vocabulary is fine.
    // Each probe is guarded: the forward map reads bare vk_* constants, which the live
    // runtime defines and the tier-1 VM does not, and a diagnostics path must never
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

    var summary = "hotkey keycode capability: " + string(ok_count) + " name(s) supported"
        + (no_code == "" ? "" : "; no keycode (by design): " + no_code)
        + "; raw_accepted=" + raw_accepted;
    // A development diagnostic in the standard [PROBE] idiom: TRACE-gated, and the log
    // sink flushes [PROBE] lines immediately while the debug agent is on, so the line
    // is on disk right after boot in a --debug deploy with no forced flush of its own.
    // The WARN below is the user-facing signal.
    if (mmapi_log_get_level() <= MmapiLogLevel.Trace) {
        mmapi_log_trace("mmapi", "[PROBE] hotkeys|capability|supported=" + string(ok_count)
            + "|no_keycode=" + (no_code == "" ? "none" : no_code)
            + "|raw_accepted=" + raw_accepted);
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
}

__mmapi_register_as(mmapi_hotkeys_poll, "mmapi");
