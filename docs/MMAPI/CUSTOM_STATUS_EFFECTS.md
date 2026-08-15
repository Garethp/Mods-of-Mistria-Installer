# Custom Status Effects

[← MMAPI](MMAPI.md)

A custom status effect uses the [status_effect](extensions/status_effect.md) extension point, the simplest one MOMI ships. One registration file mints the effect's identity, and everything else happens from your own GML: applying it, giving it a HUD icon, and reacting when it ends.

## The Registration

`momi/extensions/status_effect/<name>.toml` with no fields. An empty or comment-only file registers the id:

```toml
# momi/extensions/status_effect/well_rested.toml
# One registration = one custom status effect.
```

MOMI generates a `StatusEffectId` member for the derived symbol, `<mod symbol>_<name>` (see [The Install Namespace](MANIFEST.md#the-install-namespace)). For author `you` and mod `my_mod`, the file `well_rested.toml` derives `you_my_mod_well_rested`. Unlike an NPC's content files, a status effect is driven from GML, and GML is code, so you write the full symbol there through the registry helper:

```gml
mmapi_ext_id("status_effect", "you_my_mod_well_rested")
```

The ordinal it returns is ledger-stabilised and never changes, but resolve it through the helper rather than storing it, matching the convention everywhere else.

## Applying the Effect

The engine's status manager is id-agnostic. Apply your effect with `register`, using timestamps from `CALENDAR.unified_time()`. One in-game hour is 3600 units:

```gml
var _time = CALENDAR.unified_time();
ARI.status_effects.register(
    mmapi_ext_id("status_effect", "you_my_mod_well_rested"),
    1,                  // amount, readable back through get_effect_value
    _time,              // start
    _time + 3600 * 4    // finish: four in-game hours
);
```

The full signature is `register(type, amount, start, finish, can_stack, show_hud)`, where `can_stack` defaults to `false` and `show_hud` defaults to `true`. See [player.status_effect_register](hooks/player.status_effect_register.md).

## A Complete Example

A morning buff that applies at the start of every day:

```gml
// Well Rested

function __my_mod_runtime() {
    if (global[$ "__my_mod"] == undefined) {
        global.__my_mod = { registered_hooks: undefined };
    }
    return global.__my_mod;
}

function my_mod_shroud_id() {
    return mmapi_ext_id("status_effect", "you_my_mod_well_rested");
}

function my_mod_day_changed(_ctx) {
    var _time = CALENDAR.unified_time();
    ARI.status_effects.register(my_mod_shroud_id(), 1, _time, _time + 3600 * 4);
}

function my_mod_shroud_icon(_value, _type) {
    if (_type != my_mod_shroud_id()) { return undefined; }
    return { icon_sprite: spr_ui_hud_info_essence_icon };
}

function my_mod_register_callbacks() {
    var _rt = __my_mod_runtime();
    if (_rt.registered_hooks != undefined) return;
    _rt.registered_hooks = true;

    mmapi_on("game.day_changed", my_mod_day_changed);
    mmapi_filter("status_effect.hud_icon", my_mod_shroud_icon);
}

mmapi_mod_declare("my_mod", "1.0.0");
my_mod_register_callbacks();
```

The manifest lists both hooks: `"requires_hooks": ["game.day_changed", "status_effect.hud_icon"]`. What the buff actually does is up to the rest of your mod. Any handler can poll `ARI.status_effects.get_effect_value(my_mod_shroud_id(), 0)` and act while it returns a nonzero value.

## The HUD Icon

Without a [status_effect.hud_icon](hooks/status_effect.hud_icon.md) handler the effect works but draws no icon. The handler returns `{ icon_sprite, color }`, where `color` is optional and defaults to the vanilla status orange. The example above reuses a vanilla sprite. To ship your own, add a meta and png pair under `animations/` and reference the sprite by name, the same [art lane](EXTENSIONS.md#shipping-your-npcs-art) custom NPCs use.

## Reacting to the End

[player.status_effect_expired](hooks/player.status_effect_expired.md) fires when an effect's finish time passes, and [player.status_effect_cancel](hooks/player.status_effect_cancel.md) fires when something removes it early. Both receive the `StatusEffectId` ordinal in ctx, so compare against your id the same way the icon handler does.

## Saves and Uninstalling

Active effects persist across save and load. The manager serializes inside the player blob as a slot array whose entries carry their type by name, and an effect mid-duration survives a full quit and relaunch.

On removal, the ledger's tombstone keeps the type name resolving through the vacant enum member, so a saved effect ticks out inert with no handlers. For saves that land on an install without that ledger history, the [save_load_status_effect_tolerance](seams/save_load_status_effect_tolerance.md) engine fix drops the unresolvable slot with a warn instead of letting vanilla's fatal lookup abort the load. Only a clean game without MOMI still refuses such a save.
