# Engine Fix: save_load_pinned_spell_tolerance

An unknown pinned spell unpins instead of aborting the load.

`save_load_pinned_spell_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `pinned_spell` restore |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_pinned_tolerance` |

## The Edit

The replace swaps the fatal converter for its tolerant variant.

```gml
    ARI.set_pinned_spell(opt_and_then(files.player.pinned_spell, function(__mmapi_s) { return try_string_to_spell(__mmapi_s); })); // mmapi_save_pinned_tolerance
```

The pinned spell restores through the fatal `string_to_spell`. With the `try_` variant, an unknown name flows through `opt_and_then` as `undefined` into `set_pinned_spell(undefined)`, which is the engine's own unpin call. The spell menu's pin toggle makes the same call. This fix logs no warn of its own, because a pinned spell is always also learned, so [save_load_spells_tolerance](save_load_spells_tolerance.md) has already warned about the name by the time this line runs. On its own this line would rarely fire, since the learned-spell entry aborts the load first. Tolerance at one site and a fatal lookup at the next would still be a crash, so the pair ships together.

On an intact install every name resolves and pins exactly as before.

## See Also

- [save_load_spells_tolerance](save_load_spells_tolerance.md) - The learned-spell half this fix completes.
- [Custom Spells](../CUSTOM_SPELLS.md) - The guide, with the uninstall story in full.
