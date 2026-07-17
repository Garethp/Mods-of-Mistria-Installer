// Quirk 2: "A value returned through a call frame can compare equal to both
// undefined and false (an implicit or explicit undefined return satisfies
// `== false`). Callers must therefore test `result == undefined` first and
// only then `result == false`."
//   -- mmapi.gml, quirk 2.
//
// mmapi_check_guards tests `== undefined` first and only then `== false`, on
// the strength of that note. If the quirk were real and those two lines were
// reversed, a guard handler returning nothing would veto instead of allow,
// silently changing game behaviour.
//
// Every line below must print PASS. A FAIL means the quirk reproduces and the
// ordering discipline in mmapi_check_guards is load-bearing after all.

function q2_ret_implicit_undef() {
}

function q2_ret_explicit_undef() {
    return undefined;
}

function q2_ret_false() {
    return false;
}

function q2_ret_true() {
    return true;
}

function q2_ret_zero() {
    return 0;
}

function q2_expect(label, got, want) {
    if (got == want) {
        show_debug_message("PASS " + label);
    } else {
        show_debug_message("FAIL " + label + " -- quirk reproduced");
    }
}

// the baseline: a literal undefined, never through a call frame
var q2_lit = undefined;
q2_expect("literal undefined == false is false", q2_lit == false, false);
q2_expect("literal undefined == undefined is true", q2_lit == undefined, true);

// the claim: an undefined returned through a call frame
var q2_r1 = q2_ret_implicit_undef();
q2_expect("implicit-undef return == false is false", q2_r1 == false, false);
q2_expect("implicit-undef return == undefined is true", q2_r1 == undefined, true);

var q2_r2 = q2_ret_explicit_undef();
q2_expect("explicit-undef return == false is false", q2_r2 == false, false);
q2_expect("explicit-undef return == undefined is true", q2_r2 == undefined, true);

// inline, never bound to a local first (a different code path)
q2_expect("inline implicit-undef call == false is false",
    q2_ret_implicit_undef() == false, false);

// the converse: a real false must not read as undefined
var q2_r3 = q2_ret_false();
q2_expect("false return == undefined is false", q2_r3 == undefined, false);
q2_expect("false return == false is true", q2_r3 == false, true);

// other values must not read as undefined either
q2_expect("zero return == undefined is false", q2_ret_zero() == undefined, false);
q2_expect("true return == undefined is false", q2_ret_true() == undefined, false);

// through a struct field: the scratch.result path the trampoline used
var q2_scratch = { result: undefined };
q2_scratch.result = q2_ret_implicit_undef();
var q2_via = q2_scratch.result;
q2_expect("via scratch.result == false is false", q2_via == false, false);
q2_expect("via scratch.result == undefined is true", q2_via == undefined, true);

// typeof, which mmapi_apply_filters uses instead of `== undefined`
q2_expect("typeof(implicit-undef return) is undefined",
    typeof(q2_ret_implicit_undef()) == "undefined", true);
q2_expect("typeof(false return) is bool", typeof(q2_ret_false()) == "bool", true);
q2_expect("typeof(zero return) is not undefined",
    typeof(q2_ret_zero()) == "undefined", false);

// The decisive behavioural test: mmapi_check_guards' logic with the ordering
// deliberately reversed, which is the mistake the quirk note exists to prevent.
// A guard returning nothing must still be allowed. "VETOED" means quirk 2 is
// real and the note is earning its keep.
function q2_guard_returns_nothing() {
}

function q2_check_guards_reversed() {
    var scratch = { result: undefined };
    scratch.result = q2_guard_returns_nothing();
    var result = scratch.result;
    if (result == false) { return "VETOED"; }
    if (result == undefined) { return "allowed"; }
    return "allowed";
}
q2_expect("check_guards with REVERSED order still allows",
    q2_check_guards_reversed() == "allowed", true);
