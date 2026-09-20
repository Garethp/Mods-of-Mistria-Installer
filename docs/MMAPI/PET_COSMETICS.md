# Pet Cosmetics

[← MMAPI](MMAPI.md)

A mod can sell a pet cosmetic set from any store using only fiddle data. No GML, no hooks, and no handlers are involved. The engine already sells player and animal cosmetics from store stock, and one catalog entry, the [store_pet_cosmetic_entry](seams/store_pet_cosmetic_entry.md) engine fix, extends the stock parser so it accepts pet cosmetic sets the same way.

A pet cosmetic sold in a store is two linked pieces of data. The pet data declares the set and its cosmetics, and a store stock entry names the set.

## The Cosmetic Set

Pet cosmetics are ordinary pet data. A set is what the store sells and the player unlocks. Each cosmetic belongs to one set and supplies the sprites for one pet kind.

```toml
# fiddle/pets.toml
[cosmetic_sets.my_mod_top_hat]
	ui_icon = "spr_my_mod_pet_top_hat_icon"
	name = "Top Hat"

[cosmetics.cat_my_mod_top_hat]
	pet_kind = "cat"
	sprite_template = "spr_my_mod_pet_cat_top_hat_{ANIMATION_KEY}"
	cosmetic_set = "my_mod_top_hat"
```

- `name` is the display text, written inline as the vanilla sets do. The game's built-in localization rule for pet set names covers merged sets too, so it needs no registration of its own.
- A set needs one cosmetic for every pet kind it should cover, because the pet menu shows a set only when it has a cosmetic for the player's current pet kind. Vanilla sets cover every kind.
- The store does not check the pet's kind, only whether the set is owned. A player whose pet is a skipped kind can never equip the set.

## The Store Entry

Declare `pet_cosmetic` on a stock entry. Its value names an entry under `pets/cosmetic_sets`.

`MOMIidentify` matches the existing category to extend. `MOMIaction = "merge"` then appends the entries to its stock rather than replacing it.

```toml
# fiddle/stores.toml
[[hayden.categories]]
	MOMIidentify = { icon = "spr_ui_store_category_icon_animal_accessories" }
	MOMIaction = "merge"
	constant_stock = [
		{ pet_cosmetic = "my_mod_top_hat", requirements = { seen_cutscene = "pet_arrival" } },
	]
```

- The store has no pet gate of its own. The chicken statue, by contrast, offers a pet cosmetic only once the pet has arrived. Using `requirements = { seen_cutscene = "pet_arrival" }` gives a store entry the same gate. Without it a player with no pet can buy and use the item.
- The price is the vanilla `pet_cosmetic` item's `value.store`, 500 by default, and every pet cosmetic in every store shares it. There is no price per set.
- Any store requirement works on the entry, and festival stalls accept the key too.

> [!NOTE]
> The item takes `ItemUse.UnlockPetCosmetic` automatically and follows the vanilla cosmetic flow. The basket takes one copy, the purchase is soulbound, and the unlock that runs when the player holds the item to use it adds the set to the pet menu. Adding it to the basket fires [store.item_added](hooks/store.item_added.md) like any other item.

## Failure Modes

- A `pet_cosmetic` value that names no merged set stops the game at Setup with the set name in the message, before the title screen. Check the spelling against your `cosmetic_sets` key, and make sure the value is the set, not one of its cosmetics.
- A set with no cosmetics is invisible in the pet menu and fails a debug assertion in the pet prototype loader.

> [!NOTE]
> Uninstalling the mod is safe when the cosmetic item has been used (it's not in the player's inventory) and the pet is not currently wearing the cosmetic.

> [!WARNING]
> A save will fail to load if the cosmetic item is left unused in the player's inventory, or the cosmetic is currently equipped by a pet.

## See Also

- [store_pet_cosmetic_entry](seams/store_pet_cosmetic_entry.md) - The catalog entry carrying this contract.
- [store.item_added](hooks/store.item_added.md) - The event that fires when any item lands in the basket.
- [store.stock](hooks/store.stock.md) - The filter for stock the data cannot compute.
- [Treasure Chests](TREASURE_CHESTS.md) - Another contract the catalog carries in data alone.
- [Mod Anatomy](MOD_ANATOMY.md) - The mod folder layout the fiddle files above live in.
