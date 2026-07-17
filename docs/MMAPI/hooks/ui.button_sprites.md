# Hook: ui.button_sprites

Swap the sprite set a UI button is built from.

`ui.button_sprites` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when a button sprite set is built, just before it lands in `BUTTON_SPRITE_CACHE`. The filtered value is the built output. ctx is `{ key }`, the cache key. Return the replacement output (the result is cached, so the filter runs once per key), or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | When a button sprite set is built, just before it lands in `BUTTON_SPRITE_CACHE`. |
| **Value** | The built button sprite output, about to be cached. |
| **ctx** | `{ key }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `key` - The cache key the sprite set is built for. The seam inserts the filtered output into `BUTTON_SPRITE_CACHE` under this key.

> [!NOTE]
> The result is cached: the filter runs once per key, and your replacement is what every later consumer of that key sees.

## Usage

```gml
// ui.button_sprites is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function fresh_coat_ui_button_sprites(_value, _ctx) {
    // _value is the built button sprite set, about to land in
    // BUTTON_SPRITE_CACHE.
    // _ctx is { key }.
    //   .key - the cache key this set is built for. The result is cached,
    //          so the filter runs once per key.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.key == <the key your mod reskins>) {
    //     return <your replacement output>;
    // }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.button_sprites", fresh_coat_ui_button_sprites);
```

## Engine Wiring

- Seam [`ui_button_sprites`](../seams/ui_button_sprites.md) dispatches from `gml/scripts/UI/Anchor/anchor_utils.gml`, filtering `output` just before `BUTTON_SPRITE_CACHE.insert(key, output)`.

## See Also

- [ui.sprite](ui.sprite.md) - Swap the backplate sprites behind the mines menu and spell cards.
- [ui.item_icon](ui.item_icon.md) - Swap the sprite an item shows as its icon.
- [ui.item_node](ui.item_node.md) - Adjust UI item slots as they are populated.
