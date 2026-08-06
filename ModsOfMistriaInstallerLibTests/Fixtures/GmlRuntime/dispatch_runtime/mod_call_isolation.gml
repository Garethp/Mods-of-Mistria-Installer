// Every place mmapi calls into mod-supplied code must isolate a failure.
//
// dispatch_isolation.gml covers the four hook dispatchers. mmapi calls mod code
// from two more places:
//
//   mmapi_run_installs()  the installer loop, driven by the Game.gml seam every
//                         frame. One mod's throwing installer must not stop the
//                         mods after it in the list from ever registering.
//   mmapi_hotkeys_poll()  per-frame key polling, the same shape.
//
// Both took the __mmapi_guarded_call trampoline, so both are gates for removing
// it.

// keyboard_check_pressed is a game-only name. Stub it as a settable global so a
// test can "press" a key. It is the only engine name this body needs.
global.__test_pressed_vk = -1;
function keyboard_check_pressed(vk) { return vk == global.__test_pressed_vk; }

global.mci_log = "";

function mci_install_boom()  { throw "installer exploded"; }
function mci_install_a()     { global.mci_log += "a"; }
function mci_install_b()     { global.mci_log += "b"; }
function mci_hotkey_boom()   { throw "hotkey exploded"; }
function mci_hotkey_ok()     { global.mci_log += "k"; }

// ── Installs: a throwing installer is isolated, the rest still install ──────
// The thrower is registered first, so the ones behind it can only run if the
// throw was contained.
__mmapi_register_as(mci_install_boom, "bad_mod");
__mmapi_register_as(mci_install_a, "good_mod");
__mmapi_register_as(mci_install_b, "other_mod");

mmapi_run_installs();
dcheck("a throwing installer did not propagate out of mmapi_run_installs", true);
deq("  and every installer behind it still ran", global.mci_log, "ab");

// Installers re-run every frame, so the isolation has to hold on the next pass
// too: a thrower must not poison the loop permanently.
global.mci_log = "";
mmapi_run_installs();
deq("the next frame's install pass is unaffected", global.mci_log, "ab");

// ── Hotkeys: a throwing callback is isolated ────────────────────────────────
global.mci_log = "";
mmapi_hotkey_register(112, mci_hotkey_boom, { mod_name: "bad_mod" });   // F1
mmapi_hotkey_register(113, mci_hotkey_ok, { mod_name: "good_mod" });    // F2

global.__test_pressed_vk = 112;      // press the throwing one
mmapi_hotkeys_poll();
dcheck("a throwing hotkey callback did not propagate out of the poll", true);
deq("  and it did not run the other key's callback", global.mci_log, "");

global.__test_pressed_vk = 113;      // press the good one
mmapi_hotkeys_poll();
deq("the other hotkey still fires after the thrower", global.mci_log, "k");

// Nothing pressed: no callback runs, and the poll is a no-op.
global.mci_log = "";
global.__test_pressed_vk = -1;
mmapi_hotkeys_poll();
deq("no key pressed means no callback", global.mci_log, "");

// ── Gamepad hotkeys: same isolation contract on the pad leg of the poll ─────
// Stub the two gamepad names the pad leg needs, mirroring keyboard_check_pressed:
// one connected pad at slot 0, presses driven by a settable global. GAMEPADS_COUNT
// stays unbound here, so this also exercises the poll's slot-cap fallback.
global.__test_pressed_pad = -1;
function gamepad_is_connected(device) { return device == 0; }
function gamepad_button_check_pressed(device, button) {
    return device == 0 && button == global.__test_pressed_pad;
}

function mci_pad_boom() { throw "pad hotkey exploded"; }
function mci_pad_ok()   { global.mci_log += "p"; }

// Raw codes stand in for gp_* constants (live-engine bindings this VM lacks),
// exactly as 112/113 stand in for vk_f1/vk_f2 above.
mmapi_hotkey_register_pad(32769, mci_pad_boom, { mod_name: "bad_mod" });
mmapi_hotkey_register_pad(32770, mci_pad_ok, { mod_name: "good_mod" });

global.mci_log = "";
global.__test_pressed_pad = 32769;   // press the throwing one
mmapi_hotkeys_poll();
dcheck("a throwing pad callback did not propagate out of the poll", true);
deq("  and it did not run the other pad button's callback", global.mci_log, "");

global.__test_pressed_pad = 32770;   // press the good one
mmapi_hotkeys_poll();
deq("the other pad hotkey still fires after the thrower", global.mci_log, "p");

// Keyboard and pad registries are independent: a keyboard press must not fire
// pad callbacks, and vice versa.
global.mci_log = "";
global.__test_pressed_pad = -1;
global.__test_pressed_vk = 113;      // keyboard F2 only
mmapi_hotkeys_poll();
deq("a keyboard press fires only keyboard callbacks", global.mci_log, "k");

global.mci_log = "";
global.__test_pressed_vk = -1;
mmapi_hotkeys_poll();
deq("nothing pressed on either device means no callback", global.mci_log, "");

// ── Compound bindings: chord semantics + the suppression guarantee ──────────
// keyboard_check is the chord's LEVEL primitive; stub the down channel the way
// the press channel is stubbed above (the pad stubs already cover both).
global.__test_down_vk = -1;
function keyboard_check(vk) { return vk == global.__test_down_vk; }

function mci_chord_boom() { throw "chord exploded"; }
function mci_chord_ok()   { global.mci_log += "c"; }
function mci_bare_ok()    { global.mci_log += "b"; }

// Raw codes again: 16 stands in for vk_shift, 114 for vk_f3.
mmapi_hotkey_register(114, mci_bare_ok, { mod_name: "bare_mod" });
mmapi_hotkey_register_binding(
    { parts: [ { device: "kb", code: 16 }, { device: "kb", code: 114 } ] },
    mci_chord_ok, { mod_name: "chord_mod" });

// Trigger edge with the modifier UP: the chord does not match, the bare fires.
global.mci_log = "";
global.__test_pressed_vk = 114;
mmapi_hotkeys_poll();
deq("modifier up: bare fires, chord does not", global.mci_log, "b");

// Modifier DOWN + trigger edge: the chord fires and consumes - the bare stays quiet.
global.mci_log = "";
global.__test_down_vk = 16;
mmapi_hotkeys_poll();
deq("modifier down: chord fires and consumes the trigger", global.mci_log, "c");

// A throwing chord callback is isolated AND still consumes its trigger. The
// bare bind on 113 (mci_hotkey_ok, registered above) must stay quiet too.
mmapi_hotkey_register_binding(
    { parts: [ { device: "kb", code: 16 }, { device: "kb", code: 113 } ] },
    mci_chord_boom, { mod_name: "bad_mod" });
global.mci_log = "";
global.__test_pressed_vk = 113;
mmapi_hotkeys_poll();
dcheck("a throwing chord callback did not propagate out of the poll", true);
deq("  and it still consumed its trigger (the bare bind stayed quiet)", global.mci_log, "");

// Modifier released: the same press edge reaches the bare bind again.
global.__test_down_vk = -1;
global.mci_log = "";
mmapi_hotkeys_poll();
deq("modifier released: the bare bind fires again", global.mci_log, "k");
