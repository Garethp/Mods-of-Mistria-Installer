# Seam: dialogue_npc_blip

Filters an NPC speaker's blip sound right after the default lookup.

`dialogue_npc_blip` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [dialogue.npc_blip](../hooks/dialogue.npc_blip.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/TextboxMenu.gml` |
| **Locator** | text anchor in the `NpcSpeaker(id)` constructor, on the `blip_sound` default assignment |
| **Feeds** | [`dialogue.npc_blip`](../hooks/dialogue.npc_blip.md) |
| **Value filtered** | `self.blip_sound`, the default from `find_npc_blip_noise(self.id)` |
| **ctx built** | `{ npc_id: self.id, speaker: self }` |
| **Marker** | `mmapi_dialogue_run_npc_blip_filters` |

## The Edit

The `NpcSpeaker` constructor resolves the NPC's data (`self.me`), identity string, and default blip noise via `find_npc_blip_noise(self.id)`. The replace appends one line after that default: `self.blip_sound = mmapi_apply_filters("dialogue.npc_blip", self.blip_sound, { npc_id: self.id, speaker: self })` in a try/catch. The filter sees the engine's default and can swap in a custom voice blip per NPC, and because it runs inside the constructor, every `NpcSpeaker`, however it is created, carries the filtered sound.

Cameo speakers construct through the separate `CameoSpeaker` and do not pass through this dispatch.

## See Also

- [dialogue.npc_blip](../hooks/dialogue.npc_blip.md) - This is the hook this seam dispatches.
- [dialogue_speaker](dialogue_speaker.md) - Replace the speaker itself, blip and all.
- [dialogue_line](dialogue_line.md) - Filter the text the blips play under.
