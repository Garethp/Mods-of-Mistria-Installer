# Hook: dialogue.conversation_finished

Know when a conversation has finished.

`dialogue.conversation_finished` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `ConversationDriver.finish_conversation()`, after the conversation's end actions have run through `process_t2_action()`, after the driver has closed its textbox or started the textbox's closing animation, and after the driver's state is set to `ConversationDriverState.Finished`. ctx is `{ driver, conversation_name, npc_id, end_actions }`.

This hook is observation only. Every entry in `end_actions` has already run, so the array is a record of what the end did.

| | |
| --- | --- |
| **Fires** | At the end of `ConversationDriver.finish_conversation()`, after the end actions, the textbox close, and the state write. |
| **ctx** | `{ driver, conversation_name, npc_id, end_actions }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `driver` - the `ConversationDriver` that finished. `driver.state` reads `ConversationDriverState.Finished`, and `driver.prompt_index_selected` is the index of the last prompt the player picked, or `undefined` when no prompt was answered.
- `conversation_name` - the full t2 conversation name inside `assets/t2`, of the form `<folder>/<file>/<table>`. For example, `Conversations/General Dialogue/time_of_day/early_morning_summer`. For a driver `play_conversation()` built, this is the path after the `dialogue.path` filter.
- `npc_id` - the driver's `npc_owner`, the NPC the conversation was started for. When talking to an NPC, it is the NPC the player spoke to. The game generally uses `NpcId.Caldarus` for object inspect lines and `NpcId.Adeline` for cutscene conversations as defaults.
- `end_actions` - the array of `T2Action` structs that `T2R.conversation_end()` returned, possibly empty. Each entry has already run.

### The drivers that fire

| Driver | Fires |
| --- | --- |
| `play_conversation()`, the normal talk path | Once, when the conversation finishes. |
| The cutscene runtime, at scene start and on every `set_conversation` | Once per conversation the scene plays. |
| A skipped cutscene | Once, from `Mist.clean_up_scene()`. |
| The debug CLI's `conversation` command | Once, with whatever `npc_id` the command named. |
| A driver dropped before it finishes | Never. |

### The end_actions array

`T2R.conversation_end()` returns two things.

- The `actions` list on the conversation's own table in the t2 file, the `[name]` table that also carries `kind`, `requires`, and `writes`. Every vanilla `cutscene` action sits there, so a conversation that starts a scene reports it here.
- The `SpokeTo` action the runtime adds for the owner.

An `actions` list on a numbered line table, `[name.N]`, runs when that line is delivered and is not repeated here.

Each entry is a struct with a `type` field holding a `T2ActionId` value and the fields that type carries. `T2ActionFactory` in `gml/scripts/GameplaySystems/T2r.gml` lists every type with its fields, and `process_t2_action()` in the same file is what each entry has already been through. The table lists the types a handler is most likely to look for.

| `type` | Fields | What Has Already Happened |
| --- | --- | --- |
| `T2ActionId.Cutscene` | `cutscene` | `MIST.request_scene()` ran for it, and the owner NPC's instance was put in `NpcState.Dummy`. |
| `T2ActionId.Quest` | `quest_name` | `QUEST_LOG.start()` ran for it and the quest notification was shown. |
| `T2ActionId.Item` | `item_id`, `count` | The item was given `count` times, or removed when `count` is negative. |
| `T2ActionId.Gold` | `amount` | `ARI.modify_gold()` ran with `amount`. |
| `T2ActionId.HeartPoints` | `npc`, `amount` | Heart points went to `npc`, or to the owner NPC when `npc` is `undefined`. |
| `T2ActionId.UnlockRecipe` | `recipe` | The recipe for that item id was unlocked. |
| `T2ActionId.SpokeTo` | `npc` | Outside a cutscene, the talk was recorded against the NPC's `talk_flag`, with the daily heart points for a first talk. |
| `T2ActionId.CanTalk` | `npc` | The NPC's `talk_flag` was set. |
| `T2ActionId.UpdateStatus` | `npc`, `status` | The relationship status of `npc` was written. |

## Usage

```gml
// dialogue.conversation_finished is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function epilogue_dialogue_conversation_finished(_ctx) {
    // _ctx is { driver, conversation_name, npc_id, end_actions }.
    //   .driver            - the ConversationDriver that finished;
    //                        .prompt_index_selected is the last prompt picked.
    //   .conversation_name - the t2 conversation name (after dialogue.path).
    //   .npc_id            - the driver's npc_owner (NpcId.Adeline for cutscenes).
    //   .end_actions       - the T2Action structs the end already ran.
    // if (_ctx.conversation_name != <the conversation you follow>) return;
    //
    // Match by table name instead: T2R.exact_gameplay_conversation("<table>") resolves a
    // gameplay_triggered table to its path and returns undefined for any other kind or an
    // unknown name (resolve once, lazily, not at boot).
    //
    // Start a quest the way a t2 quest action does:
    // process_t2_action(T2Action.Quest(<quest key>), _ctx.npc_id, _ctx.driver);
    //
    // Request a cutscene the way a t2 cutscene action does (queues if one is running):
    // process_t2_action(T2Action.Cutscene(<cutscene id>), _ctx.npc_id, _ctx.driver);
    //
    // Show a tutorial once the textbox is gone, the engine's own await_popup shape:
    // await_popup(spawn_tutorial, [Tutorial.<Name>], new_chain().append(LinkId.Await, function() {
    //     return ANCHOR.get_menu(Menu.Textbox) == undefined;
    // }));
    //
    // Start another conversation only after the textbox has left ANCHOR.open_menus:
    // new_chain().append(LinkId.Await, function() {
    //     return ANCHOR.get_menu(Menu.Textbox) == undefined;
    // }).append(LinkId.Function, function() {
    //     play_conversation(<npc id>, <conversation path>);
    // });
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dialogue.conversation_finished", epilogue_dialogue_conversation_finished);
```

## Interactions

- The finished textbox leaves `ANCHOR.open_menus` a frame or more after this hook fires. A `play_conversation()` before then throws inside the handler, and the next conversation to start clears the aborted one. Wait for `ANCHOR.get_menu(Menu.Textbox) == undefined`, as the usage example does, or for `ui.menu_closed` with `kind == Menu.Textbox`.
- A conversation a handler starts fires this hook too when it finishes, so a handler that starts one on every fire loops. Check `conversation_name` or set a flag before starting another.
- `close_callback` runs after this hook, once the textbox's closing animation has finished. When the textbox is `Hidden` or `Translating` as the conversation ends, the driver closes it immediately and the callback never runs.
- Cutscene conversations and the debug CLI's `conversation` command never pass through `dialogue.play_guard` or `dialogue.path`. A cutscene driver's `npc_id` is always `NpcId.Adeline`, and on the skip path `MIST.skip_in_progress` is still true when this hook fires.
- After a `T2ActionId.Cutscene` end action, `MIST.running` is already true when this hook fires, so a scene a handler requests joins `MIST.queued_scenes` and runs after the current one.

## Engine Wiring

- Seam [`dialogue_conversation_finished`](../seams/dialogue_conversation_finished.md) dispatches from `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml`, after the last statement of `finish_conversation()`.

## See Also

- [dialogue.play_guard](dialogue.play_guard.md) - Block a conversation before it starts.
- [dialogue.path](dialogue.path.md) - Change which conversation plays before it starts.
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [ui.menu_closed](ui.menu_closed.md) - Know when a menu closes, the textbox included.
- [ui.spawn_tutorial_guard](ui.spawn_tutorial_guard.md) - Block a tutorial popup before it spawns.
- [quest.complete](quest.complete.md) - Know when a quest is completed.
