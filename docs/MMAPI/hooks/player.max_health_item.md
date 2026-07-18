# Hook: player.max_health_item

Know when an item permanently raises Ari's max health.

`player.max_health_item` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires right after an item's `max_health_modifier` raises `ARI.base_health`, before the vitals menu updates. ctx is `{ player, amount, live_item }`. At emit time the vitals menu still shows the old maximum (`set_max_health` runs next), and the item's ordinary `health_modifier` heal has not applied yet.

| | |
| --- | --- |
| **Fires** | Right after `ARI.base_health += live_item.prototype.max_health_modifier`, before the vitals menu updates. |
| **ctx** | `{ player, amount, live_item }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `player` - the `ARI` struct whose `base_health` just rose.
- `amount` - the item's `max_health_modifier`: how much `base_health` rose by.
- `live_item` - the `LiveItem` that carried the modifier.

## Usage

```gml
// player.max_health_item is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function vital_feast_player_max_health_item(_ctx) {
    // _ctx is { player, amount, live_item }.
    //   .player    - the ARI struct whose base_health just rose.
    //   .amount    - the item's max_health_modifier (how much it rose by).
    //   .live_item - the LiveItem that carried the modifier.
    // e.g. celebrate the milestone with a bonus heal on top:
    // _ctx.player.modify_health(_ctx.amount, true);
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.max_health_item", vital_feast_player_max_health_item);
```

## Engine Wiring

- Seam [`player_max_health_item`](../seams/player_max_health_item.md) dispatches from `gml/scripts/Player/AriFsm.gml`, between the `ARI.base_health` raise and the `ANCHOR.get_menu(Menu.Vitals).set_max_health(...)` update that follows it.

## See Also

- [items.consumed](items.consumed.md) - Know when the player eats any item, modifier or not.
- [player.health_delta](player.health_delta.md) - Filter the item's regular `health_modifier` heal, which applies right after this event.
- [ui.menu_refreshed](ui.menu_refreshed.md) - Know when the vitals menu rebuilds its content.
