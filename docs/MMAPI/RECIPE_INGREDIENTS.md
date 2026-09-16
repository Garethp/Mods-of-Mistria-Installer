# Recipe Ingredients

[← MMAPI](MMAPI.md)

A mod can write a recipe ingredient that accepts any mix of items using only fiddle data. No GML, no hooks, and no handlers are involved.

A vanilla recipe component names exactly one item. The engine also accepts a `tag` component in recipe data but cannot count, pay, or display one. Seven catalog entries close both gaps. Four [engine fixes](seams/recipe_items_factory.md) add the `items` list form, and three [engine fixes](seams/recipe_tag_display.md) make a `tag` component count, pay, and display correctly.

A recipe ingredient is one entry in an item's `recipe` array, with a `count` and one key that says what is required. Vanilla recipes use `item`, which names one item. Two keys no vanilla recipe uses, `items` and `tag`, may also be used. `items` lists the accepted items, and `tag` names a tag every accepted item has.

## The `items` List

`items` lists several eligible ingredients for one requirement, and any mix of them satisfies it. A component declares `items` with an array of item names, or `item` for a single item.

```toml
# fiddle/items/my_mod_dishes.toml
[my_mod_omelet]
	name = "Custom Omelet"
	description = "Cooked with your choice of egg."
	stars = 3
	value = { store = 300 }
	restore = "dish"
	crafting_level_requirement = 22
	kitchen_tier_requirement = 2
	icon_sprite = "spr_ui_item_omelet"
	tags = ["food", "fried_dish"]
	recipe = [
		{ count = 2, items = ["egg", "duck_egg"] },
		{ count = 1, item = "butter" },
		{ count = 1, item = "cheese" },
		{ count = 1, item = "cow_milk" },
		{ hours = 0, minutes = 40 },
	]
```

`name` and `description` are the display text, written inline as the vanilla items do. The game's built-in localization rule for item names and descriptions covers merged items too, so they need no registration of their own.

- `items` lists item names. Each name resolves exactly as `item` does, so an unknown name stops the game at Setup. Repeated names are dropped.
- The first name is the component's canonical item. Recipe pricing, a dish's restore values, the recipe tooltip, the crafting menu's return-ingredient perks, and the Living Off The Land perk read that item and none of the others.
- `count` is the number required, and any mix of the listed items satisfies it. It defaults to 1.

> [!IMPORTANT]
> For `items` components, the crafting menu only shows the first listed item's icon and tooltip, regardless of what ingredients the player has available. Every listed item the player holds, in their inventory or a chest, counts toward the requirement. Payment drains the listed items in order, prioritizing inventory over chests.

> [!NOTE]
> A handler on [crafting.component_count](hooks/crafting.component_count.md) sees `ctx.component.item_id` as the first listed id, with the full list in `ctx.component.item_ids`.

## The `tag` Component

`tag` makes every item that has a specific tag an eligible ingredient for one requirement, including items other mods add. A component declares `tag` with an item tag.

```toml
recipe = [
	{ count = 2, tag = "egg" },
	{ count = 1, item = "butter" },
	{ hours = 0, minutes = 30 },
]
```

- `tag` names an item tag. Any item that has it satisfies the requirement, including items other mods add.
- A mod's own items can have any tag. A vanilla item's `tags` array cannot be extended from a mod, because fiddle merging replaces the array, so a set of vanilla items with no shared tag needs `items`.
- No other reader sees a tag component. The recipe tooltip omits it, recipe pricing and dish restore values skip it, and the return-ingredient perks never return a tagged item.
- `count` is the number required, and any mix of tagged items satisfies it. It defaults to 1. No perk reduces it, because the engine only applies its ingredient discounts to `item` and `items` components.

> [!IMPORTANT]
> For `tag` components, the crafting menu shows the icon of the first item in the game's item list that has the tag, regardless of what ingredients the player has available. Every tagged item the player holds, in their inventory or a chest, counts toward the requirement. Payment drains tagged stacks in slot order, prioritizing inventory over chests.

## Failure Modes

- An unknown item name in `item` or `items` stops the game at Setup with the name in the error message.
- A component with no recognized key stops the game at Setup with the recipe and the component number in the error message.
- A component with more than one recognized key parses by the engine's key order with no error. `item` beats `items`, and `items` beats `tag`. The losing keys are ignored, so a component that mixes `item` and `items` requires the single item and refuses the listed alternatives.
- An empty `items` array, or an `items` value that is not an array, stops the game at Setup with a runtime error.
- A tag no item has draws the game's missing item warning icon with a count of zero, and the recipe cannot be crafted.

## See Also

- [recipe_items_parse](seams/recipe_items_parse.md) - The parser branch that reads `items`.
- [recipe_tag_payment](seams/recipe_tag_payment.md) - The payment edit that makes `tag` correct in bulk and from chests.
- [crafting.component_count](hooks/crafting.component_count.md) - The filter over every component's cost.
- [Treasure Chests](TREASURE_CHESTS.md) - Another contract the catalog carries in data alone.
- [Mod Anatomy](MOD_ANATOMY.md) - The mod folder layout the fiddle files above live in.
