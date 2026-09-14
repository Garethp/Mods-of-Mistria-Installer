# Engine Fix: store_pet_cosmetic_entry

Lets a store stock entry declaring `pet_cosmetic` sell a pet cosmetic set, validated against the merged set list at Setup.

`store_pet_cosmetic_entry` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It carries the [pet cosmetic](../PET_COSMETICS.md) contract for store data. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Stores.gml` |
| **Locator** | text anchor: the tail of `parse_store_item`, from the `animal_cosmetic` branch's item construction through the closing `assert_neq` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_store_pet_cosmetic_entry` |

## The Edit

`parse_store_item()` turns one stock entry into a `LiveItem` by testing the entry keys `item`, `recipe_scroll`, `crafting_scroll`, `cosmetic`, and `animal_cosmetic` in turn, and any other entry fails the closing `assert_neq`. The engine's reward parser accepts a sixth key, `pet_cosmetic`, for the chicken statue's prize rolls, so a pet cosmetic set can be rolled but never sold. The replace adds that branch to the store parser in the reward parser's shape. It asserts that the value names a merged `pets/cosmetic_sets` key, then builds a `LiveItem(ItemId.PetCosmetic)` with `pet_cosmetic_set_name` set to the value.

The assert runs at Setup, where `PET_PROTOTYPE` is already loaded, so a misspelled set name stops the game with the name in the message. That is how the `cosmetic` and `animal_cosmetic` branches already behave. Without the assert the name would survive until the store menu first drew the shelf and failed inside the item's icon lookup with no name to show.

Everything after the parse keys on `ItemId.PetCosmetic` or its `ItemUse.UnlockPetCosmetic`, so the item inherits the vanilla behaviour unchanged. The shelf hides it once the set is owned anywhere, the basket accepts one copy, the price is the shared `pet_cosmetic` prototype's store value, and the tooltip, the unlock popup, and the unlock that runs when the player holds the item to use it all already handle the item. Festival stalls parse their stock through the same function and gain the key as well. No vanilla stock entry carries `pet_cosmetic`, so pristine data never reaches the new branch.

## See Also

- [Pet Cosmetics](../PET_COSMETICS.md) - The contract for mod authors that this edit carries.
- [store.item_added](../hooks/store.item_added.md) - The event that fires when the item lands in the basket.
- [pet_appearance_popup_scrollable](pet_appearance_popup_scrollable.md) - The catalog's other edit to the pet menu.
