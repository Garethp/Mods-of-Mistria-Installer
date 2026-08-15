# Custom Perks

[← MMAPI](MMAPI.md)

A custom perk needs no [extension point](EXTENSIONS.md), and it is architecturally the simplest custom content, because a perk has no central logic anywhere. It is inert data whose effect exists wherever code asks about it, which is exactly how vanilla works. Engine sites each query `perk_active`, and your mod does the same.

`Perk` is a native roster rebuilt alphabetically at boot, so the same rule applies as for [spells](CUSTOM_SPELLS.md): resolve your perk by name with `string_to_perk("your_key")` at call time, and never store the ordinal.

## The Data

Merge a key into `fiddle/perks.toml`:

```toml
[my_mod_keen_eye]
	name = "Keen Eye"
	description = "Gifts you give warm hearts by {value} more."
	value = 5
```

`value` is the conventional home for the perk's headline number. It is the field `ARI.perk_value` reads by default, and the skills menu substitutes `{value}` in the description with it. A perk with no number, one that is simply on or off, can omit it.

## Granting

Call `ARI.acquire_perk(string_to_perk("my_mod_keen_eye"))`, the same call the engine makes when essence is spent. Acquisition is recorded by name in the stats, so it survives the alphabetical reshuffle.

## The Effect

A perk does nothing until code checks for it. Register a hook handler that queries `perk_active` and acts. This example warms every heart-point gain while the perk is held:

```gml
function my_mod_keen_eye_hearts(_value, _ctx) {
    if (!ARI.perk_active(string_to_perk("my_mod_keen_eye"))) { return undefined; }
    return _value + 5;
}

// inside your registration latch:
mmapi_filter("npc.heart_points", my_mod_keen_eye_hearts);
```

The hook joins `requires_hooks` in your manifest. Any hook can carry a perk effect. A combat filter makes a combat perk, a crafting filter makes a crafting perk, and the perk itself never knows.

## Making It Purchasable

The skills menu is separate from the perk roster. It renders purchase trees from `fiddle/ui/skill_menu/<skill>.toml`, so a programmatically granted perk works fully without appearing there. To sell your perk at the Dragon Shrine, ship a skill file containing just your tier entry:

```toml
# fiddle/ui/skill_menu/combat.toml
[[tier_1]]
	perk = "my_mod_keen_eye"
	essence = 25
	icon = "spr_ui_skills_combat_icon_true_strike"
```

MOMI's TOML merge appends table-array entries, so the vanilla tiers keep their entries and yours joins them. The row layout adapts to the extra tile. The `icon` may be any installed named sprite, including one your mod ships through the [art lane](EXTENSIONS.md#shipping-your-npcs-art). The icon resolves through `string_to_asset` at boot, so a typo'd name is a boot crash. `MOMIidentify` and `MOMIaction` remain available when you need to edit a vanilla entry rather than add one.

Purchasing through the menu calls the same `acquire_perk` as your code would, and the two grant paths coexist safely.

## Tunable Numbers

Numbers your effect uses can live in the perk's own fiddle entry, which is how the engine tunes its own perks. `ARI.perk_value(perk)` reads the `value` field, and it returns `0` whenever the perk is not held, so a numeric effect can use it as both the gate and the magnitude:

```gml
function my_mod_keen_eye_hearts(_value, _ctx) {
    var _bonus = ARI.perk_value(string_to_perk("my_mod_keen_eye"));
    if (_bonus == 0) { return undefined; }
    return _value + _bonus;
}
```

Rebalancing the perk is then a data edit, with no code change, and the `{value}` placeholder keeps the description in step. A perk that needs more than one number can carry extra fields and read them by name, since `ARI.perk_value(perk, "field")` accepts any field the entry declares.

## Saves and Uninstalling

Perks are uninstall-tolerant everywhere. Both the perk roster and the stats acquisition record load through vanilla's own `try_string_to_perk`, so any load simply forgets an unknown perk, even on a clean game without MOMI. Under MOMI the drop is [named in the log](seams/save_load_forget_warn.md). A perk-only mod strands no save anywhere.
