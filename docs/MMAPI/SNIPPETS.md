# Snippets

[← MMAPI](MMAPI.md)

Most mod needs are direct engine calls, not hooks. Hooks change what the engine does on its own. To make the engine do something, call it. Most snippets below are a plain engine call you run from a hook handler or a registered tick. Some depend on fiddle data as well, and the last two sections are world-fact clauses and hook observers rather than calls.

> [!CAUTION]
> Never run these at top-level boot. Boot runs while the game is still loading, when there is no player, no room, and file IO throws. Call them from a handler or a `mmapi_register` tick. See [Mod Anatomy](MOD_ANATOMY.md#the-lifecycle).

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

`create_notification` takes a **localization key**, resolved engine-side through `local_get`. The recommended pattern registers your string and passes the derived key. See the full mechanism in [User-Facing Text](MOD_ANATOMY.md#user-facing-text-localization):

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

The optional second argument suppresses repeats of the same key for that many frames (`60 * 5` ≈ five seconds). Call it once per real event, not every frame.

Because the key resolves inside the [local_get_dispatch](seams/local_get_dispatch.md) rewrite, [local.get](hooks/local.get.md) filters can substitute dynamic tokens into the text at display time. Pass the key, never pre-localized text, or the filters never see it.

For throwaway prototypes only, `create_notification(ANCHOR.wrap_for_local("raw text"))` shows unregistered text, which is untranslatable and invisible to filters.

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

## Weather Conditions (World Fact)

The `weather` world fact is written at day start and holds `"pleasant"`, `"rainy"`, or `"snowy"`. Severity is a separate fact, `weather_is_strong`. A storm is `"rainy"` plus strong, and a blizzard is `"snowy"` plus strong.

### Matching Any Bad Weather

`Inclement` weather writes `"rainy"` in Spring, Summer, and Fall and `"snowy"` in Winter. As a result, a rain-only condition won't match in Winter. Match on both `"rainy"` and `"snowy"`:

```toml
requires = [
   { any = [ { weather = "rainy" }, { weather = "snowy" } ] },
]
```

### Matching Severe Weather Only

For severe weather, meaning storms and blizzards, add an additional condition on `weather_is_strong`:

```toml
requires = [
   { any = [ { weather = "rainy" }, { weather = "snowy" } ] },
   { weather_is_strong = true },
]
```

### Matching Rain Only

This example includes storms, and never matches in Winter:

```toml
requires = [
   { weather = "rainy" },
]
```

## Tracing Dialogue

To see exactly which conversation and lines the engine serves, and which prompt a player picked, register observers on the shipped hooks. No engine edits or new seams are needed:

```gml
function my_mod_trace_play(ctx) {
    mmapi_log_warn("my_mod", "convo start: " + string(ctx[$ "path"]));
    return undefined; // observe only, never veto
}
function my_mod_trace_line(value, ctx) {
    mmapi_log_warn("my_mod", "line: " + string(value)); // banked lines arrive as the line PATH
    return undefined; // observe only, never rewrite
}
mmapi_guard("dialogue.play_guard", my_mod_trace_play);
mmapi_filter("dialogue.line", my_mod_trace_line);
```

`T2R.request_conversation(npc_id)` returns the currently pending conversation for an NPC and is side-effect-free. Poll it, for example from an `mmapi_hotkey_register` dump, to watch selection react to world-fact changes live. Add both hook names to `requires_hooks`.
