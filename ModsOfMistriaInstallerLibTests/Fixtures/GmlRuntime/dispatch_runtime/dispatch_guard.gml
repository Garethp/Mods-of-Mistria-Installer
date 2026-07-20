// Guard dispatch: false vetoes, everything else allows, and the first veto
// short-circuits.
//
// The "returns nothing allows" case is quirk 2's exact target. mmapi_check_guards
// tests `== undefined` before `== false`, and the note used to say that ordering
// was load-bearing because the VM conflated the two. It does not, so the
// ordering is cheap defensiveness now. The behaviour it protects is real
// protocol, and that is what this file pins: if a guard that declines ever
// starts reading as a veto, the game silently stops letting players do things.

function g_allow(ctx)   { return true; }
function g_veto(ctx)    { return false; }
function g_undef(ctx)   { return undefined; }
function g_nothing(ctx) { }              // implicit undefined
function g_number(ctx)  { return 1; }     // not false → allow
function g_zero(ctx)    { return 0; }     // int64 zero → allows (see below)
function g_fzero(ctx)   { return 0.0; }   // float zero → allows (see below)
function g_empty(ctx)   { return ""; }    // allows

function g_a(ctx) { global.g_log += "a"; return true; }
function g_b(ctx) { global.g_log += "b"; return false; }
function g_c(ctx) { global.g_log += "c"; return true; }

deq("no guards allows", mmapi_check_guards("g.none", undefined), true);

mmapi_guard("g.allow", g_allow);
deq("a guard returning true allows", mmapi_check_guards("g.allow", undefined), true);

mmapi_guard("g.veto", g_veto);
deq("a guard returning false vetoes", mmapi_check_guards("g.veto", undefined), false);

mmapi_guard("g.undef", g_undef);
deq("a guard returning undefined allows", mmapi_check_guards("g.undef", undefined), true);

mmapi_guard("g.nothing", g_nothing);
deq("a guard that returns nothing at all allows (declining is not vetoing)",
    mmapi_check_guards("g.nothing", undefined), true);

mmapi_guard("g.number", g_number);
deq("a non-zero number allows", mmapi_check_guards("g.number", undefined), true);

// A zero does not veto, and these two cases are the ones with history. The veto
// test used to be a bare `result == false`, which this dialect's numeric
// coercion also makes true for 0 (typeof int64) and 0.0 (typeof number): the
// types differ from bool, and the comparison did not care. A guard returning a
// stray zero silently blocked the player, which is what this hook kind's
// fail-open posture exists to prevent.
//
// These two assertions failed when this file was first written, against the
// documented behaviour, and produced the fix. Both zero widths are pinned:
// they are separate types, and an exclusion test has to name both.
mmapi_guard("g.zero", g_zero);
deq("an int64 zero allows - only a real boolean false vetoes",
    mmapi_check_guards("g.zero", undefined), true);

mmapi_guard("g.fzero", g_fzero);
deq("a float zero allows too (typeof 0.0 is `number`, not `int64`)",
    mmapi_check_guards("g.fzero", undefined), true);

// A string never vetoed even before the fix: `"" == false` is false, so the
// coercion was only ever numeric. Pinned so a future `==` change is loud.
mmapi_guard("g.empty", g_empty);
deq("an empty string allows", mmapi_check_guards("g.empty", undefined), true);

// The counterweight, which must never regress: a real false still vetoes. The
// fix narrowed the veto test, and a narrowing one step too far would silently
// disarm every guard in every mod. g.veto above covers the simple case; this
// covers it behind an allowing guard, where a broken test would be quietest.
function g_true_then_false_a(ctx) { return true; }
mmapi_guard("g.stillvetoes", g_true_then_false_a);
mmapi_guard("g.stillvetoes", g_veto);
deq("a real false STILL vetoes after the narrowing",
    mmapi_check_guards("g.stillvetoes", undefined), false);

// Short-circuit: b vetoes, so c must never run.
global.g_log = "";
mmapi_guard("g.short", g_a);
mmapi_guard("g.short", g_b);
mmapi_guard("g.short", g_c);
deq("the check vetoes", mmapi_check_guards("g.short", undefined), false);
deq("  and short-circuited at the first veto (c never ran)", global.g_log, "ab");

// A veto anywhere wins, even behind an allowing guard.
global.g_log = "";
mmapi_guard("g.late", g_a);
mmapi_guard("g.late", g_c);
mmapi_guard("g.late", g_b);
deq("a late veto still vetoes", mmapi_check_guards("g.late", undefined), false);
deq("  and every guard before it ran", global.g_log, "acb");

// Kind isolation: only guard records dispatch here.
function g_event_impostor(ctx) { global.g_impostor = true; }
global.g_impostor = false;
mmapi_guard("g.mixed", g_veto);
mmapi_on("g.mixed", g_event_impostor);
deq("check_guards dispatches only guard-kind records",
    mmapi_check_guards("g.mixed", undefined), false);
dcheck("  and left the event record alone", global.g_impostor == false);
