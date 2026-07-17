// mmapi_combat.gml. Combat runtime helpers.
//
// mmapi_deal_damage(target, amount, opts) injects a hit through the engine's
// own damage pipeline. It builds a real obj_damage_tarball via the engine's
// TarballBuilder, neutralises its collision (can_hurt = false), and enqueues
// it with the receiver's own damage() gate. A synthetic struct would not do:
// the drain's instance_exists check gates it out, and it lacks the tarball
// methods the drains call. Mitigation, popups, flinch, knockback, status
// effects, and the combat.damage / combat.damage_resolved hooks all run
// exactly as for an engine hit.
//
// Lifecycle: the tarball is built with a hit count of 1, so resolution
// (succesful_hit or blocked) destroys it. A destruction timer (opts.gc_frames,
// default 30) collects the drop-without-destroy paths: drain-time iframes,
// UntimedInvulnerable, grounded-vs-Aerial, drop_damage, and owners that never
// drain (a dead monster, the player past the health gate). A hit still queued
// when the timer fires is skipped by the combat.damage drain guard, the same
// way the engine treats any stale hit. The timer never ticks while the drain
// cannot run: tarball step, receiver step, and the drains all sit behind
// game_paused().

// Inject a hit. target is an obj_damage_receiver, or any owner exposing
// .receiver (the player obj_ari, every par_monster species). amount is
// positive pre-mitigation damage: player targets are mitigated by the engine
// and floored at 1, monster targets take it raw, and registered combat.damage
// and player.incoming_damage filters compose on top exactly as for engine
// hits. opts (a struct, all fields optional):
//   critical, heavy: popup damage flags.
//   instant_kill: the engine's own semantics (damage 999 plus the critical
//     popup flag), not a guaranteed kill for targets above 999 hit points.
//   flags: raw CombatFlag bits OR'd onto the tarball.
//   pierce_iframes: sugar for CombatFlag.Acid. Pierces the drain-time iframe
//     check only; enqueue-time iframes still reject the hit.
//   electrocute_kind: sets CombatFlag.Electric and the electrocute kind.
//   venomous, frozen, fire_oil: monster-side status effects.
//   source: instance; becomes the tarball's parent_id.
//   knockback: { force_min, force_max, radius }. Requires source, since the
//     engine's calculate_knockback dereferences parent_id; dropped with a
//     warning otherwise.
//   provenance, stats_entry: mines-run accounting fields.
//   show_popup, flinch: player targets only. false writes the tarball fields
//     the player.incoming_damage seam reads.
//   target_mask: override tarball.target. Defaults to the receiver's own mask
//     so the enqueue bitmask check always matches.
//   gc_frames: destruction-timer fallback, default 30.
// Returns the live tarball instance when the receiver accepted the hit, either
// queued or consumed by a DamageOnAttack damage-back, so callers can stamp
// per-attack fields on it. Returns undefined when rejected (target-mask miss,
// enqueue-time iframes, bad arguments, no receiver), and the tarball is
// destroyed on rejection.
//
// The hit resolves in the owner's next drain and flows through combat.damage
// and combat.damage_resolved like any engine hit. The tarball carries
// __mmapi_injected with the injecting mod's name, so filters can tell
// synthetic hits from engine hits.
function mmapi_deal_damage(target, amount, opts) {
    try {
        return __mmapi_deal_damage_impl(target, amount, opts);
    } catch (err) {
        mmapi_warn_rate_limited("deal_damage:" + mmapi_current_mod(), mmapi_current_mod(),
            "mmapi_deal_damage failed: " + string(err));
        return undefined;
    }
}

// The player convenience. Resolves the live obj_ari instance through the
// engine's own instance_find idiom. undefined when there is no player, e.g.
// the title screen.
function mmapi_deal_damage_player(amount, opts) {
    var player = instance_find(obj_ari, 0);
    if (player == undefined || instance_exists(player) == false) {
        mmapi_warn_rate_limited("deal_damage:no_player", mmapi_current_mod(),
            "mmapi_deal_damage_player: no obj_ari instance");
        return undefined;
    }
    return mmapi_deal_damage(player, amount, opts);
}

// The real work, as an ordinary function taking its arguments. It was once a
// zero-argument function fed by a reused global scratch struct, mirroring the
// __mmapi_guarded_call trampoline, on the claim that a try/catch only catches
// in a function invoked with zero arguments. That claim is false (see
// mmapi.gml), so the trampoline and the scratch are gone. The result is now a
// local, which also retires the old re-entrancy rule: a handler re-entering
// mmapi_deal_damage would reset a shared scratch, but it cannot touch a local.
//
// A malformed target (no .receiver field, so a throw on the dot read) is caught
// by the public wrapper above, which warns rate-limited and returns undefined.
function __mmapi_deal_damage_impl(target, amount, opts) {
    if (opts == undefined) { opts = {}; }

    if (target == undefined || instance_exists(target) == false) { return undefined; }
    if (opts[$ "instant_kill"] != true
            && (is_real(amount) == false || amount <= 0)) { return undefined; }

    // The target is a receiver, or it owns one: obj_ari and every par_monster
    // species expose .receiver.
    var receiver = target;
    if (target.object_index != obj_damage_receiver) {
        receiver = target.receiver; // throws on a non-owner: caught by the wrapper
    }
    if (receiver == undefined || instance_exists(receiver) == false
            || receiver.object_index != obj_damage_receiver) { return undefined; }

    // A real tarball via the engine's own builder. target defaults to the
    // receiver's own mask, so the enqueue AND-check always matches.
    var mask = opts[$ "target_mask"];
    if (mask == undefined) { mask = receiver.target; }
    var gc_frames = opts[$ "gc_frames"];
    if (gc_frames == undefined) { gc_frames = 30; }

    var builder = TarballBuilder(receiver.x, receiver.y, 0, 0, amount, mask);
    builder.set_can_destroy_grid_objects(false);
    builder.set_hit_count(1);     // succesful_hit/blocked destroy it
    builder.set_timer(gc_frames); // GC for the drop-without-destroy paths
    if (opts[$ "critical"] == true) { builder.set_critical(true); }
    if (opts[$ "heavy"] == true) { builder.set_heavy(true); }
    if (opts[$ "instant_kill"] == true) { builder.set_instant_kill(true); }
    if (opts[$ "flags"] != undefined) { builder.flags |= opts.flags; }
    if (opts[$ "pierce_iframes"] == true) { builder.flags |= CombatFlag.Acid; }
    if (opts[$ "electrocute_kind"] != undefined) { builder.set_electric(opts.electrocute_kind); }
    if (opts[$ "venomous"] == true) { builder.set_venomous(true); }
    if (opts[$ "frozen"] == true) { builder.set_frozen(true); }
    if (opts[$ "fire_oil"] == true) { builder.set_fire_oil(true); }
    if (opts[$ "source"] != undefined) { builder.set_parent(opts.source); }
    if (opts[$ "knockback"] != undefined) {
        if (opts[$ "source"] == undefined) {
            // calculate_knockback dereferences parent_id, so knockback with no
            // source would throw at resolution time.
            mmapi_warn_rate_limited("deal_damage:knockback", mmapi_current_mod(),
                "mmapi_deal_damage: knockback requires opts.source; dropped");
        } else {
            var kb = opts.knockback;
            builder.set_knockback(kb.force_min, kb.force_max, kb.radius);
        }
    }
    if (opts[$ "provenance"] != undefined || opts[$ "stats_entry"] != undefined) {
        builder.set_provenance(opts[$ "provenance"], opts[$ "stats_entry"]);
    }

    var mod_name = mmapi_current_mod();
    var tarball = builder.gen();
    tarball.can_hurt = false; // payload carrier: never collides, no grid ops
    tarball.__mmapi_injected = mod_name;
    // Player-side presentation opts ride the tarball fields the installed
    // player.incoming_damage seam already reads. Presence is gated before the
    // false test, because an absent field compares == false as true.
    var show_popup = opts[$ "show_popup"];
    var flinch = opts[$ "flinch"];
    if (show_popup != undefined && show_popup == false) { tarball.__mmapi_player_show_damage_popup = false; }
    if (flinch != undefined && flinch == false) { tarball.__mmapi_player_should_flinch = false; }

    // The engine's own enqueue gate: target mask, enqueue-time iframes,
    // DamageOnAttack damage-back. == undefined is checked first, as cheap
    // defensiveness rather than necessity (see quirk 2 in mmapi.gml).
    var accepted = receiver.damage(tarball);
    if (accepted == undefined || accepted == false) {
        instance_destroy(tarball);
        return undefined;
    }
    mmapi_emit("combat.damage_injected",
        { receiver: receiver, tarball: tarball, mod_name: mod_name });
    return tarball;
}
