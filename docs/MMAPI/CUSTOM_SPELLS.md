# Custom Spells

[← MMAPI](MMAPI.md)

A custom spell needs no [extension point](EXTENSIONS.md). The engine derives the spell roster from `fiddle/spells.toml` at boot, so a spell is three mod-side pieces. A data entry defines it, a call teaches it, and two hook handlers give it behavior.

`Spell` is a native roster rebuilt alphabetically from the installed key set at every boot, so adding or removing any spell mod reshuffles every ordinal. Resolve your spell by name with `string_to_spell("your_key")` at call time, and never store the ordinal anywhere, including your modsave.

## The Data

Merge a key into `fiddle/spells.toml`, modeled on a vanilla entry. Sprite keys may reuse vanilla assets. The spell menu lists the spell automatically once it is learned.

```toml
[my_mod_spell]
	name = "Mirror Image"
	description = "Reflect on your choices."
	cost = 20
	icon_key = "spr_ui_journal_magic_rain_spell_icon"
```

Copy a vanilla entry and keep every field it carries. The vanilla entries are the only schema, and the engine reads optional fields wherever they are present.

## Learning the Spell

Call `ARI.learn_spell(string_to_spell("my_mod_spell"))` from your mod GML. The call is idempotent, so a guarded once-per-session call inside your tick is enough:

```gml
function my_mod_tick() {
    var _rt = __my_mod_runtime();
    if (_rt.taught == undefined) {
        _rt.taught = true;
        ARI.learn_spell(string_to_spell("my_mod_spell"));
    }
}
```

Gate the call behind a quest flag, an item being used, or a purchase when the spell should be earned rather than granted unconditionally.

## The Behavior

Register [spells.can_cast](hooks/spells.can_cast.md) and [spells.cast](hooks/spells.cast.md) override handlers. Your `can_cast` therefore replaces the mana gate too, so replicate it:

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

> [!IMPORTANT]
> Both handlers must fully replace their result for your spell. Returning `undefined` defers to the engine's own checks, and their default arm is fatal for a spell it does not know.

Both hooks join `requires_hooks` in your manifest. [spells.cost](hooks/spells.cost.md) adjusts the mana cost everywhere the engine reads it. [spells.cast_done](hooks/spells.cast_done.md) fires after any completed cast, yours included.

A cast handler that applies a [custom status effect](CUSTOM_STATUS_EFFECTS.md) is a natural pairing. The handler calls the effect's `register` like any other GML, so nothing else connects the two.

## Saves and Uninstalling

Spells are uninstall-tolerant. The save records learned spells by name, so reordering ordinals never corrupts a save. The load resolves those names through the engine's own `try_string_to_spell`, so a save that learned a since-removed custom spell loads on a vanilla game as well as on an MMAPI install. The load simply forgets the unknown spell, unpins it if it was pinned, and names it in the log. The forgetting becomes permanent when the player next saves, and reinstalling the mod before that restores the spell untouched.
