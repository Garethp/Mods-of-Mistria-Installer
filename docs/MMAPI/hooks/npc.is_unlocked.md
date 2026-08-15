# Hook: npc.is_unlocked

Gate a custom NPC's visibility the way the base game gates its own.

`npc.is_unlocked` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `npc_is_unlocked()`'s default arm, which covers every NPC the base game does not explicitly gate. That is every extension NPC and any future vanilla NPC without a case. The filtered value is the unlocked boolean, already `false` for ledger vacancies (uninstalled extension NPCs). ctx is the `NpcId` ordinal. Return the replacement value, or `undefined` to keep the current value.

An NPC that ends up `false` is skipped by the relationships journal *before* any of its data is read, exactly like the market vendors before the Saturday Market unlocks. Vanilla NPCs with explicit cases (Merri, Darcy, Louis, Vera, Wheedle, Taliferro, Caldarus, Seridia, Stillwell, Zorel) never reach this hook.

| | |
| --- | --- |
| **Fires** | In `npc_is_unlocked()`'s default arm, for every id without an explicit vanilla case. |
| **Value** | The unlocked boolean. Starts `true`, or `false` when the id is a ledger vacancy. |
| **ctx** | The `NpcId` ordinal being asked about. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- ctx - the `NpcId` ordinal. Compare against `NpcId.<your symbol>` or `mmapi_ext_id("npc_roster", "<symbol>")`.

## Usage

```gml
// npc.is_unlocked is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function author_mymod_luna_unlocked(_unlocked, _npc_id) {
    if (_npc_id != NpcId.author_mymod_luna) {
        return undefined;               // not ours - keep the current value
    }
    return author_mymod_progress();     // false hides the NPC entirely
}

mmapi_filter("npc.is_unlocked", author_mymod_luna_unlocked);
```

See [Journal Visibility](../EXTENSIONS.md#journal-visibility) for the full picture. This hook is the primary of three levers, alongside the `vendor` and `animal` tags.
