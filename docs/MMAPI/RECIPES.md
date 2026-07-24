# Recipes

[← MMAPI](MMAPI.md)

Most mod needs are direct engine calls, not hooks. Hooks change what the engine does on its own; to make the engine do something, call it. Each recipe below is a plain engine call you run from a hook handler or a registered tick.

> [!CAUTION]
> Never run these at top-level boot. Boot runs while the game is still loading: no player, no room, and file IO throws. Call them from a handler or a `mmapi_register` tick. See [Mod Anatomy](MOD_ANATOMY.md#the-lifecycle).

> [!WARNING]
> These call the engine directly, so where the path is seamed another mod can legitimately veto or reshape what you asked for. Write defensive code. 

For the helper functions (config, logging, hotkeys, per-save data), see the [API Reference](API_REFERENCE.md). When you need to change or observe what the engine does on its own instead, register a [hook](HOOKS.md). For the full list of hooks, see the [Catalog](CATALOG.md).

## Give the Player an Item

```gml
var _id = try_string_to_item_id("wild_berry");
if (_id != undefined) { ARI.give_item(_id, 1); }
```

Unknown item names return `undefined`, so check before giving.

## Give or Take Gold

```gml
ARI.modify_gold(500);   // a negative amount takes gold
```

## Show a Notification

`create_notification` takes a **localization key**, resolved engine-side through `local_get`. The shipped-mod pattern registers your string and passes the derived key. See the full mechanism in [User-Facing Text](MOD_ANATOMY.md#user-facing-text-localization):

```toml
# fiddle/mods/my_mod/notifications.toml
something_happened = "Something happened!"
```

```toml
# localization/l10n.meta.toml: Flat per-file entries ONLY (the umbrella
# directory form crashes the engine at boot)
[asset_properties]
	[asset_properties.fiddle_renames]
		"mods/my_mod/notifications" = ["*"]
```

```gml
create_notification("mods/my_mod/notifications/something_happened", 60 * 5);
```

The optional second argument suppresses repeats of the same key for that many frames (`60 * 5` ≈ five seconds). Once per real event, not every frame.

Because the key resolves inside the [local_get_dispatch](seams/local_get_dispatch.md) rewrite, [local.get](hooks/local.get.md) filters can substitute dynamic tokens into the text at display time. Pass the key, never pre-localized text, or the filters never see it.

For throwaway prototypes only: `create_notification(ANCHOR.wrap_for_local("raw text"))` shows unregistered text, it is untranslatable and invisible to filters.

## Teleport the Player

```gml
if (instance_exists(obj_ari)) {
    ari_teleport_to_room("town", 1097, 1323);   // room name, then pixel coordinates
}
```

## Play a Sound

```gml
var _name = "SoundEffects/Objects/Explosion";
if (TANGO.name_exists(_name)) { TANGO.play(_name); }
```

Missing names are silent, so check first. Every `TANGO.play` still runs the `audio.play_guard` hook, so another mod can veto it.

## Read Your Config

Load lazily and validate. See [The House Pattern](API_REFERENCE.md#the-house-pattern) for the full form.
