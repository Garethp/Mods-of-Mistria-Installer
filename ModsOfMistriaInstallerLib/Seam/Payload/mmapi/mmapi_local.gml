// mmapi_local.gml. The localisation waist: the one GML point that sees every
// engine localisation lookup.
//
// local_get is a native builtin with no GML body, so the installer rewrites
// every GML local_get() call site to mmapi_local_get() (the [[call_rewrite]]
// catalog form). scripts/mmapi/ is excluded from the rewrite, so the native
// call below survives and a handler calling local_get hits the native
// directly, with no re-entry.
//
// Two filter hooks dispatch here:
//   "local.missing"  value starts undefined, ctx { key, fallback }. Fires only
//                    when the native localizer misses. fallback is what the
//                    engine would have surfaced.
//   "local.get"      value → the resolved text, ctx → the key itself, so no
//                    allocation. Fires on every lookup, after miss handling,
//                    so injected text is visible to it.
//
// A miss is one of three results: undefined, the key echoed back, or the
// "MISSING" sentinel. The engine's TestSuite treats MISSING and PLACEHOLDER as
// the localizer's own markers. "PLACEHOLDER" → the key exists with pending
// text, which is a hit, so local.missing does not fire. A non-string key
// forwards to the native unchanged; == across types is falsy and structs
// compare by reference, so the comparisons tolerate it.
//
// Cost per lookup with no handlers: one extra call frame, at most three
// comparisons, and mmapi_apply_filters' empty-registry early-outs. The
// local.missing ctx struct is allocated only on the miss path.
//
// Handler errors are isolated by the try/catch at the dispatch call site. The
// try/catch lines here were believed inert (this function takes an argument,
// see the old guarded-call quirk note in mmapi.gml). They are not: they catch
// normally on the pinned VM and in-engine, so they are real protection,
// matching the dispatch lines the catalog injects into engine functions.

function mmapi_local_get(key) {
    var text = local_get(key);
    if (text == undefined || text == key || text == "MISSING") {
        var injected = undefined;
        try {
            injected = mmapi_apply_filters("local.missing", undefined,
                { key: key, fallback: text });
        } catch (err) {}
        if (injected != undefined) {
            text = injected;
        }
    }
    try {
        text = mmapi_apply_filters("local.get", text, key);
    } catch (err) {}
    return text;
}
