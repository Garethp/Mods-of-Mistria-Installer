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

function mmapi_hotkey_register(vk, callback, opts) {
    if (global[$ "__mmapi_hotkeys"] == undefined) { global.__mmapi_hotkeys = []; }
    var hotkeys = global.__mmapi_hotkeys;

    var mod_name = mmapi_current_mod();
    if (opts != undefined && opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }

    for (var i = 0; i < array_length(hotkeys); i++) {
        if (hotkeys[i].vk == vk) {
            mmapi_log_warn(mod_name,
                "mmapi hotkey conflict: vk " + string(vk) + " is registered by "
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
                    "mmapi hotkey vk " + string(entry.vk) + " from "
                    + entry.mod_name + " failed: " + string(err));
            }
        }
    }
}

__mmapi_register_as(mmapi_hotkeys_poll, "mmapi");
