# Seam: storage_trash_button

Announces a chest trash tap while the slot still holds the item.

`storage_trash_button` is a **template seam** (`op = "emit"`). It feeds [items.trashed](../hooks/items.trashed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StorageMenu.gml` |
| **Locator** | pristine context: the top of the trash button's `set_tap_callback`, before the Shift-checked `drain()`/`pop()` block |
| **Op** | `emit` |
| **Feeds** | [`items.trashed`](../hooks/items.trashed.md) |
| **ctx built** | `{ item: self.hand.slot(0).item, count: keyboard_check(vk_shift) ? self.hand.slot(0).count : 1 }` |
| **Marker** | `mmapi_storage_run_item_trashed` |

## The Edit

The same shape as the inventory twin, at the StorageMenu (chest) trash button: the generated emit lands at the top of the button's tap callback, before the slot `pop()`/`drain()` removes the item(s). The item and unit count are captured while the slot still holds them. The one difference is where the hand lives: in the StorageMenu it is `self.hand` (the PlayerMenu twin's is `self.inventory.hand`). ctx.`item` reads `self.hand.slot(0).item`, and ctx.`count` mirrors the branch the callback is about to take, the whole stack (`slot(0).count`) when Shift is held and the callback will `drain()`, else `1` for the single `pop()`. The dispatch runs in the uniform try/catch shape (catch var `__mmapi_storage_trash_button`).

This tap callback is one of the two real trash signals in the game: the generic slot `pop()`/`drain()` are shared by every inventory move, so only the two trash-button callbacks distinguish a trash from an ordinary slot operation (the game itself marks them with the `SoundEffects/UI/UITrashItem` tap sound). This is an event, so the trash still happens.

## See Also

- [items.trashed](../hooks/items.trashed.md) - This is the hook this seam dispatches.
- [inventory_trash_button](inventory_trash_button.md) - This is the twin emit on the PlayerMenu (journal inventory) trash button, whose hand is `self.inventory.hand`.
