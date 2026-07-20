# Seam: dialogue_line

Filters each localized dialogue line before the textbox shows it.

`dialogue_line` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [dialogue.line](../hooks/dialogue.line.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml` |
| **Locator** | text anchor on the whole body of `deliver_line(line)` |
| **Feeds** | [`dialogue.line`](../hooks/dialogue.line.md) |
| **Value filtered** | `line.local`, the localized line text |
| **ctx built** | `{ driver: self, line: line, conversation_name: self.conversation_name, npc_id: self.npc_owner, is_info_line: self.is_info_line }` |
| **Marker** | `mmapi_dialogue_run_local_filters` |

## The Edit

The pristine `deliver_line(line)` hands `line.local` straight to one of three textbox deliveries: `ask` for prompt lines, `info` for info lines, `say` for everything else. The replace captures `var local = line.local`, filters it through `mmapi_apply_filters("dialogue.line", local, { ... })` in a try/catch, and rewrites all three branches to deliver `local` instead of `line.local`. One dispatch covers prompted, info, and plain lines alike, and the line struct itself is never mutated.

The ctx carries the driver, the raw `line` struct, the conversation name, the owning `npc_id`, and the `is_info_line` flag, so a handler can reword one specific line of one specific conversation and return `undefined` for everything else.

## See Also

- [dialogue.line](../hooks/dialogue.line.md) - This is the hook this seam dispatches.
- [dialogue_speaker](dialogue_speaker.md) - Swap who is speaking instead of what is said.
- [dialogue_path](dialogue_path.md) - Redirect the whole conversation before any line plays.
- [dialogue_npc_blip](dialogue_npc_blip.md) - Swap the voice blip the line plays with.
