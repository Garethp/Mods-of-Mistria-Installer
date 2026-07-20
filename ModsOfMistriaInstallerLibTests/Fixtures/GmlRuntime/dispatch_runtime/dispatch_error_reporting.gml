// Error reporting. dispatch_isolation.gml proves a throwing handler is
// contained; this proves the failure is counted and surfaced. Isolation that
// loses the error is silence: the mod misbehaves and nobody can say why.
//
// The surfacing is rate-limited. Hot hooks fire per instance per frame, so a
// handler throwing every frame would write a log line every frame and drown the
// file it is meant to inform.

// The file sink fills the per-mod buffer (see mmapi_log's
// `if (mmapi_log_file_on())`), so it has to be on to assert on log lines at
// all. It costs no file IO here: __mmapi_log_buffer_append returns early while
// mmapi_io_is_ready() is false, which it is off-engine, so lines accumulate in
// memory and nothing is written. The console sink stays off, since that is the
// one reaching the game-only names.
mmapi_log_set_sinks(false, true);

function er_count_lines(lines, needle) {
    var found = 0;
    for (var i = 0; i < array_length(lines); i++) {
        if (string_pos(needle, lines[i]) > 0) { found += 1; }
    }
    return found;
}

function er_throws(value, ctx)     { throw "filter exploded"; }
function er_downstream(value, ctx) { global.er_runs += 1; return value + 1; }

global.er_runs = 0;

// The thrower runs first at lower priority, so the downstream filter can only
// see the kept value if the throw was genuinely contained.
mmapi_filter("er.hook", er_throws,     { priority: 0, mod_name: "err_mod" });
mmapi_filter("er.hook", er_downstream, { priority: 1, mod_name: "good_mod" });

// 10 → throws, current kept at 10 → downstream sees 10 → 11
deq("a throwing filter keeps the value and the downstream filter still runs",
    mmapi_apply_filters("er.hook", 10, undefined), 11);

var needle = "mmapi hook er.hook: handler from err_mod failed";
deq("the first failure logs immediately",
    er_count_lines(mmapi_log_buffer_lines("err_mod"), needle), 1);
dcheck("  and the line carries the thrown value, not just the fact of a throw",
       er_count_lines(mmapi_log_buffer_lines("err_mod"), "filter exploded") > 0);

// Failures 2..60 are suppressed.
for (var i = 2; i <= 60; i++) {
    mmapi_apply_filters("er.hook", 10, undefined);
}
deq("failures 2..60 are suppressed - a per-frame thrower cannot drown the log",
    er_count_lines(mmapi_log_buffer_lines("err_mod"), needle), 1);

// The 61st logs again, so a persistent fault stays visible.
mmapi_apply_filters("er.hook", 10, undefined);
deq("the 61st failure logs again (one line per 60)",
    er_count_lines(mmapi_log_buffer_lines("err_mod"), needle), 2);

// The count keeps the true total even while the log is suppressed, which is why
// counting and logging are separate: the log is for humans and is throttled,
// the count feeds mmapi_hook_stats() and must be exact.
var stats = mmapi_hook_stats();
deq("the error count keeps the true total while the log is throttled",
    stats.errors[$ "err_mod"], 61);
dcheck("the innocent mod is charged nothing",
       stats.errors[$ "good_mod"] == undefined);

// And the whole point: the downstream filter ran on every single dispatch.
deq("the downstream filter ran on all 61 dispatches", global.er_runs, 61);
