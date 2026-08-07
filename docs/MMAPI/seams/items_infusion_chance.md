# Seam: items_infusion_chance

Filters the infusion roll chance in `craft_into()`, hoisted out of the roll condition before `chance_percent` consumes it.

`items_infusion_chance` is a **text seam**. It feeds [items.infusion_chance](../hooks/items.infusion_chance.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Recipe.gml` |
| **Anchor** | the roll condition in `craft_into()` (`if !infusions.is_empty() && chance_percent(15 + bonus_infusion_chance) {`) |
| **Feeds** | [`items.infusion_chance`](../hooks/items.infusion_chance.md) |
| **Value filtered** | the roll chance percent (`15 + bonus_infusion_chance`) |
| **ctx built** | `{ recipe: self, candidates: infusions }` |
| **Marker** | `mmapi_items_infusion_chance` |

## The Edit

The edit replaces `craft_into()`'s roll condition with a three-line form: the vanilla chance expression (`15 + bonus_infusion_chance`, the base 15 plus the Empowered perk bonuses tallied just above) is hoisted into a local, reassigned through `mmapi_apply_filters("items.infusion_chance", ...)` under the uniform try/catch shape, and the condition then consumes the filtered local in place of the inline expression. The candidate list built by `generate_infusions()` rides in ctx by reference, so handlers can shape the pool the `choose_random` pick draws from as well as the odds.

The chance lives inline inside a compound `if` condition, which no template op can rewrite, so the seam takes the text form. The empty-candidate gate is untouched: an empty list still short-circuits the roll regardless of the filtered chance.

## See Also

- [items.infusion_chance](../hooks/items.infusion_chance.md) - This is the hook this seam dispatches.
- [items_infusion_generate](items_infusion_generate.md) - This is the sibling dispatch that can veto candidate generation upstream in the same file.
