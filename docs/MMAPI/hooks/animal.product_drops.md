# Hook: animal.product_drops

Change what an animal's production drops.

`animal.product_drops` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `Stable.on_new_day()`'s drop block, after the normal and golden product lists are rolled and the Eggstra perk may add its extra product, and before the production stats record and the drops land in the stable grid. The filtered value is the struct `{ normal_products, golden_products }`. ctx is `{ animal, stable, production, production_tier }`.

Mutate the Lists in place, return a replacement struct, or return `undefined` to keep the current lists. The seam re-reads both fields defensively, so a non-struct return or a partial struct keeps the engine lists.

> [!IMPORTANT]
> `normal_products` and `golden_products` are **Lists** (`count()`, `get()`, `push()`), not arrays. Mutate or return Lists of `LiveItem`, never plain arrays.

| | |
| --- | --- |
| **Fires** | In `Stable.on_new_day()`'s drop block, after the Eggstra roll and before the stats records and grid pushes. |
| **Value** | `{ normal_products, golden_products }`, two Lists of `LiveItem`. |
| **ctx** | `{ animal, stable, production, production_tier }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `normal_products` - the List of `LiveItem` the normal roll produced, including any Eggstra extra.
- `golden_products` - the List of `LiveItem` the golden roll produced. It is empty below max hearts.

### The ctx struct

- `animal` - the `Animal` struct producing.
- `stable` - the `Stable` processing its new day (`self` inside `on_new_day()`).
- `production` - the production struct for the animal's sex from its prototype. Read `production.normal_product` and `production.golden_product` for the item ids.
- `production_tier` - the tier entry the animal's heart level resolved. Read `production_tier.normal` and `production_tier.golden` for the vanilla counts and bonus chances.

## Usage

```gml
// animal.product_drops is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function generous_stalls_animal_product_drops(_value, _ctx) {
    // _value is { normal_products, golden_products }, two Lists of LiveItem.
    // _ctx is { animal, stable, production, production_tier }.
    // e.g. one extra normal product per delivery:
    // _value.normal_products.push(new LiveItem(_ctx.production.normal_product));
    return undefined; // undefined = keep the (possibly mutated) lists
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("animal.product_drops", generous_stalls_animal_product_drops);
```

## Interactions

- Scheduling sits upstream. [animal.production_gate](animal.production_gate.md) decides which animals run the production step, and [animal.product_ready](animal.product_ready.md) decides whether this drop block runs at all.
- The Eggstra extra product is already in `normal_products` when the hook fires, and the perk's own stat is already recorded.
- Emptying both lists suppresses the drops and their `GAME_STATS.animal_production` records, because both push blocks test `is_empty()` first.
- The CurrencyOfCareTwo bead roll and the bonus gather item roll run after this hook whatever the lists hold.

## Engine Wiring

- Seam [`animal_product_drops`](../seams/animal_product_drops.md) dispatches from `gml/scripts/GameplaySystems/Ranching/Stable.gml`, between the Eggstra roll and the stats records, re-reading both lists defensively.

## See Also

- [animal.product_ready](animal.product_ready.md) - Change whether an animal produces today.
- [animal.production_gate](animal.production_gate.md) - Change which animals are eligible to produce each day.
- [animal.heart_points](animal.heart_points.md) - Hearts resolve the production tier this hook receives in ctx.
- [items.treasure_distribution](items.treasure_distribution.md) - The dungeon counterpart. It filters a rolled drop before it lands.
