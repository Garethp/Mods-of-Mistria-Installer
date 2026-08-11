// mmapi_npc.gml. Registry for custom NPC ids (e.g. "example_mod:mayor_greta")
// resolved at the native call sites the npc_custom_* engine fixes patch, so a
// mod's NPC never needs a real NpcId enum member. One dedicated object index
// per id, same as every vanilla NPC.
//
// Call at top-level boot, NOT from an mmapi_register(...) installer: quest
// TOMLs parse before the Game object exists, so a deferred registration
// misses npc_custom_quest_query and can crash the game at boot. This call
// has no player/room/file-IO dependency, so top-level boot is safe here.
//
// Rejects an id colliding with a real NpcId/CameoId name, and an
// object_index that doesn't exist or falls in NpcId's own integer range
// (that range is compared by == against real NpcId values elsewhere, e.g.
// par_NPC.gml's my_query_quests).

function mmapi_register_custom_npc_id(id, object_index) {
    var mod_name = mmapi_current_mod();

    if (try_string_to_npc_id(id) != undefined || try_string_to_cameo_id(id) != undefined) {
        mmapi_warn_rate_limited(
            "custom_npc_id_reserved:" + string(id),
            mod_name,
            "custom NPC id `" + string(id) + "` collides with a real vanilla NPC or Cameo name - rejected");
        return false;
    }

    if (!object_exists(object_index) || (object_index >= 0 && object_index < NpcId.LEN)) {
        mmapi_warn_rate_limited(
            "custom_npc_id_bad_object:" + string(id),
            mod_name,
            "custom NPC id `" + string(id) + "` was registered with an invalid object_index - rejected");
        return false;
    }

    if (global[$ "__mmapi_custom_npc_registry"] == undefined) { global.__mmapi_custom_npc_registry = {}; }
    var existing = global.__mmapi_custom_npc_registry[$ id];
    if (existing != undefined && existing.mod_name != mod_name) {
        mmapi_warn_rate_limited(
            "custom_npc_id:" + string(id),
            mod_name,
            "custom NPC id `" + string(id) + "` is already registered by `"
            + string(existing.mod_name) + "` - skipping registration from `" + string(mod_name) + "`");
        return false;
    }

    global.__mmapi_custom_npc_registry[$ id] = { object_index: object_index, mod_name: mod_name };
    return true;
}

function __mmapi_custom_npc_object_for(name) {
    if (global[$ "__mmapi_custom_npc_registry"] == undefined) { return undefined; }
    if (try_string_to_npc_id(name) != undefined || try_string_to_cameo_id(name) != undefined) {
        return undefined; // never shadow a real actor, regardless of caller check order
    }
    var entry = global.__mmapi_custom_npc_registry[$ name];
    if (entry == undefined) { return undefined; }
    return entry.object_index;
}
