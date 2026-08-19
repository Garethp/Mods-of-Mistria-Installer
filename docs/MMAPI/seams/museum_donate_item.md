# Seam: museum_donate_item

Emits the moment an item is donated to the museum.

`museum_donate_item` is a **template seam** (`op = "emit"`). It feeds [museum.donate_item](../hooks/museum.donate_item.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Museum.gml` |
| **Locator** | structural target: `donate_item_to_museum`, at head |
| **Op** | `emit` |
| **Feeds** | [`museum.donate_item`](../hooks/museum.donate_item.md) |
| **ctx built** | `{ item_id: item_id }` |
| **Marker** | `mmapi_museum_donate_item` |

## The Edit

The generated emit lands at the head of `donate_item_to_museum()`. It calls `mmapi_emit("museum.donate_item", { item_id: item_id })` in the uniform try/catch shape. The head placement puts the emit before everything the donation does: `register_item_to_museum()` (the progress write and its `T2R` fact), the pending renown entry push, and the set-progress scan that decides the `DonationResult`.

`donate_item_to_museum()` is the donation menu's single entry point, so the hook sees exactly the player's donations (plus those of the engine's test suite). The engine's two other progress writers, save load and the `ALL_UNLOCKS` new-game path, call `register_item_to_museum()` directly, below this seam's reach. That is what keeps load-time restoration from replaying as donations. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [museum.donate_item](../hooks/museum.donate_item.md) - This is the hook this seam dispatches.
- [player_renown_delta](player_renown_delta.md) - This is the filter the donation's pending renown entry drains through at day rollover.
- [quest_complete](quest_complete.md) - This is the other progression emit that queues a pending renown entry.
