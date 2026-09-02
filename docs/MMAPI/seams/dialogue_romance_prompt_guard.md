# Seam: dialogue_romance_prompt_guard

Puts a veto check beside the vanilla marriage lock as a pink prompt is styled.

`dialogue_romance_prompt_guard` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [dialogue.romance_prompt_guard](../hooks/dialogue.romance_prompt_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/TextboxMenu.gml` |
| **Locator** | text anchor on the married or engaged lock block inside the prompt boxes' `reset_sprites` closure |
| **Feeds** | [`dialogue.romance_prompt_guard`](../hooks/dialogue.romance_prompt_guard.md) |
| **ctx built** | `{ npc_id, speaker, path }` from the open textbox menu's `current_speaker` and `driver` |
| **Marker** | `mmapi_dialogue_run_romance_prompt_guards` |

## The Edit

The pristine closure styles a pink prompt and then locks it when `!ARI.disable_break_ups && (ARI.spouse() != undefined || ARI.fiance() != undefined)` holds. The replace captures that condition into a local, and only when it is false dispatches the guard with a ctx built from `ANCHOR.get_menu(Menu.Textbox)`. The speaker's `identity` string resolves to an `NpcId` through `try_string_to_npc_id`, which yields undefined for cameo speakers, and `path` reads the driver's `conversation_name`. A guard veto sets the same local, and the original four lock statements run off it unchanged.

With zero handlers the guard check returns true, the local keeps the vanilla value, and the styled result is identical to pristine. The dispatch sits inside its own try/catch, so a failure in ctx construction or dispatch counts as allow.

The guard never dispatches when the vanilla condition already locks the prompt. Mods add lock reasons and cannot remove the vanilla lock.

## Edge Cases

- `reset_sprites` runs when the prompts slide in and again during the cleanup after a selection, while the pink flag is still set. Both passes evaluate the lock, and a veto in the cleanup pass re-inserts `stay_locked` exactly as the vanilla condition does for a married player. The hook page tells handlers to answer from stable state so both passes agree.
- `current_speaker` can be undefined for info line asks, and `driver` is undefined outside a driven conversation. The ctx fields degrade to undefined rather than failing, and a crash inside the ctx build is caught and counts as allow.

## See Also

- [dialogue.romance_prompt_guard](../hooks/dialogue.romance_prompt_guard.md) - This is the hook this seam dispatches.
- [dialogue_play_guard](dialogue_play_guard.md) - Veto a whole conversation before it starts.
- [dialogue_npc_blip](dialogue_npc_blip.md) - Filters an NPC speaker's blip sound from this same file.
