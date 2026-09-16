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

// mmapi_monster_categories.gml. Builds prototypes for the custom monster
// categories MOMI found while installing mods.

function __mmapi_monster_category_descriptors() {
    if (global[$ "__mmapi_monster_categories"] == undefined) {
        global.__mmapi_monster_categories = global[$ "__mmapi_monster_category_catalog"] ?? [];
        if (array_length(global.__mmapi_monster_categories) > 0)
            mmapi_log_info("mmapi", "custom monster registry ready: "
                + string(array_length(global.__mmapi_monster_categories)) + " category(s)");
        for (var i = 0; i < array_length(global.__mmapi_monster_categories); i++) {
            var descriptor = global.__mmapi_monster_categories[i];
            mmapi_log_debug("mmapi", "custom monster category `" + descriptor.key
                + "` from " + descriptor.mod_name + " has "
                + string(array_length(descriptor.state_names)) + " state(s)");
        }
    }
    return global.__mmapi_monster_categories;
}

function __mmapi_monster_build_custom_prototypes(prototypes) {
    var descriptors = __mmapi_monster_category_descriptors();
    for (var category_index = 0; category_index < array_length(descriptors); category_index++) {
        var descriptor = descriptors[category_index];
        var category = MonsterCategory.LEN + category_index;
        var data = fiddle_get("monsters/" + descriptor.key);
        if (data == undefined) {
            mmapi_log_warn("mmapi", "custom monster category `" + descriptor.key
                + "` has no Fiddle table; no prototypes built");
            continue;
        }
        var default_data = struct_exists(data, "default") ? data[$ "default"] : {};
        var names = descriptor.state_names;

        for (var monster_id = 0; monster_id < MonsterId.LEN; monster_id++) {
            var entry_name = monster_id_to_string(monster_id);
            var entry = struct_exists(data, entry_name) ? data[$ entry_name] : undefined;
            if (entry == undefined) continue;
            if (prototypes[monster_id] != undefined) {
                mmapi_log_warn("mmapi", "monster prototype `" + entry_name
                    + "` from category `" + descriptor.key
                    + "` rejected: this MonsterId already has a prototype");
                continue;
            }

            var obj = clone_value(default_data);
            patch_object(entry, obj);
            var catalogue = array_create(array_length(names), undefined);
            for (var i = 0; i < array_length(names); i++) {
                var value = obj.sprites[$ names[i]];
                assert_neq(value, undefined,
                    "sprite.{} was undefined in {MonsterId}, but we need it for custom category {}!",
                    names[i], monster_id, descriptor.key);
                if (is_string(value)) {
                    var asset = string_to_asset(value);
                    catalogue[i] = [asset, asset, asset, asset];
                } else {
                    catalogue[i] = [
                        opt_and_then(value[$ "east"], string_to_asset),
                        string_to_asset(value.north),
                        opt_and_then(value[$ "east"], string_to_asset),
                        string_to_asset(value.south),
                    ];
                }
            }

            obj.misc_sprites = {};
            if (struct_exists(obj.sprites, "misc")) {
                var misc_names = struct_get_names(obj.sprites.misc);
                for (var j = 0; j < array_length(misc_names); j++)
                    obj.misc_sprites[$ misc_names[j]] = string_to_asset(obj.sprites.misc[$ misc_names[j]]);
            }
            obj.sprites = undefined;
            obj.sprite_catalogue = catalogue;

            obj.tango_catalogue = array_create(array_length(names), undefined);
            for (var i = 0; i < array_length(names); i++)
                obj.tango_catalogue[i] = opt_and_then(obj.tango[$ names[i]], audio_asset_assert_exists);
            obj.misc_tango = {};
            if (struct_exists(obj.tango, "misc")) {
                var tango_names = struct_get_names(obj.tango.misc);
                for (var j = 0; j < array_length(tango_names); j++)
                    obj.misc_tango[$ tango_names[j]] = audio_asset_assert_exists(obj.tango.misc[$ tango_names[j]]);
            }
            obj.tango = undefined;

            obj.coin_count = opt_and_then(obj[$ "coin_count"], fiddle_deserialize_vec2) ?? [0, 0];
            obj.monster_category = category;
            obj.monster_id = monster_id;
            obj.gm_object = object(obj.gm_object);
            obj.hurtbox = opt_and_then(obj[$ "hurtbox"], string_to_asset);
            obj.hitbox = opt_and_then(obj[$ "hitbox"], string_to_asset);

            var drops = List();
            for (var j = 0; j < array_length(obj.drops); j++) {
                var drop = obj.drops[j];
                var chance = drop[$ "chance"] ?? 100;
                var count_range = drop[$ "count_range"] ?? [1, 1];
                var exclusive = bool(drop[$ "exclusive"] ?? true);
                var perfect = drop[$ "perfect_pick_chance"] ?? 0;
                drops.push(new ItemDrop(chance, exclusive, parse_reward(drop), count_range, perfect));
            }
            obj.drops = new ItemDropBundle(drops, function(value) {
                var reward_list = consume_reward(value);
                return reward_list.first();
            });

            prototypes[monster_id] = obj;
            mmapi_log_info("mmapi", "monster prototype `" + entry_name
                + "` ready in category `" + descriptor.key + "` from "
                + descriptor.mod_name + " as object " + string(obj.gm_object));
        }
    }
    return prototypes;
}
