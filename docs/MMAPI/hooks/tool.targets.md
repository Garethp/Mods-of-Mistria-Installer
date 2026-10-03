# Hook: tool.targets

Change which cells a tool use covers.

`tool.targets` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the player's Tool FSM state wherever the engine resolves the cells a tool use covers. The filtered value is the `List` of `Vec2` cells that `range_pattern_to_targets()` built for the use. ctx is `{ x, y, range_pattern, cardinal, item }`. Return a replacement `List`, mutate the `List` in place and return `undefined`, or return `undefined` to keep the current value.

The hook fires at two sites, listed under Where It Fires, and both hand the handler the same ctx.

| | |
| --- | --- |
| **Fires** | In `PlayerState.Tool`, at the two sites listed below. |
| **Value** | The `List` of `Vec2` cells the use covers. |
| **ctx** | `{ x, y, range_pattern, cardinal, item }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

> [!IMPORTANT]
> Hot path. This filter fires every step of the Tool state while a tool is in use, and again each time the charge advances. Make the callback's first check its cheapest early-exit.

### The ctx struct

- `x`, `y` - the origin cell the use targets.
- `range_pattern` - the `RangePattern` value of the current charge level. It is `RangePattern.One` for an uncharged use, and it climbs through `OneByThree`, `ThreeByThree`, `ThreeBySix`, `SixBySix`, and `SixByNine` as the charge advances.
- `cardinal` - the player's facing `Cardinal`.
- `item` - the `LiveItem` in use. `item.prototype.tool_type` is a `ToolType` for a tool and `undefined` for seeds, saplings, and grass.

### The value

- The value is an engine `List`, not an array. Read it with `count()` and `get(i)`, add to it with `push(cell)`, and build a cell with `Vec2(x, y)`.
- The engine builds a new `List` for every dispatch, so a handler can mutate it in place.
- Cells are grid cells of 8 pixels. A tile spans 2 cells, and the engine's own patterns step by 2.
- A final value without a working `count()` is discarded, and the engine's `List` is kept. Nothing validates the entries, so every entry must be a `Vec2`.
- The action loop skips a cell that lies outside the grid.
- An empty `List` makes the use cover no cells.

### Where It Fires

| Site | When | What The Engine Does With The Final Value |
| --- | --- | --- |
| Step | Every step of the Tool state, up to and including the step on which the use acts. | It draws the charging preview from the value while the player charges, and it visits the value's cells on the step the use acts. |
| Stamina | Once each time the charge advances to the next range pattern. | It multiplies the value's `count()` by the item's stamina cost per cell and compares the product against current stamina. A pattern the player cannot afford resets the charge to `RangePattern.One`. |

> [!IMPORTANT]
> Answer from ctx alone. The two sites are separate dispatches, and only a handler that returns the same cells at both is charged for the cells it works.

Every use that enters the Tool state passes through the step site. That covers the axe, pickaxe, hoe, shovel, watering can, net, seeds, saplings and planting.

## Usage

```gml
// tool.targets is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function long_hoe_tool_targets(_value, _ctx) {
    // _value is the List of Vec2 cells the use covers.
    // _ctx is { x, y, range_pattern, cardinal, item }.
    // Every Tool-state use passes through, seeds and saplings included, so
    // test the charge level and the item first.
    // HOT PATH: every step of the Tool state. Make your first check the
    // cheapest one and get out early.
    if (_ctx.range_pattern != RangePattern.OneByThree) return undefined;
    if (_ctx.item.prototype.tool_type != ToolType.Hoe) return undefined;

    // Stretch the hoe's three-tile line to five tiles. A tile spans 2 cells.
    var _dx = 0;
    var _dy = 0;
    switch (_ctx.cardinal) {
        case Cardinal.East:  _dx = 2;  break;
        case Cardinal.West:  _dx = -2; break;
        case Cardinal.South: _dy = 2;  break;
        case Cardinal.North: _dy = -2; break;
    }
    _value.push(Vec2(_ctx.x + _dx * 3, _ctx.y + _dy * 3));
    _value.push(Vec2(_ctx.x + _dx * 4, _ctx.y + _dy * 4));
    return undefined; // undefined = keep the (mutated) List
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("tool.targets", long_hoe_tool_targets);
```

The handler answers from ctx alone, so the stamina site counts five cells for the same charge level the step site previews and tills.

## Interactions

- The action loop calls the item's callback once per cell. Sapling and grass planting ignore the cell they are handed and plant at the origin, so a value with more than one cell repeats that planting attempt. Those uses always arrive with `RangePattern.One`.
- Seeds charge only with the Superb Sower perk, and the engine caps their highest range pattern from the seed count in the held slot against fixed thresholds. A seed use stops planting when the slot runs out.
- The highest range pattern a tool reaches comes from its `quality` in fiddle data, and its stamina cost per cell comes from its `stamina_cost`. This hook changes neither.
- Each cell's stamina cost still passes through [player.stamina_delta](player.stamina_delta.md) when that cell's action lands.
- [resource.node_modifier](resource.node_modifier.md) still receives the charged penalty for every pick and chop of a use whose `range_pattern` is not `RangePattern.One`, whatever cells this hook returns.
- The Earthbreaker perk's chained picks read `range_pattern_to_targets()` directly and never pass through this hook. [resource.node_picked](resource.node_picked.md) reports them with `effect_override` set.
- Tool descriptions state each tool's charge area as fiddle text, which this hook does not change.

## Engine Wiring

- Seam [`tool_targets`](../seams/tool_targets.md) dispatches from `gml/scripts/Player/AriFsm.gml`, filtering the `List` the Tool state's step resolves before the charging preview and the action loop read it.
- Seam [`tool_targets_stamina_gate`](../seams/tool_targets_stamina_gate.md) dispatches from the same file, replacing the charge loop's table count with the count of the filtered `List`.

## See Also

- [resource.node_modifier](resource.node_modifier.md) - Change the charged-tool modifier on picks and chops.
- [resource.node_picked](resource.node_picked.md) - Know the moment a pick lands on a rock or dig site.
- [resource.node_chopped](resource.node_chopped.md) - Know the moment a chop lands on a tree or stump.
- [player.stamina_delta](player.stamina_delta.md) - Change every stamina cost or gain before it applies.
- [items.use_guard](items.use_guard.md) - Block an item from being used.
- [fsm.transition](fsm.transition.md) - Redirect or cancel any state transition in the game's shared FSMs.
