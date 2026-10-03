// MMAPI - A GML modding framework for Fields of Mistria
// Copyright (C) 2026 Anna Nomoly
//
// This file is part of MMAPI, distributed with the Mods of Mistria Installer.
// Licensed under the GNU General Public License v3.0 or later, WITH
// ADDITIONAL TERMS under GPLv3 section 7 (attribution preservation, no
// misrepresentation of origin, no trademark grant).
//
// See the LICENSE file in this directory for those additional terms.
// See LICENCE.txt at the repository root for the full GPL text.
//
// SPDX-License-Identifier: GPL-3.0-or-later

// mmapi_hotkeys.gml. The hotkey registry: mods register a keyboard vk, a
// gamepad button, or a compound binding (a chord like SHIFT+F5) → callback,
// and the module polls once per frame through its own lifecycle install -
// press edges for triggers, level checks for a chord's held parts. A matched
// chord consumes its trigger for that frame, so bare registrations on the same
// code stay quiet. A binding registered by more than one mod logs a conflict
// Warn and both stay registered, so a collision never silently drops one.

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

// "SHIFT+F5" → ["SHIFT", "F5"]. Empty tokens ("+F5", "F5+", "A++B") make the
// whole name invalid, so a typo takes the designed invalid-name path.
function __mmapi_hotkey_split_name(name) {
    var tokens = [];
    var current = "";
    var len = string_length(name);
    for (var i = 1; i <= len; i++) {
        var c = string_char_at(name, i);
        if (c == "+") {
            if (current == "") { return undefined; }
            array_push(tokens, current);
            current = "";
        } else {
            current += c;
        }
    }
    if (current == "") { return undefined; }
    array_push(tokens, current);
    return tokens;
}

// Compound-binding resolver: a "+"-joined name over the two device vocabularies
// ("SHIFT+F5", "GAMEPAD_LEFT_SHOULDER+GAMEPAD_A", mixed devices allowed).
// Returns { parts: [{ device: "kb"|"pad", code }] } with the LAST part as the
// trigger, or undefined when any token fails both resolvers. A plain single
// name resolves to a one-part binding, so this can validate ANY hotkey config
// value in one call.
function mmapi_hotkey_binding_from_name(name) {
    if (!is_string(name)) { return undefined; }
    var tokens = __mmapi_hotkey_split_name(name);
    if (tokens == undefined) { return undefined; }
    var parts = [];
    for (var i = 0; i < array_length(tokens); i++) {
        var vk = mmapi_hotkey_vk_from_name(tokens[i]);
        if (vk != undefined) {
            array_push(parts, { device: "kb", code: vk });
            continue;
        }
        var pad = mmapi_hotkey_pad_from_name(tokens[i]);
        if (pad != undefined) {
            array_push(parts, { device: "pad", code: pad });
            continue;
        }
        return undefined;
    }
    return { parts: parts };
}

// Reverse of mmapi_hotkey_binding_from_name, for diagnostics ("SHIFT+F5" in a
// warn, never a struct dump). Guarded like the other reverse lookups: a
// malformed binding falls back to "binding ?" rather than throwing.
function mmapi_hotkey_name_from_binding(binding) {
    var out = "";
    try {
        var parts = binding.parts;
        var part_count = array_length(parts);
        if (part_count == 0) { return "binding ?"; }
        for (var i = 0; i < part_count; i++) {
            var part = parts[i];
            var piece = (part.device == "pad")
                ? mmapi_hotkey_name_from_pad(part.code)
                : mmapi_hotkey_name_from_vk(part.code);
            out += (i == 0 ? "" : "+") + piece;
        }
    } catch (__mmapi_binding_name_err) {
        return "binding ?";
    }
    return out;
}

// Chord match test for the poll: the LAST part is the trigger (edge-checked),
// every other part must be held (level-checked). Pad parts must all read from
// ONE connected device; keyboard parts are global. Throws propagate - the poll
// dead-marks the entry.
function __mmapi_hotkey_chord_matched(parts, device_cap) {
    var part_count = array_length(parts);
    var trigger_index = part_count - 1;

    // Keyboard phase: every kb held part down; a kb trigger on its press edge.
    for (var i = 0; i < part_count; i++) {
        var part = parts[i];
        if (part.device != "kb") { continue; }
        if (i == trigger_index) {
            if (!keyboard_check_pressed(part.code)) { return false; }
        } else {
            if (!keyboard_check(part.code)) { return false; }
        }
    }

    // Pad phase: all pad parts (helds, plus a pad trigger's edge) on ONE device.
    var has_pad = false;
    for (var i = 0; i < part_count; i++) {
        if (parts[i].device == "pad") { has_pad = true; break; }
    }
    if (!has_pad) { return true; }
    for (var d = 0; d < device_cap; d++) {
        if (gamepad_is_connected(d) == false) { continue; }
        var all_on_device = true;
        for (var i = 0; i < part_count; i++) {
            var part = parts[i];
            if (part.device != "pad") { continue; }
            if (i == trigger_index) {
                if (!gamepad_button_check_pressed(d, part.code)) { all_on_device = false; break; }
            } else {
                if (!gamepad_button_check(d, part.code)) { all_on_device = false; break; }
            }
        }
        if (all_on_device) { return true; }
    }
    return false;
}

// Level twin for held-pattern mods (a mod polls this itself, on its own
// schedule, the way bulk-buy style held gates already poll one code): are ALL
// of the binding's parts down right now? No edge anywhere, no registration, no
// suppression involvement. Never throws - a missing input builtin (a headless
// VM without the stubs) reads as "not held".
function mmapi_hotkey_binding_held(binding) {
    var held = false;
    try {
        var parts = binding.parts;
        var part_count = array_length(parts);
        if (part_count == 0) { return false; }
        for (var i = 0; i < part_count; i++) {
            var part = parts[i];
            if (part.device == "kb" && !keyboard_check(part.code)) { return false; }
        }
        var has_pad = false;
        for (var i = 0; i < part_count; i++) {
            if (parts[i].device == "pad") { has_pad = true; break; }
        }
        if (!has_pad) { return true; }
        var device_cap = undefined;
        try { device_cap = GAMEPADS_COUNT; } catch (__mmapi_bh_cap) {}
        if (device_cap == undefined) { device_cap = 12; }
        for (var d = 0; d < device_cap; d++) {
            if (gamepad_is_connected(d) == false) { continue; }
            var all_on_device = true;
            for (var i = 0; i < part_count; i++) {
                var part = parts[i];
                if (part.device != "pad") { continue; }
                if (!gamepad_button_check(d, part.code)) { all_on_device = false; break; }
            }
            if (all_on_device) { held = true; break; }
        }
    } catch (__mmapi_binding_held_err) {
        return false;
    }
    return held;
}

// Compound (chord) registration: binding comes from mmapi_hotkey_binding_from_name.
// The LAST part is the trigger; the rest must be held when its press lands. A
// matched chord CONSUMES its trigger for that frame - bare registrations on the
// same code stay quiet. That is a guarantee, not an option: SHIFT+F5 is not F5.
// Chord-vs-chord conflicts warn and both fire, like the single registries. A
// multi-part chord whose trigger overlaps an existing bare registration warns
// an advisory, because that bare bind goes quiet whenever the chord matches.
// No register-time engine probe, for the register_pad reason: parts may be pad
// codes whose validity is only observable against a connected device. The
// poll's per-entry dead-marking is the backstop.
function mmapi_hotkey_register_binding(binding, callback, opts) {
    if (global[$ "__mmapi_binding_hotkeys"] == undefined) { global.__mmapi_binding_hotkeys = []; }
    var hotkeys = global.__mmapi_binding_hotkeys;

    var mod_name = mmapi_current_mod();
    if (opts != undefined && opts[$ "mod_name"] != undefined) { mod_name = opts.mod_name; }

    var parts_ok = false;
    if (is_struct(binding)) {
        try { parts_ok = array_length(binding.parts) > 0; } catch (__mmapi_binding_shape) {}
    }
    if (!parts_ok) {
        mmapi_log_warn(mod_name,
            "mmapi hotkey binding from " + mod_name + " rejected: not a binding "
            + "(resolve the config name through mmapi_hotkey_binding_from_name first)");
        return;
    }

    var name = mmapi_hotkey_name_from_binding(binding);
    for (var i = 0; i < array_length(hotkeys); i++) {
        if (mmapi_hotkey_name_from_binding(hotkeys[i].binding) == name) {
            mmapi_log_warn(mod_name,
                "mmapi hotkey conflict: " + name + " is registered by "
                + hotkeys[i].mod_name + " and now also by " + mod_name
                + ". Both will fire");
        }
    }

    // The subset advisory: this chord's trigger over an existing bare bind.
    var parts = binding.parts;
    if (array_length(parts) > 1) {
        var trigger = parts[array_length(parts) - 1];
        if (trigger.device == "kb") {
            var singles = global[$ "__mmapi_hotkeys"];
            if (singles != undefined) {
                for (var i = 0; i < array_length(singles); i++) {
                    if (singles[i].vk == trigger.code) {
                        __mmapi_hotkey_overlap_warn(mod_name,
                            mmapi_hotkey_name_from_vk(trigger.code), singles[i].mod_name,
                            name, mod_name);
                    }
                }
            }
        } else {
            var pad_singles = global[$ "__mmapi_pad_hotkeys"];
            if (pad_singles != undefined) {
                for (var i = 0; i < array_length(pad_singles); i++) {
                    if (pad_singles[i].button == trigger.code) {
                        __mmapi_hotkey_overlap_warn(mod_name,
                            mmapi_hotkey_name_from_pad(trigger.code), pad_singles[i].mod_name,
                            name, mod_name);
                    }
                }
            }
        }
    }

    array_push(hotkeys, { binding: binding, callback: callback, mod_name: mod_name });
}

// The overlap advisory, one symmetric shape for both registration directions.
// Deliberately fault-neutral: both bindings are named identically, the routing
// rule is stated as a property of the pair, and the closing sentence says the
// bare binding is otherwise untouched. warn_to is the registrant whose log
// carries the line.
function __mmapi_hotkey_overlap_warn(warn_to, bare_name, bare_mod, chord_name, chord_mod) {
    mmapi_log_warn(warn_to,
        "mmapi hotkey overlap: " + bare_name + " (" + bare_mod + ") and "
        + chord_name + " (" + chord_mod + ") share a trigger. The compound keybind "
        + "takes precedence. " + bare_name + " by itself works as normal");
}

// The overlap advisory's other direction: a bare registration landing on a code
// that is already some chord's trigger. Shared by register and register_pad.
function __mmapi_hotkey_warn_bare_overlap(device, code, mod_name) {
    var chords = global[$ "__mmapi_binding_hotkeys"];
    if (chords == undefined) { return; }
    for (var i = 0; i < array_length(chords); i++) {
        var chord_parts = undefined;
        try { chord_parts = chords[i].binding.parts; } catch (__mmapi_overlap_shape) { continue; }
        if (array_length(chord_parts) < 2) { continue; }
        var trigger = chord_parts[array_length(chord_parts) - 1];
        if (trigger.device != device || trigger.code != code) { continue; }
        var bare_name = (device == "pad")
            ? mmapi_hotkey_name_from_pad(code)
            : mmapi_hotkey_name_from_vk(code);
        __mmapi_hotkey_overlap_warn(mod_name,
            bare_name, mod_name,
            mmapi_hotkey_name_from_binding(chords[i].binding), chords[i].mod_name);
    }
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

    __mmapi_hotkey_warn_bare_overlap("kb", vk, mod_name);
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

    __mmapi_hotkey_warn_bare_overlap("pad", button, mod_name);
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
    // Compound entries first: a matched chord fires AND consumes its trigger, so
    // the single loops below skip that code this frame. SHIFT+F5 is not F5.
    // Single-part bindings registered through register_binding consume nothing -
    // they ARE the bare bind.
    var consumed_vk = [];
    var consumed_pad = [];
    var chord_hotkeys = global[$ "__mmapi_binding_hotkeys"];
    if (chord_hotkeys != undefined && array_length(chord_hotkeys) > 0) {
        var chord_device_cap = undefined;
        try { chord_device_cap = GAMEPADS_COUNT; } catch (__mmapi_chord_cap) {}
        if (chord_device_cap == undefined) { chord_device_cap = 12; }
        for (var b = 0; b < array_length(chord_hotkeys); b++) {
            var chord_entry = chord_hotkeys[b];
            if (chord_entry[$ "dead"] == true) { continue; }
            // Same per-entry dead-marking as the single loops: a throw disables
            // the ENTRY, never the poll.
            var chord_matched = false;
            try {
                chord_matched = __mmapi_hotkey_chord_matched(chord_entry.binding.parts, chord_device_cap);
            } catch (chord_err) {
                chord_entry.dead = true;
                mmapi_log_warn(chord_entry.mod_name,
                    "mmapi hotkey " + mmapi_hotkey_name_from_binding(chord_entry.binding) + " from "
                    + chord_entry.mod_name + " disabled: the engine rejected its poll: " + string(chord_err));
                continue;
            }
            if (chord_matched) {
                var chord_parts = chord_entry.binding.parts;
                if (array_length(chord_parts) > 1) {
                    var chord_trigger = chord_parts[array_length(chord_parts) - 1];
                    if (chord_trigger.device == "kb") {
                        array_push(consumed_vk, chord_trigger.code);
                    } else {
                        array_push(consumed_pad, chord_trigger.code);
                    }
                }
                try {
                    chord_entry.callback();
                } catch (chord_err) {
                    mmapi_warn_rate_limited(
                        "hotkey_binding:" + mmapi_hotkey_name_from_binding(chord_entry.binding) + ":" + chord_entry.mod_name,
                        chord_entry.mod_name,
                        "mmapi hotkey " + mmapi_hotkey_name_from_binding(chord_entry.binding) + " from "
                        + chord_entry.mod_name + " failed: " + string(chord_err));
                }
            }
        }
    }

    // No early return on an empty keyboard registry: pad and chord registrants
    // must poll regardless of whether any keyboard single exists.
    var hotkeys = global[$ "__mmapi_hotkeys"];
    var count = (hotkeys == undefined) ? 0 : array_length(hotkeys);
    for (var i = 0; i < count; i++) {
        var entry = hotkeys[i];
        if (entry[$ "dead"] == true) { continue; }
        // A chord consumed this code this frame: the bare bind stays quiet.
        var suppressed = false;
        for (var s = 0; s < array_length(consumed_vk); s++) {
            if (consumed_vk[s] == entry.vk) { suppressed = true; break; }
        }
        if (suppressed) { continue; }
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
        // A chord consumed this button this frame: the bare bind stays quiet.
        var pad_suppressed = false;
        for (var s = 0; s < array_length(consumed_pad); s++) {
            if (consumed_pad[s] == pad_entry.button) { pad_suppressed = true; break; }
        }
        if (pad_suppressed) { continue; }
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
