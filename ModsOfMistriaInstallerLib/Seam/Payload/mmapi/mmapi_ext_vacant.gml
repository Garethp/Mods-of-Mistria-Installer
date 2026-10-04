// mmapi_ext_vacant.gml. The vacancy query for extension points. The
// generated registry publishes a table of ledger vacancies at load, and
// engine seams ask this function before showing a member to the player.

// Is this ordinal a ledger vacancy - an extension symbol whose mod is not
// installed? Reads the vacant table the generated registry publishes at load.
// Guarded with the [$ ] accessor throughout: the framework must work when the
// generated file is absent (no registrants, bare engine, unit tests), so this
// can reference nothing the registry defines by name.
function mmapi_ext_is_vacant(point, ordinal) {
    if (global[$ "__mmapi_ext_vacant"] == undefined) { return false; }
    var __entries = global.__mmapi_ext_vacant[$ point];
    if (__entries == undefined) { return false; }
    return __entries[$ string(ordinal)] != undefined;
}
