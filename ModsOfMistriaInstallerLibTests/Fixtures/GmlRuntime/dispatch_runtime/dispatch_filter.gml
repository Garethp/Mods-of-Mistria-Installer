// Filter dispatch: the value chains handler to handler, and `undefined` means
// "declined" rather than "the value is now undefined".
//
// The false and 0 cases are the point of this file. Distinguishing a decline
// from a real falsy value is protocol, separate from the disproven engine claim
// about undefined comparing equal to false, and it is what typeof() in the
// dispatcher is for.

function f_double(v, ctx)  { return v * 2; }
function f_inc(v, ctx)     { return v + 1; }
function f_decline(v, ctx) { return undefined; }
function f_nothing(v, ctx) { }                 // implicit undefined
function f_false(v, ctx)   { return false; }
function f_zero(v, ctx)    { return 0; }
function f_seen(v, ctx)    { global.f_v = v; global.f_c = ctx; return v; }

deq("no handlers returns the input unchanged",
    mmapi_apply_filters("f.none", 5, undefined), 5);

mmapi_filter("f.one", f_double);
deq("a filter transforms the value", mmapi_apply_filters("f.one", 21, undefined), 42);

// Order is observable: double-then-inc is (5*2)+1 = 11, where the other order
// would give 12, so this pins the direction.
mmapi_filter("f.chain", f_double);
mmapi_filter("f.chain", f_inc);
deq("filters chain in dispatch order", mmapi_apply_filters("f.chain", 5, undefined), 11);

mmapi_filter("f.dec", f_decline);
deq("an explicit undefined declines and keeps the current value",
    mmapi_apply_filters("f.dec", 7, undefined), 7);

mmapi_filter("f.nothing", f_nothing);
deq("a handler that returns nothing at all also declines",
    mmapi_apply_filters("f.nothing", 7, undefined), 7);

// Declining mid-chain keeps the running value, not the original input.
mmapi_filter("f.decmid", f_double);
mmapi_filter("f.decmid", f_decline);
mmapi_filter("f.decmid", f_inc);
deq("a decline mid-chain keeps the running value, not the original",
    mmapi_apply_filters("f.decmid", 5, undefined), 11);

// The protocol rule: false and 0 are ordinary values a handler set on purpose,
// and only a genuine undefined declines. A dispatcher testing `== undefined`
// loosely, or testing truthiness, would swallow these and silently keep the
// previous value.
mmapi_filter("f.false", f_false);
deq("false is a real value, not a decline",
    mmapi_apply_filters("f.false", 9, undefined), false);

mmapi_filter("f.zero", f_zero);
deq("0 is a real value, not a decline",
    mmapi_apply_filters("f.zero", 9, undefined), 0);

// A false value must also survive being chained through a later decline.
mmapi_filter("f.falsethru", f_false);
mmapi_filter("f.falsethru", f_decline);
deq("a false value threads through a later declining filter",
    mmapi_apply_filters("f.falsethru", 9, undefined), false);

mmapi_filter("f.args", f_seen);
mmapi_apply_filters("f.args", 3, { k: 1 });
deq("a filter receives the current value as arg 0", global.f_v, 3);
deq("a filter receives ctx as arg 1", global.f_c.k, 1);

// Kind isolation: only filter records dispatch here.
function f_event_impostor(ctx) { global.f_impostor = true; }
global.f_impostor = false;
mmapi_filter("f.mixed", f_double);
mmapi_on("f.mixed", f_event_impostor);
deq("apply_filters dispatches only filter-kind records",
    mmapi_apply_filters("f.mixed", 4, undefined), 8);
dcheck("  and left the event record alone", global.f_impostor == false);
