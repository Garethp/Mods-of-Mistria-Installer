// Hook liveness: the dispatchers tally per-hook dispatch counts, but only
// while the debug agent is enabled. The tally counts the dispatch site, not
// the handlers, so a hook with nothing registered still counts.

function lv_event(ctx) { global.lv_ran += 1; }

global.lv_ran = 0;

// Debug never enabled: dispatching counts nothing and creates no debug state.
mmapi_emit("lv.pre", undefined);
deq("no debug state means no tally", mmapi_hook_fired_count("lv.pre"), 0);
dcheck("dispatch does not create the debug state", global[$ "__mmapi_debug"] == undefined);
var report = mmapi_hook_liveness();
deq("the report reads disabled before debug ever ran", report.enabled, false);

mmapi_debug_set_enabled(true);

// The core property: a zero-handler dispatch still counts, because the tally
// proves the dispatch site ran, not that anyone listened.
mmapi_emit("lv.silent_site", undefined);
deq("a zero-handler dispatch counts", mmapi_hook_fired_count("lv.silent_site"), 1);

// All four dispatchers tally, once per dispatch.
mmapi_on("lv.event", lv_event);
mmapi_emit("lv.event", undefined);
mmapi_emit("lv.event", undefined);
deq("emit tallies once per dispatch", mmapi_hook_fired_count("lv.event"), 2);
deq("  and the handlers still ran", global.lv_ran, 2);

mmapi_apply_filters("lv.filter", 5, undefined);
deq("apply_filters tallies", mmapi_hook_fired_count("lv.filter"), 1);
mmapi_check_guards("lv.guard", undefined);
deq("check_guards tallies", mmapi_hook_fired_count("lv.guard"), 1);
mmapi_run_override("lv.override", undefined);
deq("run_override tallies", mmapi_hook_fired_count("lv.override"), 1);

// Disabling stops the tally without clearing what was gathered.
mmapi_debug_set_enabled(false);
mmapi_emit("lv.event", undefined);
deq("a disabled agent stops counting", mmapi_hook_fired_count("lv.event"), 2);
mmapi_debug_set_enabled(true);

// With no installed catalog the report stays honest: counts are real, and
// silent is empty rather than a guess.
report = mmapi_hook_liveness();
deq("the report reads enabled", report.enabled, true);
deq("with no catalog the report claims no silent hooks", array_length(report.silent), 0);

// Against an installed catalog: declared hooks that never dispatched list as
// silent, and counted names outside the catalog list as undeclared, custom
// mod-emitted hooks included.
global.__mmapi_hook_catalog = {};
global.__mmapi_hook_catalog[$ "lv.event"] = "event";
global.__mmapi_hook_catalog[$ "lv.declared_quiet"] = "filter";
report = mmapi_hook_liveness();
var silent = report.silent;
deq("one declared hook is silent", array_length(silent), 1);
deq("  and it is the undispatched one", silent[0], "lv.declared_quiet");
var undeclared = report.undeclared;
deq("counted names outside the catalog list as undeclared", array_length(undeclared), 4);
deq("  sorted, first", undeclared[0], "lv.filter");
deq("  sorted, last", undeclared[3], "lv.silent_site");
var counts = report.counts;
deq("counts ride in the report", counts[$ "lv.event"], 2);
