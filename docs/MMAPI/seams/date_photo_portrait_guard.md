# Engine Fix: date_photo_portrait_guard

Lets a date photo open when its NPC's portrait sprite is not installed. The photo renders without the portrait overlay, with a logged warn, instead of killing the popup.

`date_photo_portrait_guard` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor on the photo popup's portrait resolution in `spawn_date_photo` |
| **Op** | text (guarded render) |
| **Marker** | `mmapi_date_photo_portrait_guard` |

## The Edit

The outfit and portrait lookups are null-guarded, and the portrait overlay node is only built when a sprite resolved:

```gml
    var sprite = undefined;
    if (outfit != undefined) { sprite = outfit.portraits.get(visuals.expression); }

    if (sprite == undefined) {
        warn("MMAPI: date photo portrait for '{}' is not installed - photo shown without the portrait overlay", npc_id_to_string(data.npc));
    } else {
        // vanilla portrait overlay, unchanged
    }
```

## Why

Date photos are the tombstone contract's showcase: a departed NPC's photos survive because the enum member keeps the name resolving. But the photo popup dereferences the NPC's portrait sprite unguarded, and a vacancy's portraits table is empty by necessity, because portrait sprite names derive from the symbol through a fatal lookup no absent mod can satisfy. `set_sprite` then calls `sprite_get_number(undefined)` and the popup dies, which converts a kept memory into a broken interaction.

The guard also covers a live extension NPC whose mod ships fewer portrait emotions or outfits than a date's visuals request, which would fault the same way.

Zero-registrant inert: every vanilla NPC-and-date pairing resolves a portrait, so the guarded arm never fires on an intact install.
