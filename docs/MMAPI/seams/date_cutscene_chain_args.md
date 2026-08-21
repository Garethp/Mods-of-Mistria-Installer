# Seam: date_cutscene_chain_args

Extends the date completion chain's argument list so the filtered cutscene name reaches the reward gate.

`date_cutscene_chain_args` is a **text seam** (`anchor` + `replace`). It feeds [date.cutscene](../hooks/date.cutscene.md), though it dispatches nothing itself - the dispatch lives in [date_cutscene](date_cutscene.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor on the completion chain's closing args array in `start_date_cutscene(npc, date)` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`date.cutscene`](../hooks/date.cutscene.md) (via [`date_cutscene`](date_cutscene.md)) |
| **Value filtered** | none - this edit dispatches nothing itself |
| **ctx built** | none |
| **Marker** | `mmapi_date_cutscene_chain_args` |

## The Edit

A one-line extension of the chain's bound arguments:

```gml
    }, [npc, date, item, __mmapi_date_scene]); // mmapi_date_cutscene_chain_args
```

Chain callbacks are not closures, so the engine threads locals through an explicit args array. The [date_cutscene](date_cutscene.md) seam adds a fourth parameter to the callback and hoists `__mmapi_date_scene` at the dispatch site. This edit passes the value through, completing the pair. It declares `depends_on` for the dispatch seam, and staging fails closed across the catalog, so the two edits apply together or not at all.

This is the same companion-edit pattern as [player_mana_item_delta](player_mana_item_delta.md). An edit that dispatches nothing of its own, existing only so its sibling's dispatch reaches everything it should.

## See Also

- [date.cutscene](../hooks/date.cutscene.md) - This is the hook this edit feeds.
- [date_cutscene](date_cutscene.md) - This is the dispatch this edit completes.
- [player_mana_item_delta](player_mana_item_delta.md) - The catalog's other dispatch-less companion edit.
