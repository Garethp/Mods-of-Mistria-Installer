# Engine Fix: save_load_spells_tolerance

Lets a save that learned a since-removed custom spell load anyway. The spell is forgotten with a logged warn instead of the load aborting.

`save_load_spells_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `spells_learned` deserialize |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_spells_tolerance` |

## The Edit

The replace swaps the converter the array loader calls for its tolerant variant.

```gml
    ARI.spells_learned = deserialize_array_bool(
        files.player.spells_learned,
        function(__mmapi_s) { return try_string_to_spell(__mmapi_s); }, // mmapi_save_spells_tolerance
        Spell.LEN,
    );
```

The save records learned spells by name, and this line resolves them with the fatal `string_to_spell`. It is one of the load pipeline's fatal name lookups that the `save_load_*` family in the [Catalog](../CATALOG.md) covers. Vanilla's own perks, items, and recipes lines directly beside it use the tolerant `try_` variants, so the tolerant form is the engine's own convention and this line is the exception. One unknown name, such as a custom spell whose mod was uninstalled, aborts the load natively. There is no dialog and nothing in any log. The game returns to the title screen.

The fix swaps in `try_string_to_spell`, which returns `undefined` for unknown names and so routes an unknown spell through the skip `deserialize_array_bool` already has. That is the same path perks already take. [save_load_forget_warn](save_load_forget_warn.md) names the dropped spell in the log. Re-saving writes the cleansed list, and reinstalling the mod before re-saving restores the spell untouched.

For a name that resolves, the `try_` variant returns the same ordinal as the fatal one, so an intact install behaves exactly as before.

## See Also

- [save_load_pinned_spell_tolerance](save_load_pinned_spell_tolerance.md) - The companion fix for the pinned slot.
- [save_load_forget_warn](save_load_forget_warn.md) - Names the dropped spell in the log.
- [Custom Spells](../CUSTOM_SPELLS.md) - The guide, with the uninstall story in full.
