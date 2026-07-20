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
