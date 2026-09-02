# Treasure Chests

[← MMAPI](MMAPI.md)

A mod can add custom treasure chests with their own loot tables using only fiddle data. No GML, no hooks, and no handlers are involved. The engine implements these as its fishing-chest pipeline, which is why the field and file names below say fish even for a chest that is never fished. That pipeline reads everything from fiddle data, and three catalog [engine fixes](seams/fish_chest_item_use.md) together provide support for custom entries.

A custom treasure chest item is three linked pieces of data. The item declares `fish_chest`, the named chest table declares the loot, and optional fish entries make the chest catchable while fishing.

## The `fish_chest` Item Field

Declare the `fish_chest` property on an item's fiddle entry. Its value names an entry under `fishing/chest_tables`.

```toml
# fiddle/items/my_chests.toml
[my_mod_custom_chest]
	name = "mods/my_mod/items/custom_chest_name"
	description = "mods/my_mod/items/custom_chest_description"
	icon_sprite = "spr_ui_item_wooden_chest"
	value = { store = 1000 }
	fish_chest = "my_mod_custom_chest"
```

`name` and `description` are localization keys, so register the strings with the pattern in [User-Facing Text](MOD_ANATOMY.md#user-facing-text-localization). A literal string still installs but displays raw.

> [!NOTE]
> The item automatically gains the `ItemUse.OpenChest` interaction and inherits the whole vanilla chest implementation. It opens with the long hold-to-use, shows the open interaction prompt, is not giftable, and does not stack.

## The Chest Table

```toml
# fiddle/fishing.toml
[chest_tables.my_mod_custom_chest]
	gold = [0, 0]
	items = [
		{ value = "chocolate", kind = "item" },
		{ value = "copper_ingot", kind = "item", count = 2 },
		{ value = "fish_stew", kind = "recipe" },
	]
```

- `gold` is **mandatory** and is a two-element range rolled on open. `gold = [0, 0]` is the no-payout idiom, use it to award zero gold.
- `items` is **mandatory**. One entry is rolled per open, so an entry's chance is its share of the list. Weight individual items by repeating entries, exactly as the vanilla tables do.
- `count` is a per-drop quantity, not a weight. It defaults to 1.
- `kind = "item"` drops the item with no duplicate protection. `kind = "recipe"` drops the recipe scroll for `value` and rerolls while the player already knows it, and `kind = "cosmetic"` behaves the same for obtained cosmetics.

> [!NOTE]
> A chest that is only ever bought, awarded to the player, or dropped needs nothing more than the two pieces above. The vanilla Abyssal Chest works exactly this way.

## Making It Fishable

Add fish entries and a votes entry. The runtime derives fish ids from the merged fiddle `fish` keys, so the entries below are complete.

```toml
# fiddle/fish.toml
[my_mod_custom_chest_fish]
	item = "my_mod_custom_chest"
	seasons = ["spring", "summer", "fall", "winter"]
	water_type = ["river", "pond", "ocean"]
	size = "medium"
	rarity = "my_mod_treasure_chest"
	is_chest = true
	retrieval = ["fishing", "divespot"]
```

```toml
# fiddle/fishing.toml
[votes]
	my_mod_treasure_chest = 5
```

- The **rarity** name is a free-form string with no required format. While not strictly mandatory, you should define your own rarity to avoid the game's `common` default.
- Every fish rarity must have its own `fishing/votes` entry, which is its base spawn weight (the game's vanilla chest tiers vary from 7 down to 4). Set this depending on how rare you want your custom treasure chest to be while fishing, with lower numbers being more rare.
- Vanilla ships each chest as a medium and a large fish entry pair so it appears at two water depths. Copy that shape when you want the same coverage.
- Fish traps are outside this contract. Their chest yields are a separate hardcoded system.

## Multiple Chests And Multiple Mods

Every piece is an ordinary fiddle key, so any number of chests from any number of mods coexist. Prefix your keys with a mod identifier (`my_mod_custom_chest`, not `custom_chest`), because fiddle merging is a silent last mod wins behavior on a collision. Several items may share one chest table, and one mod may ship many tables.

## Failure Modes

- A `fish_chest` key that names no merged chest table crashes when opened with the engine's own unexpected chest message. Check the spelling against your `chest_tables` entry.
- A table without `gold` or `items` fails during setup, before the title screen.
- A fishable rarity without a `fishing/votes` entry also fails during setup.

> [!NOTE]
> Uninstalling the mod is safe for saves. A saved chest item simply deserializes to the engine's unknown item placeholder.

## See Also

- [fish_chest_item_use](seams/fish_chest_item_use.md), [fish_chest_table_lookup](seams/fish_chest_table_lookup.md), [fish_chest_custom_rarity](seams/fish_chest_custom_rarity.md) - The engine fixes carrying this contract.
- [Mod Anatomy](MOD_ANATOMY.md) - The mod folder layout the fiddle files above live in.
- [items.use_guard](hooks/items.use_guard.md) - A guard over item use, modded chests included.
