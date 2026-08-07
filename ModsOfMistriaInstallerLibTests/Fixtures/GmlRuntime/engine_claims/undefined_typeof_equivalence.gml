// The seam-uniformity claim: `v != undefined` and `typeof(v) != "undefined"`
// decide identically for every value this dialect can produce, on every data
// path a dispatch result can travel. The override seams test their
// mmapi_run_override result with one form or the other; if the two forms can
// ever disagree, normalizing the seams to typeof would change behaviour.
//
// Each value asserts the BICONDITIONAL directly - (v == undefined) must equal
// (typeof(v) == "undefined"), and likewise for the negated pair - so a FAIL
// names the exact value and path where the forms diverge.
//
// Every line below must print PASS. A FAIL means the forms are NOT equivalent
// on the pinned VM and the seams must keep their current comparisons.

function ueq_expect(label, got, want) {
    if (got == want) {
        show_debug_message("PASS " + label);
    } else {
        show_debug_message("FAIL " + label + " -- forms diverge");
    }
}

// The biconditional, both polarities, for one value on one path.
function ueq_check(label, v) {
    ueq_expect(label + ": (v == undefined) matches (typeof == undefined)",
        (v == undefined) == (typeof(v) == "undefined"), true);
    ueq_expect(label + ": (v != undefined) matches (typeof != undefined)",
        (v != undefined) == (typeof(v) != "undefined"), true);
}

// ---------------------------------------------------------------------------
// Direction 1: undefined itself, on every suspect path. The danger case is an
// undefined that fails `== undefined` while typeof still says "undefined"
// (the historical deep-call-frame suspicion).
// ---------------------------------------------------------------------------

ueq_check("undefined literal", undefined);

function ueq_ret_implicit() {
}
function ueq_ret_explicit() {
    return undefined;
}
ueq_check("implicit-undef return, 1 frame", ueq_ret_implicit());
ueq_check("explicit-undef return, 1 frame", ueq_ret_explicit());

// 2- and 3-deep call chains: a handler's return travels handler -> dispatcher
// -> seam, so the seam always reads its value at least two frames down.
function ueq_ret_depth2() {
    return ueq_ret_implicit();
}
function ueq_ret_depth3() {
    return ueq_ret_depth2();
}
ueq_check("implicit-undef return, 2 frames", ueq_ret_depth2());
ueq_check("implicit-undef return, 3 frames", ueq_ret_depth3());

// Bound to a local first, then checked (the seams' own shape).
var ueq_local = ueq_ret_depth3();
ueq_check("undef via local from 3 frames", ueq_local);

// Through a struct field, and via an absent member read.
var ueq_scratch = { result: undefined };
ueq_scratch.result = ueq_ret_implicit();
ueq_check("undef via struct field", ueq_scratch.result);
ueq_check("undef via absent struct member", ueq_scratch[$ "missing"]);

// Through an array element.
var ueq_arr = [undefined];
ueq_check("undef via array element", ueq_arr[0]);

// ---------------------------------------------------------------------------
// Direction 2: every non-undefined shape. The danger case is a value that
// loosely compares equal to undefined while typeof says otherwise.
// ---------------------------------------------------------------------------

ueq_check("bool false", false);
ueq_check("bool true", true);
ueq_check("int64 zero", 0);
ueq_check("int64 one", 1);
ueq_check("int64 negative", -1);
ueq_check("number zero", 0.0);
ueq_check("number half", 0.5);
ueq_check("number negative", -0.5);
ueq_check("empty string", "");
ueq_check("string zero", "0");
ueq_check("string false", "false");
ueq_check("string undefined", "undefined");
ueq_check("empty array", []);
ueq_check("one-element array", [0]);
ueq_check("empty struct", {});
ueq_check("populated struct", { a: 1 });
ueq_check("function reference", ueq_ret_implicit);

// A method bound to a struct, the other callable shape.
var ueq_host = { n: 1 };
ueq_host.m = method(ueq_host, function() { return self.n; });
ueq_check("bound method", ueq_host.m);

// Non-undefined values through the same deep-frame paths as direction 1.
function ueq_ret_false_d2() {
    return ueq_ret_false_d1();
}
function ueq_ret_false_d1() {
    return false;
}
function ueq_ret_zero_d2() {
    return ueq_ret_zero_d1();
}
function ueq_ret_zero_d1() {
    return 0;
}
ueq_check("bool false via 2 frames", ueq_ret_false_d2());
ueq_check("int64 zero via 2 frames", ueq_ret_zero_d2());

// ---------------------------------------------------------------------------
// The behavioural twin: the old and new seam conditions themselves, evaluated
// side by side over the winning-value shapes an override can produce. This is
// the exact decision the normalized seams make.
// ---------------------------------------------------------------------------

function ueq_old_consumes(v) {
    return v != undefined;
}
function ueq_new_consumes(v) {
    return typeof(v) != "undefined";
}
function ueq_same_decision(label, v) {
    ueq_expect(label + ": old and new seam conditions agree",
        ueq_old_consumes(v) == ueq_new_consumes(v), true);
}

ueq_same_decision("override answer undefined", undefined);
ueq_same_decision("override answer via implicit return", ueq_ret_implicit());
ueq_same_decision("override answer true", true);
ueq_same_decision("override answer false", false);
ueq_same_decision("override answer 0", 0);
ueq_same_decision("override answer 0.0", 0.0);
ueq_same_decision("override answer string", "handled");
ueq_same_decision("override answer struct", { handled: true });
