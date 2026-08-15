# Engine Fix: save_load_spells_tolerance

Lets a save that learned a since-removed custom spell load anyway. The spell is forgotten with a logged warn instead of the load aborting.

`save_load_spells_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `spells_learned` deserialize |
| **Op** | text (functor swap) |
| **Marker** | `mmapi_save_spells_tolerance` |

## The Edit

```gml
    ARI.spells_learned = deserialize_array_bool(
        files.player.spells_learned,
        try_string_to_spell, // mmapi_save_spells_tolerance
        Spell.LEN,
    );
```

## Why

The save records learned spells by name, and this line was one of the first two fatal name lookups found in the load pipeline, alongside [save_load_status_effect_tolerance](save_load_status_effect_tolerance.md). The later static sweep completed the enumeration, and the whole `save_load_*` family in the [Catalog](../CATALOG.md) covers the rest. Vanilla's own perks, items, and recipes lines directly beside it use the tolerant `try_` variants, so this is the engine's inconsistency, not its policy. One unknown name, such as a custom spell whose mod was uninstalled, aborts the load natively. There is no dialog and nothing in any log. The game returns to the title screen.

The fix swaps the functor for `try_string_to_spell`, which returns `undefined` for unknown names, routing unknown spells through `deserialize_array_bool`'s existing skip. That is the same path perks already take. [save_load_forget_warn](save_load_forget_warn.md) names the dropped spell in the log. Re-saving writes the cleansed list, and reinstalling the mod before re-saving restores the spell untouched.

Zero-registrant inert: for a name that resolves, the `try_` variant returns the same ordinal as the fatal one, so an intact install is behaviorally identical.
