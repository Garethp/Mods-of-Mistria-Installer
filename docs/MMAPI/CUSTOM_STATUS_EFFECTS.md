# Custom Status Effects

[← MMAPI](MMAPI.md)

A custom status effect uses the [status_effect](extensions/status_effect.md) extension point. One registration file mints the effect's identity, and everything else happens from your own GML. Your code applies it, gives it a HUD icon, and reacts when it ends.

## The Registration

`momi/extensions/status_effect/<name>.toml` with no fields. An empty or comment-only file registers the id:

```toml
# momi/extensions/status_effect/exhaustion.toml
# One registration = one custom status effect.
```

MOMI generates a `StatusEffectId` member for the derived symbol, as `<mod symbol>_<name>` (see [The Install Namespace](MANIFEST.md#the-install-namespace)). For author `you` and mod `my_mod`, the file `exhaustion.toml` derives `you_my_mod_exhaustion`. A status effect is driven from GML, and GML is code, so you write the full symbol there through the registry helper:

```gml
mmapi_ext_id("status_effect", "you_my_mod_exhaustion")
```

The ledger keeps that ordinal fixed, but resolve it through the helper rather than storing it, as the extension [rules](EXTENSIONS.md#rules) state.

## Applying the Effect

The engine's status manager is id-agnostic. Apply your effect with `register`, using timestamps from `CALENDAR.unified_time()`, which counts in-game seconds, so one in-game hour is 3600:

```gml
var _time = CALENDAR.unified_time();
ARI.status_effects.register(
    mmapi_ext_id("status_effect", "you_my_mod_exhaustion"),
    1,                  // amount, readable back through get_effect_value
    _time,              // start
    _time + 3600 * 4    // finish: four in-game hours
);
```

The full signature is `register(type, amount, start, finish, can_stack, show_hud)`, where `can_stack` defaults to `false` and `show_hud` defaults to `true`. See [player.status_effect_register](hooks/player.status_effect_register.md).

## A Complete Example

A debuff that halves movement speed while stamina is empty. The speed filter is the one place that sees the player move, so it both keeps the effect in sync with stamina and applies the slow:

```gml
// Exhaustion example

function __my_mod_runtime() {
    if (global[$ "__my_mod"] == undefined) {
        global.__my_mod = { registered_hooks: undefined };
    }
    return global.__my_mod;
}

function my_mod_exhaustion_id() {
    return mmapi_ext_id("status_effect", "you_my_mod_exhaustion");
}

function my_mod_exhaustion_active() {
    return ARI.status_effects.get_effect_value(my_mod_exhaustion_id(), 0) != 0;
}

function my_mod_exhaustion_speed(_value, _ctx) {
    if (_value == undefined) { return undefined; }
    var _exhausted = ARI.get_stamina() <= 0;
    if (_exhausted && !my_mod_exhaustion_active()) {
        var _time = CALENDAR.unified_time();
        ARI.status_effects.register(my_mod_exhaustion_id(), 1, _time, _time + 3600 * 24);
    } else if (!_exhausted && my_mod_exhaustion_active()) {
        ARI.status_effects.cancel(my_mod_exhaustion_id());
    }
    if (!_exhausted || _ctx.on_mount) { return undefined; }
    return _value * 0.5;
}

function my_mod_exhaustion_icon(_value, _type) {
    if (_type != my_mod_exhaustion_id()) { return undefined; }
    return { icon_sprite: spr_ui_hud_info_essence_icon };
}

function my_mod_register_callbacks() {
    var _rt = __my_mod_runtime();
    if (_rt.registered_hooks != undefined) return;
    _rt.registered_hooks = true;

    mmapi_filter("player.move_speed", my_mod_exhaustion_speed);
    mmapi_filter("status_effect.hud_icon", my_mod_exhaustion_icon);
}

mmapi_mod_declare("my_mod", "1.0.0");
my_mod_register_callbacks();
```

The manifest's `requires_hooks` lists both, `player.move_speed` and `status_effect.hud_icon`. The effect lifts the next time the player moves after stamina recovers, and the new-day routine clears it with every other status effect. Any handler can poll `ARI.status_effects.get_effect_value(my_mod_exhaustion_id(), 0)` the same way and act while it returns a nonzero value.

## The HUD Icon

Without a [status_effect.hud_icon](hooks/status_effect.hud_icon.md) handler the effect works but draws no icon. The handler returns `{ icon_sprite, color }`, where `color` is optional and defaults to the vanilla status orange. The example above reuses a vanilla sprite. To ship your own, add a meta and png pair under `animations/` and reference the sprite by name, as a [named sprite](EXTENSIONS.md#named-sprites).

## Reacting to Expiration

[player.status_effect_expired](hooks/player.status_effect_expired.md) fires when an effect's finish time passes, and [player.status_effect_cancel](hooks/player.status_effect_cancel.md) fires when something removes it early. Both receive the `StatusEffectId` ordinal in ctx, so compare against your id the same way the icon handler does.

## Saves and Uninstalling

Active effects persist across save and load. The manager serializes inside the player blob as a slot array whose entries carry their type by name, and an effect mid-duration survives a full quit and relaunch.

On removal, the ledger's tombstone keeps the type name resolving through the vacant enum member, so a saved effect ticks out inert with no handlers. For saves that land on an install without that ledger history, the [save_load_status_effect_tolerance](seams/save_load_status_effect_tolerance.md) engine fix drops the unresolvable slot with a warn instead of letting vanilla's fatal lookup abort the load. Only a vanilla game without MMAPI still refuses to load such a save.
