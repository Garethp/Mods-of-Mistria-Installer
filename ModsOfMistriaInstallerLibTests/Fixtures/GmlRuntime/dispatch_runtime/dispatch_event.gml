// Event dispatch: every event handler runs, in dispatch order, and the return
// value means nothing.

function e_a(ctx)   { global.e_log += "a"; }
function e_b(ctx)   { global.e_log += "b"; }
function e_ctx(ctx) { global.e_seen = ctx; }
function e_ret(ctx) { global.e_log += "r"; return "ignored"; }

// A filter registered on an event name, which mmapi_emit must skip. One name
// should hold one kind, but nothing stops a mod registering the wrong one, so
// dispatch filters by kind rather than trusting it.
function e_filter_impostor(v, ctx) { global.e_log += "F"; return v; }

global.e_log = "";
global.e_seen = undefined;

// No handlers at all is a no-op, not a crash. The registry itself may not exist
// yet on a boot's first call, which is the case this covers.
mmapi_emit("e.none", undefined);
dcheck("emitting a hook with no handlers is a no-op", true);

mmapi_on("e.one", e_a);
mmapi_emit("e.one", undefined);
deq("a single event handler runs", global.e_log, "a");

global.e_log = "";
mmapi_on("e.two", e_a);
mmapi_on("e.two", e_b);
mmapi_emit("e.two", undefined);
deq("every event handler runs, in dispatch order", global.e_log, "ab");

mmapi_on("e.ctx", e_ctx);
mmapi_emit("e.ctx", { hello: 42 });
deq("the handler receives ctx", global.e_seen.hello, 42);

// Emitting twice runs the handlers twice: dispatch is not once-only, and hooks
// that fire per frame depend on that.
global.e_log = "";
mmapi_on("e.twice", e_a);
mmapi_emit("e.twice", undefined);
mmapi_emit("e.twice", undefined);
deq("emitting twice dispatches twice", global.e_log, "aa");

// An event handler's return value is discarded, and returning one is harmless.
global.e_log = "";
mmapi_on("e.ret", e_ret);
dcheck("mmapi_emit returns undefined regardless of the handler",
       typeof(mmapi_emit("e.ret", undefined)) == "undefined");
deq("  and the handler still ran", global.e_log, "r");

// Kind isolation.
global.e_log = "";
mmapi_on("e.mixed", e_a);
mmapi_filter("e.mixed", e_filter_impostor);
mmapi_emit("e.mixed", undefined);
deq("emit dispatches only event-kind records", global.e_log, "a");

// A duplicate registration (same fn, kind and mod) is skipped: installers rerun
// every frame, and without this the handler list would compound without bound.
global.e_log = "";
mmapi_on("e.dup", e_a, { mod_name: "dupmod" });
mmapi_on("e.dup", e_a, { mod_name: "dupmod" });
mmapi_emit("e.dup", undefined);
deq("a duplicate registration does not land twice", global.e_log, "a");

// The same fn from a different mod is a distinct record.
global.e_log = "";
mmapi_on("e.twomods", e_a, { mod_name: "m1" });
mmapi_on("e.twomods", e_a, { mod_name: "m2" });
mmapi_emit("e.twomods", undefined);
deq("the same function from two mods is two records", global.e_log, "aa");
