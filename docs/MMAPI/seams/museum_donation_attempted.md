# Seam: museum_donation_attempted

Emits an event at the beginning of a museum donation attempt.

`museum_donation_attempted` is a **text seam** (`context_before` + `context_after`). It feeds [museum.donation_attempted](../hooks/museum.donation_attempted.md). Mod authors register handlers for the hook; they do not write seams. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Museum.gml` |
| **Locator** | the unique `donate_item_to_museum(item_id)` declaration and its first registration call |
| **Op** | text insertion |
| **Feeds** | [`museum.donation_attempted`](../hooks/museum.donation_attempted.md) |
| **Context** | `{ item_id: item_id }` |
| **Marker** | `mmapi_museum_run_donation_attempted_callbacks` |

## Behavior

The dispatch runs before the pristine `register_item_to_museum(item_id)` call. The original donation function, including every `DonationResult` return path, remains engine-owned. The event is guarded so a failing mod callback cannot prevent the donation operation.
