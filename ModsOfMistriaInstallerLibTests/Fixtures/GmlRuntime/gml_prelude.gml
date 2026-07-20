// Prepended to every dispatch_*.gml body, after the whole mmapi framework. Not
// a test itself: the assertion helpers, plus the one line of setup every body
// needs.

// PASS/FAIL is the wire format. The harness asserts no FAIL and at least one
// PASS, so a body that asserts nothing cannot pass vacuously.
function dcheck(label, ok) {
    if (ok) {
        show_debug_message("PASS " + label);
    } else {
        show_debug_message("FAIL " + label);
    }
}

// The workhorse. Reports what it got on failure: a bare FAIL on a
// dispatch-order test says nothing useful.
function deq(label, got, want) {
    if (got == want) {
        show_debug_message("PASS " + label);
    } else {
        show_debug_message("FAIL " + label + " -- got " + string(got)
                           + ", want " + string(want));
    }
}

// Sinks off. mmapi's console sink reaches three game-only names that do not
// exist off the engine: text_color_logging, LogLevel.* and CONFIG_DIRECTORY.
// The compat dialect late-binds them, so they only fail when called, and this
// keeps them uncalled. Registering an uncataloged hook warns, and every body
// here does that constantly, so without this the noise would be the output.
//
// This is belt-and-braces rather than load-bearing: the console sink wraps
// text_color_logging in a try/catch and falls back to a plain line, and that
// catch genuinely works (it was believed inert under the guarded-call quirk,
// which is false). Sinks stay off because the warnings are noise here, not
// because the alternative is a crash.
//
// A body that asserts on log output re-enables the file sink itself with
// `mmapi_log_set_sinks(false, true)` - see dispatch_error_reporting.gml. Only
// the file sink fills the per-mod buffer, and off-engine it writes nothing:
// __mmapi_log_buffer_append returns early while mmapi_io_is_ready() is false.
// The console sink is the one to leave off.
mmapi_log_set_sinks(false, false);
