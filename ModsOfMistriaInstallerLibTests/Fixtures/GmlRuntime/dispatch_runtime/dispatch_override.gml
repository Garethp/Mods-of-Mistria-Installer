// Override dispatch: the first non-undefined result wins and stops the chain,
// and undefined passes to the next handler.
//
// The false and 0 cases matter for the same reason as in dispatch_filter: an
// override answering `false` has answered. A dispatcher testing truthiness, or
// `== undefined` loosely, would treat that as a pass and let the next mod or
// the vanilla path answer instead. That is a silent wrong answer, not a crash.

function o_a(ctx)     { global.o_log += "a"; return "A"; }
function o_b(ctx)     { global.o_log += "b"; return "B"; }
function o_pass(ctx)  { global.o_log += "p"; return undefined; }
function o_quiet(ctx) { global.o_log += "q"; }            // implicit undefined
function o_false(ctx) { return false; }
function o_zero(ctx)  { return 0; }

global.o_log = "";

dcheck("no override handlers yields undefined",
       typeof(mmapi_run_override("o.none", undefined)) == "undefined");

mmapi_override("o.one", o_a, { mod_name: "m1" });
deq("an override answers", mmapi_run_override("o.one", undefined), "A");

// First non-undefined wins, and the loser must not run at all. Overrides can
// have side effects, so winning has to mean the second one never happened.
global.o_log = "";
mmapi_override("o.first", o_a, { mod_name: "m1" });
mmapi_override("o.first", o_b, { mod_name: "m2" });
deq("the first non-undefined result wins", mmapi_run_override("o.first", undefined), "A");
deq("  and the losing override never ran", global.o_log, "a");

// undefined passes through to the next handler.
global.o_log = "";
mmapi_override("o.pass", o_pass, { mod_name: "m1" });
mmapi_override("o.pass", o_b, { mod_name: "m2" });
deq("an undefined result passes to the next override",
    mmapi_run_override("o.pass", undefined), "B");
deq("  and both ran", global.o_log, "pb");

// Returning nothing at all is also a pass: the claim-scoped shape, where each
// handler claims its own targets and declines the rest.
global.o_log = "";
mmapi_override("o.quiet", o_quiet, { mod_name: "m1" });
mmapi_override("o.quiet", o_b, { mod_name: "m2" });
deq("a handler returning nothing passes to the next",
    mmapi_run_override("o.quiet", undefined), "B");
deq("  and both ran", global.o_log, "qb");

// Every handler declining yields undefined, so the caller falls back to vanilla.
global.o_log = "";
mmapi_override("o.allpass", o_pass, { mod_name: "m1" });
mmapi_override("o.allpass", o_quiet, { mod_name: "m2" });
dcheck("all handlers declining yields undefined (vanilla wins)",
       typeof(mmapi_run_override("o.allpass", undefined)) == "undefined");
deq("  and every handler was consulted", global.o_log, "pq");

// false and 0 are answers, not passes.
mmapi_override("o.false", o_false, { mod_name: "m1" });
deq("false is a real override answer, not a pass",
    mmapi_run_override("o.false", undefined), false);

mmapi_override("o.zero", o_zero, { mod_name: "m1" });
deq("0 is a real override answer, not a pass",
    mmapi_run_override("o.zero", undefined), 0);

// A false answer must stop the chain, not merely be returned.
global.o_log = "";
mmapi_override("o.falsestops", o_false, { mod_name: "m1" });
mmapi_override("o.falsestops", o_b, { mod_name: "m2" });
deq("a false answer wins", mmapi_run_override("o.falsestops", undefined), false);
deq("  and stopped the chain", global.o_log, "");

// Kind isolation.
function o_event_impostor(ctx) { global.o_impostor = true; }
global.o_impostor = false;
mmapi_override("o.mixed", o_a, { mod_name: "m1" });
mmapi_on("o.mixed", o_event_impostor);
global.o_log = "";
deq("run_override dispatches only override-kind records",
    mmapi_run_override("o.mixed", undefined), "A");
dcheck("  and left the event record alone", global.o_impostor == false);
