// MMAPI - A GML modding framework for Fields of Mistria
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

// mmapi_monsters.gml. Ownership and save helpers for custom monsters.

function __mmapi_monsters_enabled() {
    return global[$ "__mmapi_custom_monsters_enabled"] == true;
}

function __mmapi_monster_is_vanilla_key(name) {
    var keys = global[$ "__mmapi_vanilla_monster_keys"];
    return keys != undefined && keys[$ name] == true;
}

function mmapi_monster_is_vanilla_id(monster_id) {
    if (!is_real(monster_id) || monster_id < 0 || monster_id >= MonsterId.LEN)
        return false;
    if (!__mmapi_monsters_enabled()) return true;
    return __mmapi_monster_is_vanilla_key(monster_id_to_string(monster_id));
}

function __mmapi_monster_owner_for_key(monster_key) {
    var owners = global[$ "__mmapi_custom_monster_owners"];
    if (owners == undefined) return undefined;
    return owners[$ monster_key];
}

function mmapi_monster_owner(monster_id) {
    if (!is_real(monster_id) || monster_id < 0 || monster_id >= MonsterId.LEN)
        return undefined;
    return __mmapi_monster_owner_for_key(monster_id_to_string(monster_id));
}

function mmapi_monster_is_custom_id(monster_id) {
    return mmapi_monster_owner(monster_id) != undefined;
}

function mmapi_monster_save_kills(kills) {
    if (!__mmapi_monsters_enabled())
        return array_to_struct(kills, monster_id_to_string);

    var output = array_to_struct(kills, monster_id_to_string);
    var names = struct_get_names(output);
    for (var i = 0; i < array_length(names); i++) {
        var name = names[i];
        if (__mmapi_monster_is_vanilla_key(name)) continue;
        struct_remove(output, name);
        mmapi_log_debug("mmapi", "monster save omitted custom name `" + name + "`");
    }
    return output;
}

function mmapi_monster_load_kills(kills, saved) {
    var names = struct_get_names(saved);
    var custom_enabled = __mmapi_monsters_enabled();
    if (!custom_enabled) {
        var native_load = true;
        for (var i = 0; i < array_length(names); i++) {
            var resolved_id = try_string_to_monster_id(names[i]);
            if (resolved_id == undefined || resolved_id < 0 || resolved_id >= array_length(kills)) {
                native_load = false;
                break;
            }
        }
        if (native_load) {
            apply_struct_to_array(kills, saved, try_string_to_monster_id);
            return;
        }
    }

    for (var i = 0; i < array_length(names); i++) {
        var name = names[i];
        if (custom_enabled && !__mmapi_monster_is_vanilla_key(name)) {
            mmapi_log_warn("mmapi", "save carried custom monster name `" + name + "`; dropped");
            continue;
        }
        var monster_id = try_string_to_monster_id(name);
        if (monster_id == undefined || monster_id < 0 || monster_id >= array_length(kills)) {
            mmapi_log_warn("mmapi", "save carried unavailable monster kill key `" + name + "`; dropped");
            continue;
        }
        kills[monster_id] = saved[$ name];
    }
}
