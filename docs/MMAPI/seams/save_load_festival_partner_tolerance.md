# Engine Fix: save_load_festival_partner_tolerance

Lets a save with an accepted festival date load after the partner NPC's mod is removed. The pending date is cleared with a logged warn instead of the load aborting.

`save_load_festival_partner_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `festival_date_partner` line |
| **Op** | text (functor swap plus warn arm) |
| **Marker** | `mmapi_save_festival_partner_tolerance` |

## The Edit

```gml
    var __mmapi_fdp = files.player[$ "festival_date_partner"]; // mmapi_save_festival_partner_tolerance
    if (!is_nullish(__mmapi_fdp) && __mmapi_fdp != "" && try_string_to_npc_id(__mmapi_fdp) == undefined) {
        warn("MMAPI: save carried festival date partner '{}' who no longer resolves - cleared", __mmapi_fdp);
        ARI.festival_date_partner = undefined;
    } else {
        ARI.festival_date_partner = opt_and_then(__mmapi_fdp, string_to_npc_id);
    }
```

## Why

Accepting a festival date stores the partner's name, and the load resolves it through fatal `string_to_npc_id`. Without a tombstone keeping the name alive, the save aborts natively at this line.

The fix engages only for a non-empty name the tolerant variant cannot resolve, clearing the pending date with a warn naming the partner. Clearing is the correct amputation: the festival-day lookup path for a departed partner was already documented as unsafe, so a cleared date loses less than a kept one.

Every other value takes the exact vanilla expression. That includes the empty string, which fresh saves serialize for this field and which the vanilla line passes through `string_to_npc_id` without throwing, so the fix neither warns about it nor changes whatever value vanilla derives from it.
