# Engine Fix: game_stats_seed_on_load

Seeds the per-name game-stats structs on every load, so content added after a save was written cannot crash its first stats increment.

`game_stats_seed_on_load` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `GAME_STATS` restore |
| **Op** | text (one added call) |
| **Marker** | `mmapi_game_stats_seed` |

## The Edit

```gml
    GAME_STATS = files.game_stats;
    patch_game_stats(GAME_STATS); // mmapi_game_stats_seed
```

## Why

`GAME_STATS`'s name-keyed structs (`perks`, `menu_opens`, `npcs_spoken_to`, `location_visits`) are seeded only at new-game and at save-version migration. Both paths call the engine's own `patch_game_stats`, which fills a `0` for every current enum member's key. A same-version save takes neither path, so it never gains keys for content installed after it was written, and the engine's roughly 90 raw `[$ key] += 1` sites crash with `bad unary op "inc" undefined` on first touch.

The first arrival in a mod-added location dies at Taxi's `location_visits` increment. The same hazard exists for custom NPCs at the `npcs_spoken_to` increment on their first conversation, and at the two stat sites that increment a dynamically chosen perk.

The fix invokes `patch_game_stats` on every load. It is the engine's own migration function. It is idempotent, filling only keys that are `undefined` and prototype-patching missing fields. It is cheap, costing a few hundred key probes. It retires the whole class at one site instead of guarding roughly 90 increments.

Zero-registrant inert: a same-version vanilla save already carries every key, so every probe finds them defined and the call changes nothing.
