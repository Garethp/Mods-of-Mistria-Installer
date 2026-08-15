# Engine Fix: save_load_pinned_spell_tolerance

An unknown pinned spell unpins instead of aborting the load.

`save_load_pinned_spell_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `pinned_spell` restore |
| **Op** | text (functor swap) |
| **Marker** | `mmapi_save_pinned_tolerance` |

## The Edit

```gml
    ARI.set_pinned_spell(opt_and_then(files.player.pinned_spell, try_string_to_spell)); // mmapi_save_pinned_tolerance
```

## Why

The pinned spell restores through the fatal `string_to_spell`. With the `try_` variant, an unknown name flows through `opt_and_then` as `undefined` into `set_pinned_spell(undefined)`, which is the engine's own unpin call. The spell menu's pin toggle uses exactly it. This fix logs no warn of its own, because a pinned spell is always also learned, so [save_load_spells_tolerance](save_load_spells_tolerance.md) has already warned about the name by the time this line runs. In practice this line is a companion fix, since the learned-spell entry would abort the load first, but tolerance at one site and a fatal at the next would be a standing trap.

Zero-registrant inert: resolvable names pin exactly as before.
