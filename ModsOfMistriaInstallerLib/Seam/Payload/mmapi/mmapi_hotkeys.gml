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

        case "NUMPAD_0": return vk_numpad0; case "NUMPAD_1": return vk_numpad1;
        case "NUMPAD_2": return vk_numpad2; case "NUMPAD_3": return vk_numpad3;
        case "NUMPAD_4": return vk_numpad4; case "NUMPAD_5": return vk_numpad5;
        case "NUMPAD_6": return vk_numpad6; case "NUMPAD_7": return vk_numpad7;
        case "NUMPAD_8": return vk_numpad8; case "NUMPAD_9": return vk_numpad9;

        case "INSERT":      return vk_insert;
        case "DELETE":      return vk_delete;
        case "HOME":        return vk_home;
        case "PAGE_UP":     return vk_pageup;
        case "PAGE_DOWN":   return vk_pagedown;
        case "SHIFT":       return vk_shift;
        case "ALT":         return vk_alt;
        case "CONTROL":     return vk_control;
        case "PAUSE_BREAK": return vk_pause;
        // No GML vk_ constants exist for these, so use the raw Windows codes.
        case "CAPS_LOCK":   return 20;
        case "NUM_LOCK":    return 144;
        case "SCROLL_LOCK": return 145;
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
    var names = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "NUMPAD_0", "NUMPAD_1", "NUMPAD_2", "NUMPAD_3", "NUMPAD_4",
        "NUMPAD_5", "NUMPAD_6", "NUMPAD_7", "NUMPAD_8", "NUMPAD_9",
        "INSERT", "DELETE", "HOME", "PAGE_UP", "PAGE_DOWN", "SHIFT", "ALT",
        "CONTROL", "PAUSE_BREAK", "CAPS_LOCK", "NUM_LOCK", "SCROLL_LOCK",
    ];
    for (var i = 0; i < array_length(names); i++) {
        if (mmapi_hotkey_vk_from_name(names[i]) == vk) { return names[i]; }
    }
    return "vk " + string(vk);
}

function mmapi_hotkey_register(vk, callback, opts) {
    if (global[$ "__mmapi_hotkeys"] == undefined) { global.__mmapi_hotkeys = []; }
    var hotkeys = global.__mmapi_hotkeys;

    var mod_name = mmapi_current_mod();
    if (opts != undefined && opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }

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

function mmapi_hotkeys_poll() {
    var hotkeys = global[$ "__mmapi_hotkeys"];
    if (hotkeys == undefined) { return; }
    var count = array_length(hotkeys);
    for (var i = 0; i < count; i++) {
        var entry = hotkeys[i];
        if (keyboard_check_pressed(entry.vk)) {
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
