// mmapi_npc_vacant.gml. The vacant-NPC stub object for the npc_roster
// extension point.
//
// A ledger vacancy keeps its enum member forever (saves reference NPCs by
// name, and native string_to_npc_id throws on a name that stops resolving),
// so the id->object switch still needs somewhere to point when the mod that
// owned the symbol is uninstalled. This object is that somewhere. It is never
// instantiated by the framework - a vacant NPC's schedule parks it in the
// engine's holding pen, and the journal hides vacants - it exists so the
// generated `case NpcId.<symbol>: return obj_mmapi_npc_vacant;` resolves at
// compile time.
//
// The macro mirrors object_manifest.gml's convention: identifiers bind to
// runtime by-name lookups. It lives here rather than in a generated file
// because the identifier must resolve on EVERY modded install - the vacancy
// case only renders when a vacancy exists, but engine code referencing the
// identifier compiles as a unit.
#macro obj_mmapi_npc_vacant object("obj_mmapi_npc_vacant")

// Guarded like every other engine-only boot call in the payload: the compat
// dialect late-binds names, and the framework's off-engine contract is that
// no game-only name is EXECUTED at top level (an off-engine test VM has no
// object layer). In-engine the catch never fires and the object is created
// exactly as before; off-engine nothing downstream needs the object.
try {
    object_create(
        "obj_mmapi_npc_vacant",
        object_reserve("par_NPC"),
        {
            sprite_index: spr_npc_mask,
        }
    );
} catch (__mmapi_npc_vacant_env) {
    // off-engine: no object layer to register the vacant stub with
}
