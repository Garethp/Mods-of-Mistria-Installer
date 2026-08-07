# Hook: items.infusion_chance

Change the odds that a crafted item rolls an infusion.

`items.infusion_chance` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `Recipe.craft_into()`, once per crafted item, right before the infusion roll. The filtered value is the roll chance percent: the engine computes 15, plus 5 per Empowered perk tier, so the vanilla value is 15 to 25. ctx is `{ recipe, candidates }`. Return the replacement percent, or `undefined` to keep the current value. The percent feeds `chance_percent`, which treats 0 as never (an explicit zero check, not just a failed roll) and 100 or more as always.

| | |
| --- | --- |
| **Fires** | In `Recipe.craft_into()`, once per crafted item, right before the roll condition. |
| **Value** | The roll chance percent (vanilla 15, +5 per Empowered perk tier). |
| **ctx** | `{ recipe, candidates }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `recipe` - the `Recipe` struct being crafted. Read `ctx.recipe.item_id` for the item.
- `candidates` - the LIVE engine List of candidate entries the uniform pick draws from, each a `{ infusion, perk }` struct (already filtered for perk requirements, craftability, and star gates). Because the list is live, a handler may also prune or push entries to shape the selection itself. A pushed entry's `perk` may be `undefined`.

The hook fires even when the candidate list is empty. An empty list yields no infusion regardless of the returned chance, which includes items with a `default_infusion`, whose generation returns empty by design. To veto generation outright, use [items.infusion_generate](items.infusion_generate.md) instead of returning 0 here. The guard empties the pool before candidates are ever built.

## Usage

```gml
// items.infusion_chance is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function dairy_devotion_items_infusion_chance(_value, _ctx) {
    // _value is the roll chance percent (vanilla 15-25).
    // _ctx is { recipe, candidates }.
    //   .recipe.item_id - the item being crafted.
    //   .candidates     - the live { infusion, perk } List the pick draws from.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else

    // Item ids are minted at boot and shift between game versions and mod
    // sets, so resolve them by name and never hardcode the ordinal.
    var cheese_id = try_string_to_item_id("cheese");
    if (cheese_id == undefined || _ctx.recipe.item_id != cheese_id) {
        return undefined; // not our item: keep the game's value
    }
    return _value * 2; // crafting cheese has double the vanilla odds
}

mmapi_filter("items.infusion_chance", dairy_devotion_items_infusion_chance);
```

Scoping by item class instead of a single item works the same way through the prototype's tags (`ITEM_PROTOTYPES[_ctx.recipe.item_id].tags.contains("armor")`). The live tags carrier is an engine List, so guard the read with try/catch.

### Raising the odds for one infusion

The engine has one shared roll, so odds for a specific infusion mean reacting to that infusion being in the candidate pool. Scan `ctx.candidates` and boost only then:

```gml
// When Fortified is on the table, raise the roll odds and leave every
// other craft untouched.
function stalwart_smith_items_infusion_chance(_value, _ctx) {
    if (_value == undefined) return undefined;

    var fortified = string_to_infusion("fortified");
    var candidates = _ctx.candidates;
    for (var i = 0; i < candidates.count(); i++) {
        if (candidates.get(i).infusion == fortified) {
            return max(_value, 60); // 60% whenever Fortified can land
        }
    }
    return undefined; // Fortified is not eligible: keep the game's value
}
```

This raises the odds that some candidate lands, still picked uniformly. To favor the infusion itself, shape the pool as well, as the next section shows.

### Forcing an infusion and shaping the pool

The candidate List is live and `choose_random` picks uniformly, which gives three levers: prune to force, duplicate to weight, push to add. Guaranteeing one infusion combines the chance with a prune:

```gml
// Veil always applies: guarantee the roll fires AND that veil wins the pick.
function veilwright_items_infusion_chance(_value, _ctx) {
    if (_value == undefined) return undefined;

    var veil = try_string_to_infusion("veil"); // fail-soft if the infusion is absent
    if (veil == undefined) return undefined;

    var candidates = _ctx.candidates;
    var found = false;
    for (var i = candidates.count() - 1; i >= 0; i--) { // backwards: safe removal
        if (candidates.get(i).infusion == veil) {
            found = true;
        } else {
            candidates.remove(i); // prune everything that is not veil
        }
    }
    if (!found) {
        candidates.push({ infusion: veil, perk: undefined }); // no perk stat tally
    }
    return 100; // chance_percent(100) always passes
}
```

A pushed entry bypasses the validity filtering `generate_infusions()` applied to the natural candidates (perk requirements, craftability, the star gate), so only push infusions you know are legal for the item. For a bias rather than a guarantee, push duplicate references of an existing entry instead of pruning: two extra copies of one entry give it triple the weight in the uniform pick, with the chance left alone. Pruning the pool to empty is a legitimate "never infuse this craft", though the [items.infusion_generate](items.infusion_generate.md) guard is the cleaner veto, because it skips candidate construction entirely.

## Engine Wiring

- Seam [`items_infusion_chance`](../seams/items_infusion_chance.md) dispatches from `gml/scripts/GameplaySystems/Recipe.gml`, hoisting the chance out of `craft_into()`'s roll condition and filtering it before `chance_percent` consumes it.

## See Also

- [items.infusion_generate](items.infusion_generate.md) - Stop a recipe from rolling infusions.
- [crafting.pay_component_costs](crafting.pay_component_costs.md) - Veto a recipe's material payment.
- [crafting.max_crafts](crafting.max_crafts.md) - Take over how many of a recipe can be crafted.
