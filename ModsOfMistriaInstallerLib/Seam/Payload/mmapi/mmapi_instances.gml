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

// mmapi_instances.gml. The instance poll behind the runtime-provided
// "instance.created" event. A registration names the object it watches in
// opts.object, and once per frame this poll walks that object's live
// instances with `with`, stamps each one it has not seen with the
// registration's own marker field, and dispatches the handler with the
// instance as ctx. The poll runs from the lifecycle install drain, which is
// the Game begin_step seam, so by the time an instance is reached its whole
// create chain has finished.
//
//   "instance.created"   ctx → the instance
//
// Each registration carries its own marker (minted in __mmapi_hook_register),
// so two registrations watching overlapping objects, or one registered late,
// each fire exactly once per instance with no shared bookkeeping.
//
// The hot roots are refused here rather than at registration, because object
// asset names only resolve inside the game. obj_node_renderer is every grid
// object in the room and par_interactable is every interactable, and a scan
// of either every frame is a cost every player would pay.

function mmapi_instances_poll() {
    var registry = global[$ "__mmapi_hooks"];
    if (registry == undefined) { return; }
    var handlers = registry[$ "instance.created"];
    if (handlers == undefined) { return; }

    var count = array_length(handlers);
    for (var i = 0; i < count; i++) {
        var record = handlers[i];
        if (record.kind != "event") { continue; }
        var watched = record[$ "object"];
        if (watched == undefined) { continue; }

        if (record[$ "refused"] == undefined) {
            record.refused = (watched == obj_node_renderer || watched == par_interactable);
            if (record.refused) {
                mmapi_warn_rate_limited(
                    "hook_hot_root:" + string(record.mod_name),
                    record.mod_name,
                    "mmapi hook instance.created: " + string(record.mod_name)
                    + " watches obj_node_renderer or par_interactable, which is refused. "
                    + "Watch a concrete object or a narrower parent, or use object.interact for grid objects");
            }
        }
        if (record.refused) { continue; }

        var marker = record.marker;
        var mod_name = record.mod_name;

        // The function's locals stay visible inside the with block, and the
        // handler is called through the record the way mmapi_emit calls it.
        with (watched) {
            if (self[$ marker] != undefined) { continue; }
            self[$ marker] = true;
            __mmapi_hook_fired("instance.created");
            try {
                record.fn(self);
            } catch (err) {
                __mmapi_hook_handler_failed("instance.created", mod_name, err);
            }
        }
    }
}

__mmapi_register_as(mmapi_instances_poll, "mmapi");
