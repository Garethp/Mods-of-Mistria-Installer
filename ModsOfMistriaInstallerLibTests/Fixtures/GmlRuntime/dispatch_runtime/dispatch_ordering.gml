// Dispatch order: priority first, lower running earlier, stable by registration
// at equal priority, with before/after mod-name edges layered on top
// topologically. __mmapi_hook_resort does this on every registration, never at
// dispatch.
//
// Order is a contract, not an implementation detail. Whose filter runs first
// decides whose value wins, and these edges are how mods coordinate when they
// cannot coordinate any other way.

function ord_a(ctx) { global.ord_log += "a"; }
function ord_b(ctx) { global.ord_log += "b"; }
function ord_c(ctx) { global.ord_log += "c"; }
function ord_d(ctx) { global.ord_log += "d"; }

// Lower priority runs first, regardless of the order they registered in.
global.ord_log = "";
mmapi_on("ord.pri", ord_a, { priority: 10, mod_name: "ma" });
mmapi_on("ord.pri", ord_b, { priority: -5, mod_name: "mb" });
mmapi_on("ord.pri", ord_c, { priority: 0,  mod_name: "mc" });
mmapi_emit("ord.pri", undefined);
deq("priority orders dispatch, lower first", global.ord_log, "bca");

// The default priority is 0, so an opts-less registration sorts against
// explicit priorities rather than landing last.
global.ord_log = "";
mmapi_on("ord.def", ord_a, { priority: 1, mod_name: "ma" });
mmapi_on("ord.def", ord_b);
mmapi_emit("ord.def", undefined);
deq("the default priority is 0", global.ord_log, "ba");

// Equal priority is stable by registration sequence. Without this, order would
// be an accident of the sort and would shift as mods came and went.
global.ord_log = "";
mmapi_on("ord.stable", ord_a, { mod_name: "ma" });
mmapi_on("ord.stable", ord_b, { mod_name: "mb" });
mmapi_on("ord.stable", ord_c, { mod_name: "mc" });
mmapi_emit("ord.stable", undefined);
deq("equal priority is stable by registration order", global.ord_log, "abc");

// A late registration at equal priority goes to the back, not the middle.
global.ord_log = "";
mmapi_on("ord.late", ord_a, { mod_name: "ma" });
mmapi_on("ord.late", ord_b, { mod_name: "mb" });
mmapi_on("ord.late", ord_c, { mod_name: "mc" });
mmapi_on("ord.late", ord_d, { mod_name: "md" });
mmapi_emit("ord.late", undefined);
deq("a later equal-priority registration goes last", global.ord_log, "abcd");

// `after`: mb registered first but must run after mc.
global.ord_log = "";
mmapi_on("ord.after", ord_b, { mod_name: "mb", after: "mc" });
mmapi_on("ord.after", ord_c, { mod_name: "mc" });
mmapi_emit("ord.after", undefined);
deq("an `after` edge beats registration order", global.ord_log, "cb");

// `before`: md registered second but must run before ma.
global.ord_log = "";
mmapi_on("ord.before", ord_a, { mod_name: "ma" });
mmapi_on("ord.before", ord_d, { mod_name: "md", before: "ma" });
mmapi_emit("ord.before", undefined);
deq("a `before` edge beats registration order", global.ord_log, "da");

// An edge beats priority too: it is a hard constraint, not a tiebreak.
global.ord_log = "";
mmapi_on("ord.edgewins", ord_a, { priority: -10, mod_name: "ma" });
mmapi_on("ord.edgewins", ord_b, { priority: 10, mod_name: "mb", before: "ma" });
mmapi_emit("ord.edgewins", undefined);
deq("an edge overrides priority", global.ord_log, "ba");

// An edge naming a mod that never registered is inert, not an error.
global.ord_log = "";
mmapi_on("ord.ghost", ord_a, { mod_name: "ma", after: "nobody_here" });
mmapi_on("ord.ghost", ord_b, { mod_name: "mb" });
mmapi_emit("ord.ghost", undefined);
deq("an edge naming an absent mod is inert", global.ord_log, "ab");

// An array of mod names is accepted as well as a single name.
global.ord_log = "";
mmapi_on("ord.arr", ord_a, { mod_name: "ma", after: ["mb", "mc"] });
mmapi_on("ord.arr", ord_b, { mod_name: "mb" });
mmapi_on("ord.arr", ord_c, { mod_name: "mc" });
mmapi_emit("ord.arr", undefined);
deq("`after` accepts an array of mod names", global.ord_log, "bca");

// A cycle must not hang or crash: it warns and falls back to priority order.
// Two mods each insisting they go last is a real thing that happens, and the
// game still has to boot.
global.ord_log = "";
mmapi_on("ord.cycle", ord_a, { mod_name: "ma", after: "mb" });
mmapi_on("ord.cycle", ord_b, { mod_name: "mb", after: "ma" });
mmapi_emit("ord.cycle", undefined);
dcheck("contradictory edges do not crash or hang", true);
deq("  and fall back to priority order", global.ord_log, "ab");

// mmapi_hook_info reports the resolved dispatch order, which is what a mod
// author debugs with.
mmapi_on("ord.info", ord_a, { priority: 5, mod_name: "ma" });
mmapi_on("ord.info", ord_b, { priority: 1, mod_name: "mb" });
var info = mmapi_hook_info("ord.info");
deq("hook_info reports handlers in dispatch order: first", info.handlers[0].mod_name, "mb");
deq("hook_info reports handlers in dispatch order: second", info.handlers[1].mod_name, "ma");
deq("hook_info carries the priority", info.handlers[0].priority, 1);
