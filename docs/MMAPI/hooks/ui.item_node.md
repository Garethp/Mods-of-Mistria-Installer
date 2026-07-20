# Hook: ui.item_node

Adjust UI item slots as they are populated.

`ui.item_node` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when a UI item node is populated: in `item_node.set_to_item()` with the struct `{ node, item, count }`, and in the crafting menu grid with `{ node, item, count, source: "crafting_menu" }`. Both dispatch sites call `mmapi_apply_filters` and discard the return value, so handlers must mutate the value struct in place. The struct is the second argument to the dispatcher; the third, `ctx`, argument is `undefined`.

| | |
| --- | --- |
| **Fires** | When a UI item node is populated: `item_node.set_to_item()` and the crafting menu grid. |
| **Value** | The `{ node, item, count }` struct. The crafting site adds `source: "crafting_menu"`. |
| **ctx** | `undefined` - the struct rides in the value position. |
| **Kind contract** | In-place filter. Mutate `_value`; the dispatcher discards every return value. |

### The value struct

- `node` - the UI item node being populated, whose sprite has just been set from `item.get_ui_icon()`.
- `item` - the item the node is being set to.
- `count` - the stack count (`set_to_item()`'s `count` argument, default `1`, and always `1` at the crafting site).
- `source` - `"crafting_menu"` at the crafting menu grid site, and absent at `set_to_item()`.

> [!IMPORTANT]
> This is an in-place filter: the return value is discarded. Mutate the value you are given. Never build a replacement.

## Usage

```gml
// ui.item_node is an IN-PLACE filter: the return value is DISCARDED.
// Change the struct/instance you are given; never build a replacement.
function stack_lens_ui_item_node(_value, _ctx) {
    // _value is the { node, item, count } struct - the crafting menu grid
    // adds source: "crafting_menu". _ctx is undefined: the struct rides in
    // the value position.
    //   .node   - the UI item node being populated (its sprite is already set).
    //   .item   - the item the node is set to.
    //   .count  - the stack count (always 1 at the crafting site).
    //   .source - "crafting_menu" at the crafting grid; absent in set_to_item().
    // mutate _value.node here, e.g. restyle the slot for items you own
    // no return statement - the game keeps reading the same struct
}

mmapi_filter("ui.item_node", stack_lens_ui_item_node);
```

## Engine Wiring

- Seam [`ui_item_node_set_to_item`](../seams/ui_item_node_set_to_item.md) dispatches from `gml/scripts/UI/Anchor/anchor_utils.gml`, inside `item_node.set_to_item()`, right after the node's sprite is set from `item.get_ui_icon()`.
- Seam [`ui_item_node_crafting_menu`](../seams/ui_item_node_crafting_menu.md) dispatches from `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml`, in the crafting grid build, right after `icon.set_sprite(item.get_ui_icon())`.

## See Also

- [ui.item_icon](ui.item_icon.md) - Swap the sprite an item shows as its icon.
- [ui.button_sprites](ui.button_sprites.md) - Swap the sprite set a UI button is built from.
- [combat.tarball_grid](combat.tarball_grid.md) - This is the other in-place filter, which mutates a swing's grid-action flags.
