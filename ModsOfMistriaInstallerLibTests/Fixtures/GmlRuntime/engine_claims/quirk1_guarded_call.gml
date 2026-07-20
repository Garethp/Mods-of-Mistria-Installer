// Quirk 1: "A try/catch inside a function that was itself called with
// arguments fails to catch exceptions raised in callees. The error is
// silently swallowed and execution resumes after the try/catch."
//   -- mmapi.gml, the guarded-call quirk note.
//
// That claim was the entire justification for __mmapi_guarded_call: the
// trampoline, the reused global scratch struct, the argc/arg0/arg1
// marshalling, and the re-entrancy discipline.
//
// Every shape below must print PASS. A FAIL means the quirk reproduces at this
// fabricator rev and the trampoline was earning its keep after all.
//
// .gml on purpose: the extension selects the compat dialect mmapi ships in.

function q1_thrower() {
    throw "boom";
}

function q1_check(label, got) {
    if (got == "YES") {
        show_debug_message("PASS " + label);
    } else {
        show_debug_message("FAIL " + label + " -- catch was inert (quirk reproduced)");
    }
}

// no declared params, called with no args: the baseline
function q1_p0_a0() {
    var c = "NO";
    try { q1_thrower(); } catch (e) { c = "YES"; }
    return c;
}

// no declared params, called with args: fabricator's own try_catch.gml shape
function q1_p0_a3() {
    var c = "NO";
    try { q1_thrower(); } catch (e) { c = "YES"; }
    return c;
}

// declared params, called with args: the mmapi_config_save(mod_name, cfg) shape
function q1_p2_a2(x, y) {
    var c = "NO";
    try { q1_thrower(); } catch (e) { c = "YES"; }
    return c;
}

// declared params, called with no args
function q1_p2_a0(x, y) {
    var c = "NO";
    try { q1_thrower(); } catch (e) { c = "YES"; }
    return c;
}

// throw written directly in the try, not raised in a callee
function q1_direct(x) {
    var c = "NO";
    try { throw "direct"; } catch (e) { c = "YES"; }
    return c;
}

// callee reached through a local variable: a handler reference
function q1_via_local(hook_name, ctx) {
    var c = "NO";
    var fn = q1_thrower;
    try { fn(ctx); } catch (e) { c = "YES"; }
    return c;
}

// callee reached through a struct field: the scratch.fn shape
function q1_via_struct(hook_name, ctx) {
    var c = "NO";
    var scratch = { fn: q1_thrower };
    try { scratch.fn(); } catch (e) { c = "YES"; }
    return c;
}

// callee reached through a struct on `global`, where mmapi's scratch lives
function q1_via_global(hook_name, ctx) {
    global.__q1_scratch = { fn: q1_thrower };
    var c = "NO";
    var s = global.__q1_scratch;
    try { s.fn(); } catch (e) { c = "YES"; }
    return c;
}

// nested arg-taking frames
function q1_inner(a, b) {
    var c = "NO";
    var fn = q1_thrower;
    try { fn(a); } catch (e) { c = "YES"; }
    return c;
}
function q1_outer(x, y) {
    return q1_inner(x, y);
}

// mmapi_emit's actual structure without the trampoline: an arg-taking
// dispatcher looping over handler records, calling record.fn(ctx) in a
// try/catch. This is the shape the quirk claims cannot work.
function q1_emit_shape(hook_name, ctx) {
    var handlers = [{ fn: q1_thrower, mod_name: "probe" }];
    var caught = 0;
    for (var i = 0; i < array_length(handlers); i++) {
        var record = handlers[i];
        try {
            record.fn(ctx);
        } catch (err) {
            caught += 1;
        }
    }
    if (caught == 1) { return "YES"; }
    return "NO";
}

q1_check("params=0 args=0 named callee", q1_p0_a0());
q1_check("params=0 args=3 named callee", q1_p0_a3(1, 2, 3));
q1_check("params=2 args=2 named callee (mmapi_config_save shape)", q1_p2_a2(1, 2));
q1_check("params=2 args=0 named callee", q1_p2_a0());
q1_check("params=1 args=1 direct throw", q1_direct(1));
q1_check("params=2 args=2 callee via local", q1_via_local("h", undefined));
q1_check("params=2 args=2 callee via struct field", q1_via_struct("h", undefined));
q1_check("params=2 args=2 callee via global scratch", q1_via_global("h", undefined));
q1_check("nested arg-taking frames", q1_outer(1, 2));
q1_check("mmapi_emit shape WITHOUT the trampoline", q1_emit_shape("h", 42));
