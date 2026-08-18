# Hook: quest.complete

Know when a quest is completed.

`quest.complete` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `QuestLog.complete(quest_name)`, after the completion is validated (the quest is active and its final task is done) and before any completion bookkeeping. The `T2R` quest facts, the active-to-completed move, the request-board cleanup, the pending renown entry, and the achievements refresh all land after the emit. Calls that fail validation return early and never fire, so every emit is a genuine completion.

ctx is `{ quest_name, quest }`. `quest` is the `ActiveQuest` being completed. Its prototype is `quest.quest` and its rewards `quest.quest.reward_list`. This hook is observation only. At emit time the quest still counts as active (`QUEST_LOG.active` holds it, `QUEST_LOG.completed` does not), which is how a handler can distinguish "completing right now" from "already completed".

| | |
| --- | --- |
| **Fires** | Inside `QuestLog.complete()`, after validation, before the completion bookkeeping. |
| **ctx** | `{ quest_name, quest }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `quest_name` - the quest's key string.
- `quest` - the `ActiveQuest` being completed. `quest.quest` is the quest prototype, `quest.quest.reward_list` its rewards, and `quest.current_stage` the finished stage count.

## Usage

```gml
// quest.complete is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function quest_scribe_quest_complete(_ctx) {
    // _ctx is { quest_name, quest }.
    //   .quest_name - the quest's key string.
    //   .quest      - the ActiveQuest. .quest.quest is the prototype,
    //                 .quest.quest.reward_list its rewards.
    // Validation has passed: this is a real completion, about to be recorded.
    // if (_ctx.quest_name == "<your quest key>") { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("quest.complete", quest_scribe_quest_complete);
```

## Engine Wiring

- Seam [`quest_complete`](../seams/quest_complete.md) dispatches from `gml/scripts/GameplaySystems/Quests/QuestLog.gml`, inside `complete()` below the validation early-return, anchored on the first `T2R` fact write.

## See Also

- [player.renown_delta](player.renown_delta.md) - This filter is where a renown-rewarding quest's pending entry lands at day rollover.
- [request_board.fetch_pool](request_board.fetch_pool.md) - Change the request board's daily candidate pool and draw cap.
- [museum.donate_item](museum.donate_item.md) - This event is the other progression moment that queues a pending renown entry.
