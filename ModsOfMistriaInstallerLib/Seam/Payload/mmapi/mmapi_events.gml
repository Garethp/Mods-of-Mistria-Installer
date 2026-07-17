// mmapi_events.gml. Derived events: a few cheap pieces of engine state read
// once per frame, emitting a named event whenever one of them changes. The
// poll runs from the lifecycle install drain, which is the Game begin_step
// seam.
//
//   "game.room_changed"  ctx { previous, current }
//   "game.day_started"   ctx { total_days }
//   "game.title_entered" ctx {}
//
// These fire from begin_step, which runs after room_start, so they report a
// change that has already happened. To change room content as it loads, use
// the in-file seams (dungeon.floor_enter and Taxi) instead.
//
// The first poll of a session records the current state as the baseline. After
// that, an event fires each time the state changes.

// rm_menu is the engine's title and menu room, defined in
// scripts/GameplaySystems/RoomCheckScripts.gml under is_menu_room. Tests fake
// the predicate through global.__mmapi_events_title_room_name.
function mmapi_events_title_room_name() {
    var override = global[$ "__mmapi_events_title_room_name"];
    if (override != undefined) { return override; }
    return "rm_menu";
}

function mmapi_events_room_is_title(room_value) {
    // asset_to_string, not room_get_name: this engine has no room_get_name and
    // calling it throws "no such field" in-engine. A gm_room is an asset, and
    // the engine reads room names with asset_to_string.
    return asset_to_string(room_value) == mmapi_events_title_room_name();
}

function mmapi_events_poll() {
    var current_room = room();
    var current_days = total_days();

    if (global[$ "__mmapi_events_state"] == undefined) {
        global.__mmapi_events_state = {
            last_room: current_room,
            last_room_was_title: mmapi_events_room_is_title(current_room),
            last_total_days: current_days,
        };
        return;
    }
    var state = global.__mmapi_events_state;

    if (current_room != state.last_room) {
        var previous_room = state.last_room;
        state.last_room = current_room;
        mmapi_emit("game.room_changed", { previous: previous_room, current: current_room });

        var now_title = mmapi_events_room_is_title(current_room);
        if (now_title && state.last_room_was_title == false) {
            mmapi_emit("game.title_entered", {});
        }
        state.last_room_was_title = now_title;
    }

    if (current_days != state.last_total_days) {
        state.last_total_days = current_days;
        mmapi_emit("game.day_started", { total_days: current_days });
    }
}

__mmapi_register_as(mmapi_events_poll, "mmapi");
