// MMAPI - A GML modding framework for Fields of Mistria
// Copyright (C) 2026 Anna Nomoly
//
// This file is part of MMAPI, distributed with the Mods of Mistria Installer.
// Licensed under the GNU General Public License v3.0 or later, WITH
// ADDITIONAL TERMS under GPLv3 section 7 (attribution preservation, no
// misrepresentation of origin, no trademark grant).
//
// See the LICENSE file in this directory for those additional terms.
// See LICENCE.txt at the repository root for the full GPL text.
//
// SPDX-License-Identifier: GPL-3.0-or-later

// mmapi_events.gml. Derived events: a few cheap pieces of engine state read
// once per frame, emitting a named event whenever one of them changes. The
// poll runs from the lifecycle install drain, which is the Game begin_step
// seam.
//
//   "game.room_changed"  ctx { previous, current }
//   "game.day_changed"   ctx { total_days }
//
// These fire from begin_step, which runs after room_start, so they report a
// change that has already happened. To change room content as it loads, use
// the in-file seams (dungeon.floor_enter and Taxi) instead.
//
// The first poll of a session records the current state as the baseline. After
// that, an event fires each time the state changes.
//
// This poll runs from the Game object's begin_step, and no Game instance ever
// steps in the title room. At the boot title none exists yet, and quit-to-
// title halts stepping entirely.

function mmapi_events_poll() {
    var current_room = room();
    var current_days = total_days();

    if (global[$ "__mmapi_events_state"] == undefined) {
        global.__mmapi_events_state = {
            last_room: current_room,
            last_total_days: current_days,
        };
        return;
    }
    var state = global.__mmapi_events_state;

    if (current_room != state.last_room) {
        var previous_room = state.last_room;
        state.last_room = current_room;
        mmapi_emit("game.room_changed", { previous: previous_room, current: current_room });
    }

    if (current_days != state.last_total_days) {
        state.last_total_days = current_days;
        mmapi_emit("game.day_changed", { total_days: current_days });
    }
}

__mmapi_register_as(mmapi_events_poll, "mmapi");
