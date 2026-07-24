// Error isolation: the framework's central promise, and the one thing every mod
// implicitly relies on. A handler throwing does not crash the game and does not
// take out the other mods' handlers.

function i_boom(ctx)          { throw "event handler exploded"; }
function i_boom_filter(v, ctx) { throw "filter exploded"; }
function i_boom_guard(ctx)    { throw "guard exploded"; }
function i_boom_override(ctx) { throw "override exploded"; }

function i_mark(ctx)          { global.i_log += "m"; }
function i_double(v, ctx)     { return v * 2; }
function i_answer(ctx)        { return "answered"; }

global.i_log = "";

// ── One throwing handler per kind, each with an innocent handler behind it ──

mmapi_on("i.ev", i_boom, { mod_name: "bad_mod" });
mmapi_on("i.ev", i_mark, { mod_name: "good_mod" });
mmapi_emit("i.ev", undefined);
dcheck("a throwing event handler did not propagate to the caller", true);
deq("  and the chain continued past it", global.i_log, "m");

mmapi_filter("i.filt", i_boom_filter, { mod_name: "bad_mod" });
mmapi_filter("i.filt", i_double, { mod_name: "good_mod" });
deq("a throwing filter keeps the current value and the chain continues",
    mmapi_apply_filters("i.filt", 5, undefined), 10);

mmapi_guard("i.guard", i_boom_guard, { mod_name: "bad_mod" });
deq("a throwing guard counts as allow (guards fail open)",
    mmapi_check_guards("i.guard", undefined), true);

mmapi_override("i.over", i_boom_override, { mod_name: "bad_mod" });
mmapi_override("i.over", i_answer, { mod_name: "good_mod" });
deq("a throwing override is skipped and the next one still answers",
    mmapi_run_override("i.over", undefined), "answered");

// ── The unguarded seam shapes, reproduced verbatim ──────────────────────────
//
// Two layers of protection exist at a seam, guarding different code. The
// dispatcher wraps every HANDLER call itself, so a throwing mod callback is
// always contained, at every seam. The seam's own try/catch (the try_catch
// template option) guards the SEAM's code instead: a ctx expression that
// reads live engine state, or a write-back after the dispatch. A few seams
// skip that site catch because their dispatch line is provably total - a
// literal ctx like { player: self } and a bare assignment of the return have
// nothing left to fail. The worry this section retires is that those bare
// seams are unprotected against throwing callbacks: they are not, because
// callback isolation never came from the site catch. Both bare shapes below
// reproduce real catalog seams and survive a thrower.

// player_move_speed's seam body, in shape: no try/catch anywhere.
function i_move_speed_seam() {
    var spd = 10;
    spd = mmapi_apply_filters("player.move_speed", spd, { player: 1, on_mount: false });
    return spd;   // unreachable if the throw propagated
}
mmapi_filter("player.move_speed", i_boom_filter, { mod_name: "bad_mod" });
deq("an UNGUARDED filter seam survives a throwing handler, keeping its value",
    i_move_speed_seam(), 10);

// interact_elevator_action's seam body: mmapi_check_guards, no try/catch.
function i_elevator_seam() {
    if (mmapi_check_guards("interact.elevator_action", { who: 1 })) { return "allowed"; }
    return "vetoed";
}
mmapi_guard("interact.elevator_action", i_boom_guard, { mod_name: "bad_mod" });
deq("an UNGUARDED guard seam survives a throwing handler, failing open",
    i_elevator_seam(), "allowed");

// ── The failures were counted, and charged to the right mod ─────────────────
//
// Isolation that loses the error is silence, not safety. Six throws above, all
// from bad_mod.
var stats = mmapi_hook_stats();
deq("every failure was attributed to the mod that threw", stats.errors[$ "bad_mod"], 6);
dcheck("and the innocent mod was charged nothing",
       stats.errors[$ "good_mod"] == undefined);
