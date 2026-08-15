# Custom Spells

[← MMAPI](MMAPI.md)

A custom spell needs no [extension point](EXTENSIONS.md). The engine derives the spell roster from `fiddle/spells.toml` at boot, so a spell is three mod-side pieces: a data entry, a call that teaches it, and two hook handlers that give it behavior.

## How Spells Resolve

`Spell` is a native roster. The engine rebuilds it alphabetically from the installed key set at every boot, so adding or removing any spell mod reshuffles every ordinal.

> [!WARNING]
> Resolve your spell by name at call time with `string_to_spell("your_key")`, and never store the ordinal anywhere, including your modsave. The save records learned spells by name for the same reason.

## The Data

Merge a key into `fiddle/spells.toml`, modeled on a vanilla entry. Sprite keys may reuse vanilla assets. The spell menu lists the spell automatically once it is learned.

```toml
[my_mod_spell]
	name = "Mirror Image"
	description = "Reflect on your choices."
	cost = 20
	icon_key = "spr_ui_spell_icon_summon_rain"
```

Copy a vanilla entry and keep every field it carries. The engine reads optional fields wherever they are present, so the copy is the contract.

## Learning the Spell

Call `ARI.learn_spell(string_to_spell("my_mod_spell"))` from your GML. The call is idempotent, so a guarded once-per-session call inside your tick is enough:

```gml
function my_mod_tick() {
    var _rt = __my_mod_runtime();
    if (_rt.taught == undefined) {
        _rt.taught = true;
        ARI.learn_spell(string_to_spell("my_mod_spell"));
    }
}
```

Gate the call behind a quest flag, an item, or a purchase when the spell should be earned rather than granted.

## The Behavior

Register [spells.can_cast](hooks/spells.can_cast.md) and [spells.cast](hooks/spells.cast.md) override handlers. Both must fully replace their result for your spell. Returning `undefined` defers to the engine's checks, whose default arm is fatal for a spell it does not know. That also means your `can_cast` replaces the mana gate, so replicate it:

```gml
function my_mod_can_cast(ctx) {
    if (ctx != string_to_spell("my_mod_spell")) { return undefined; }
    return ARI.get_mana() >= SPELLS[ctx].cost;   // the override replaces EVERYTHING, mana gate included
}

function my_mod_cast(ctx) {
    if (ctx != string_to_spell("my_mod_spell")) { return undefined; }
    // this function IS the spell's effect - anything GML can do goes here
    create_notification("misc_local/known_recipe");
    return true;                                  // consume the cast (mana is still deducted)
}
```

Both hooks join `requires_hooks` in your manifest. [spells.cost](hooks/spells.cost.md) adjusts the mana cost everywhere the engine reads it. [spells.cast_done](hooks/spells.cast_done.md) fires after any completed cast, yours included.

A cast handler that applies a [custom status effect](CUSTOM_STATUS_EFFECTS.md) is a natural pairing. The two systems compose without any extra wiring.

## Saves and Uninstalling

The save records learned spells by name, so reordering ordinals never corrupts a save. Vanilla's load side resolves those names with the fatal `string_to_spell`, unlike perks and items beside it, which use tolerant `try_` variants. On a clean game, a save that learned a since-removed custom spell fails silently, bouncing to the title with no error anywhere. Under MOMI the [save_load_spells_tolerance](seams/save_load_spells_tolerance.md) engine fix, with its [pinned-spell companion](seams/save_load_pinned_spell_tolerance.md), forgets the unknown spell with a [logged warn](seams/save_load_forget_warn.md) and the load continues. The forgetting becomes permanent when the player next saves. Reinstalling the mod before that restores the spell untouched.
