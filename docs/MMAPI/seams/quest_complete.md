# Seam: quest_complete

Emits inside `QuestLog.complete()` once the completion is validated, before the bookkeeping runs.

`quest_complete` is a **template seam** (`op = "emit"`). It feeds [quest.complete](../hooks/quest.complete.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Quests/QuestLog.gml` |
| **Locator** | structural target: `complete`, at before `T2R.write(format("quest_{}_in_progress", quest_name), false);` |
| **Op** | `emit` |
| **Feeds** | [`quest.complete`](../hooks/quest.complete.md) |
| **ctx built** | `{ quest_name: quest_name, quest: finished_quest }` |
| **Marker** | `mmapi_quest_complete` |

## The Edit

The generated emit lands inside the `QuestLog` struct's `complete(quest_name)`, anchored on the first `T2R` fact write. It calls `mmapi_emit("quest.complete", { quest_name: quest_name, quest: finished_quest })` in the uniform try/catch shape. The anchor choice, below the validation early-return rather than at head, is the point of the seam. `complete()` bails with `undefined` when the quest is not active or its final task is not done (defensive re-completes from the debug CLI, or mod GML, land there), and only calls that pass fire the hook. It also puts `finished_quest`, the looked-up `ActiveQuest`, in scope and non-`undefined` for the ctx.

Everything that makes the completion real happens after the emit: both `T2R` quest facts, the active-to-completed move, the completion timestamp, the request-board cleanup, the pending renown entry for renown-rewarding quests, and the achievements refresh. Every engine completion routes through this one method: stage progression, festivals, the seal tablet, renown level quests, and the debug CLI alike. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [quest.complete](../hooks/quest.complete.md) - This is the hook this seam dispatches.
- [player_renown_delta](player_renown_delta.md) - This is the filter a renown-rewarding quest's pending entry drains through.
- [request_board_fetch_pool_ready](request_board_fetch_pool_ready.md) - This is the emit of the finished daily request board.
- [museum_donate_item](museum_donate_item.md) - This is the other progression emit that queues a pending renown entry.
