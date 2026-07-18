# Catalog

[← MMAPI](MMAPI.md)

Every named hook the seam catalog declares has its own page, as does every seam, engine fix, and call rewrite behind them. The catalog currently declares **87 hooks**, fed by **93 seams**, **2 engine fixes**, and **1 call rewrite**. The authoritative source for all of it is the seam catalog itself, `ModsOfMistriaInstallerLib/Seam/Payload/seams.toml`. See [Seams](SEAMS.md).

Each hook has exactly one kind, and each kind has one registration directive. A handler registered with the wrong directive never runs and produces only a warning in the MMAPI log. See [Hooks](HOOKS.md).

| Kind | Directive | The callback |
| ---- | --------- | ------------ |
| event | `mmapi_on` | Reacts to a moment; return ignored. The individual contract may allow context mutation. |
| filter | `mmapi_filter` | Receives `(value, ctx)`; normally returns a replacement or `undefined` to keep. In-place hooks say when to mutate instead. |
| guard | `mmapi_guard` | The Boolean value `false` vetoes; every other value allows. |
| override | `mmapi_override` | First non-`undefined` return replaces the engine's answer. |

## Hooks

### Game Flow And World State

| Name | Kind | Description |
| ---- | ---- | ----------- |
| [game.clock_tick](hooks/game.clock_tick.md) | event | Know every frame the game clock ticks, even while paused. |
| [game.day_started](hooks/game.day_started.md) | event | Know the moment a new day has begun. |
| [game.room_changed](hooks/game.room_changed.md) | event | Know when the player has landed in a different room. |
| [game.room_transition_pre](hooks/game.room_transition_pre.md) | event | React to a room transition before it starts, and redirect it. |
| [game.room_transition_post](hooks/game.room_transition_post.md) | event | Know the moment a room transition has finished, destination settled. |
| [game.save_guard](hooks/game.save_guard.md) | guard | Block a game save before anything is written. |
| [game.title_entered](hooks/game.title_entered.md) | event | Know when the game returns to the title screen. |
| [save.game_loaded](hooks/save.game_loaded.md) | event | Know the moment a save file starts loading. |
| [save.game_saving](hooks/save.game_saving.md) | event | Know the moment the game commits to writing a save. |
| [clock.time_advance](hooks/clock.time_advance.md) | filter | Adjust or freeze how much game time passes each frame. |
| [camera.culls_processed](hooks/camera.culls_processed.md) | event | React the instant culling reactivates on-screen renderers, before they draw. |
| [dungeon.runner_created](hooks/dungeon.runner_created.md) | event | Know the moment a dungeon run begins. |
| [dungeon.floor_enter](hooks/dungeon.floor_enter.md) | event | Know the moment a dungeon floor is entered, before its room builds. |
| [dungeon.room_build_begin](hooks/dungeon.room_build_begin.md) | event | Know the last moment before a dungeon room is built. |
| [dungeon.floor_built](hooks/dungeon.floor_built.md) | event | Know the moment a dungeon floor's room is fully built. |
| [dungeon.ladder_spawn](hooks/dungeon.ladder_spawn.md) | guard | Block the descent ladder before it spawns. |
| [dungeon.side_room_chance](hooks/dungeon.side_room_chance.md) | filter | Adjust the odds of dungeon side rooms. |
| [dungeon.treasure_chest](hooks/dungeon.treasure_chest.md) | event | Know the moment a treasure chest starts its drop chain. |
| [interact.elevator_action](hooks/interact.elevator_action.md) | guard | Block the dungeon elevator before its menu opens. |
| [interact.ladder_down_action](hooks/interact.ladder_down_action.md) | guard | Stop a dungeon ladder descent before it starts. |
| [resource.node_modifier](hooks/resource.node_modifier.md) | filter | Change the charged-tool modifier on picks and chops. |
| [furniture.place_guard](hooks/furniture.place_guard.md) | guard | Veto a furniture placement before it is written. |
| [object.interact](hooks/object.interact.md) | override | Take over any grid object's interaction. |
| [object.node_sprite](hooks/object.node_sprite.md) | filter | Swap the sprite of any world node before it draws. |
| [store.item_added](hooks/store.item_added.md) | event | Know when an item lands in the shopping basket. |
| [gossip.selections](hooks/gossip.selections.md) | filter | Change which NPCs the day's gossip offers. |

### Player, Actors, And Progression

| Name | Kind | Description |
| ---- | ---- | ----------- |
| [player.health_delta](hooks/player.health_delta.md) | filter | Change every player health gain or loss before it lands. |
| [player.stamina_delta](hooks/player.stamina_delta.md) | filter | Change every stamina cost or gain before it applies. |
| [player.incoming_damage](hooks/player.incoming_damage.md) | filter | Change the final damage a hit deals the player. |
| [player.move_speed](hooks/player.move_speed.md) | filter | Change the player's move speed after every engine modifier. |
| [player.equipment_bonus](hooks/player.equipment_bonus.md) | filter | Adjust the bonus an equipment infusion grants the player. |
| [player.max_health_item](hooks/player.max_health_item.md) | event | Know when an item permanently raises Ari's max health. |
| [player.heal_vfx](hooks/player.heal_vfx.md) | guard | Block the player's heal sparkle before it plays. |
| [player.status_effect_register](hooks/player.status_effect_register.md) | filter | Rewrite a status effect as it registers. |
| [player.status_effect_cancel](hooks/player.status_effect_cancel.md) | event | Know when the game cancels a status effect. |
| [player.status_effect_expired](hooks/player.status_effect_expired.md) | event | Know the moment a status effect runs out. |
| [npc.heart_points](hooks/npc.heart_points.md) | filter | Adjust the heart points a villager gains. |
| [npc.gift_received](hooks/npc.gift_received.md) | event | Know when the player gives an NPC a gift. |
| [animal.heart_points](hooks/animal.heart_points.md) | filter | Adjust the heart points a barn animal gains. |
| [animal.pet](hooks/animal.pet.md) | event | Know when the player pets or puts down an animal. |
| [combat.damage](hooks/combat.damage.md) | filter | Change any hit before it resolves. |
| [combat.damage_resolved](hooks/combat.damage_resolved.md) | event | Know the moment a hit lands or is blocked. |
| [combat.damage_injected](hooks/combat.damage_injected.md) | event | Know when a mod injects a hit through the damage pipeline. |
| [combat.tarball_grid](hooks/combat.tarball_grid.md) | filter | Change what a swing can pick, chop, or destroy. |
| [monster.spawn](hooks/monster.spawn.md) | filter | Change, move, or cancel any monster spawn. |
| [monster.death](hooks/monster.death.md) | event | Know the moment a monster dies. |
| [monster.step_begin](hooks/monster.step_begin.md) | event | React to every monster, every frame, right after its aggro update. |
| [monster.draw](hooks/monster.draw.md) | event | React to every monster's draw with your own world-space visuals. |
| [monster.shroom.should_hide](hooks/monster.shroom.should_hide.md) | guard | Stop shroom monsters from hiding. |
| [monster.spirit_projectile.step](hooks/monster.spirit_projectile.step.md) | guard | Stop a spirit projectile mid-flight. |
| [spells.can_cast](hooks/spells.can_cast.md) | override | Take over whether a spell can be cast. |
| [spells.cast](hooks/spells.cast.md) | override | Replace a spell's cast with your own behavior. |
| [spells.cast_done](hooks/spells.cast_done.md) | event | Know when a spell cast completes. |
| [spells.cost](hooks/spells.cost.md) | filter | Change a spell's mana cost everywhere the engine reads it. |
| [fsm.transition](hooks/fsm.transition.md) | filter | Redirect or cancel any state transition in the game's shared FSMs. |

### Items And Crafting

| Name | Kind | Description |
| ---- | ---- | ----------- |
| [items.give](hooks/items.give.md) | filter | Rewrite any item the player is about to receive. |
| [items.use_guard](hooks/items.use_guard.md) | guard | Block an item from being used. |
| [items.consumed](hooks/items.consumed.md) | event | Know every item the player eats. |
| [items.dropped](hooks/items.dropped.md) | event | Know what is about to drop into the world. |
| [items.trashed](hooks/items.trashed.md) | event | Know the moment the player trashes an item. |
| [items.dig_artifact](hooks/items.dig_artifact.md) | filter | Swap the artifact an archaeology dig spot yields. |
| [items.treasure_distribution](hooks/items.treasure_distribution.md) | filter | Change what the dungeon treasure roll drops. |
| [items.infusion_generate](hooks/items.infusion_generate.md) | guard | Stop a recipe from rolling infusions. |
| [item.display_description](hooks/item.display_description.md) | filter | Reword the description an item's tooltip renders. |
| [crafting.max_crafts](hooks/crafting.max_crafts.md) | override | Take over how many of a recipe can be crafted. |
| [crafting.pay_component_costs](hooks/crafting.pay_component_costs.md) | guard | Veto a recipe's material payment, craft for free. |

### UI, Text, And Presentation

| Name | Kind | Description |
| ---- | ---- | ----------- |
| [ui.menu_opened](hooks/ui.menu_opened.md) | event | Know the moment a menu opens. |
| [ui.menu_closed](hooks/ui.menu_closed.md) | event | Know when a menu closes. |
| [ui.menu_refreshed](hooks/ui.menu_refreshed.md) | event | React when a menu rebuilds its content. |
| [ui.toolbar_tick](hooks/ui.toolbar_tick.md) | event | React on every toolbar tick. |
| [ui.draw_gui](hooks/ui.draw_gui.md) | event | React to every GUI draw with your own overlay. |
| [ui.hud_should_show](hooks/ui.hud_should_show.md) | filter | Change whether the HUD shows. |
| [ui.item_icon](hooks/ui.item_icon.md) | filter | Swap the sprite an item shows as its icon. |
| [ui.item_node](hooks/ui.item_node.md) | filter | Adjust UI item slots as they are populated. |
| [ui.button_sprites](hooks/ui.button_sprites.md) | filter | Swap the sprite set a UI button is built from. |
| [ui.sprite](hooks/ui.sprite.md) | filter | Swap the backplate sprites behind the mines menu and spell cards. |
| [dialogue.play_guard](hooks/dialogue.play_guard.md) | guard | Block a conversation before it starts. |
| [dialogue.path](hooks/dialogue.path.md) | filter | Change which conversation plays before it starts. |
| [dialogue.line](hooks/dialogue.line.md) | filter | Reword any dialogue line before the textbox shows it. |
| [dialogue.speaker](hooks/dialogue.speaker.md) | filter | Swap the speaker a textbox shows. |
| [dialogue.npc_blip](hooks/dialogue.npc_blip.md) | filter | Swap the blip sound an NPC speaks with. |
| [audio.play_guard](hooks/audio.play_guard.md) | guard | Block any sound effect before it plays. |
| [audio.music_selector](hooks/audio.music_selector.md) | filter | Swap the dungeon biome music track. |
| [local.get](hooks/local.get.md) | filter | Reword any localized text the game looks up. |
| [local.missing](hooks/local.missing.md) | filter | Change what a missing localization key resolves to. |
| [input.check_value_id](hooks/input.check_value_id.md) | filter | Swap the input id `Input.check_value()` looks up. |
| [input.take_press](hooks/input.take_press.md) | guard | Block an interactable's press before the interaction runs. |

## Seams

The anchored engine edits that make the hooks fire. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](SEAMS.md) for how the catalog is applied, verified, and re-verified on game updates.

### Game Flow And World State

| Name | Description |
| ---- | ----------- |
| [game_clock_tick](seams/game_clock_tick.md) | Emits the every-frame clock tick from the head of `Clock.update()`. |
| [clock_time_advance](seams/clock_time_advance.md) | Routes each frame's game-seconds advance through the filter chain before it reaches the clock's buffer. |
| [taxi_room_transition_pre](seams/taxi_room_transition_pre.md) | Builds the room-transition ctx at the head of `taxi_player()` and writes handler edits back onto the itinerary. |
| [taxi_room_transition_post](seams/taxi_room_transition_post.md) | Re-reads the live itinerary at the end of the taxi transition and emits the arrival event. |
| [game_save_guard](seams/game_save_guard.md) | Puts a veto check at the head of `save_game()`, before anything is written. |
| [save_game_saving](seams/save_game_saving.md) | Announces an imminent save, right after the engine records the save path. |
| [save_game_loaded](seams/save_game_loaded.md) | Announces the start of a save load, right after the save path is recorded. |
| [camera_culls_processed](seams/camera_culls_processed.md) | Emits the end-of-cull moment so mods can refresh renderers the camera just reactivated. |
| [dungeon_runner_created](seams/dungeon_runner_created.md) | Emits the birth of a dungeon run, after `DUNGEON_RUNNER` is constructed and before the first floor loads. |
| [dungeon_floor_bracket](seams/dungeon_floor_bracket.md) | Brackets dungeon floor entry with three emits: floor enter, room-build begin, and floor built. |
| [dungeon_ladder_spawn](seams/dungeon_ladder_spawn.md) | Puts a veto check at the head of `spawn_ladder()`, before a floor's exit ladder appears. |
| [dungeon_side_room_chance](seams/dungeon_side_room_chance.md) | Routes the side-room spawn chance through the filter chain before the per-floor roll. |
| [dungeon_treasure_chest](seams/dungeon_treasure_chest.md) | Emits the moment a dungeon treasure chest starts its drop chain. |
| [interact_elevator_action](seams/interact_elevator_action.md) | Puts a veto check on the elevator's interaction action, the press that opens the lift menu. |
| [interact_ladder_down_action](seams/interact_ladder_down_action.md) | Puts a veto check on the ladder's descend action, before the sound and the floor change. |
| [pick_node_modifier](seams/pick_node_modifier.md) | Filters the tool modifier at the head of every pick action. |
| [chop_node_modifier](seams/chop_node_modifier.md) | Filters the tool modifier at the head of every chop action. |
| [furniture_place_guard](seams/furniture_place_guard.md) | Puts a veto check in front of every furniture placement. |
| [object_interact](seams/object_interact.md) | Puts a claim-scoped override in front of every grid-object interaction. |
| [node_renderer_set_sprite](seams/node_renderer_set_sprite.md) | Filters the sprite every world node renderer is about to wear. |
| [store_item_added](seams/store_item_added.md) | Announces every shelf tap that puts an item in the shopping basket. |
| [gossip_selections](seams/gossip_selections.md) | Wraps the gossip picker so the day's NPC selection passes through a filter. |

### Player, Actors, And Progression

| Name | Description |
| ---- | ----------- |
| [player_health_delta](seams/player_health_delta.md) | Filters the signed health delta at the top of `Ari.modify_health()`. |
| [player_stamina_delta](seams/player_stamina_delta.md) | Filters the signed stamina delta before the stamina cost modifier applies. |
| [player_incoming_damage](seams/player_incoming_damage.md) | Rewrites the player's damage drain so mods filter the final damage and its popup and flinch side effects. |
| [player_move_speed](seams/player_move_speed.md) | Filters the player's computed move speed after the status-effect multipliers. |
| [player_equipment_bonus](seams/player_equipment_bonus.md) | Rewrites the equipment bonus lookup's return into a filtered return. |
| [player_max_health_item](seams/player_max_health_item.md) | Emits right after an item raises the player's base health. |
| [player_heal_vfx](seams/player_heal_vfx.md) | Puts a veto check at the head of `play_heal_vfx()`. |
| [player_status_effect_register](seams/player_status_effect_register.md) | Filters every status effect's fields at the top of `register()`. |
| [player_status_effect_cancel](seams/player_status_effect_cancel.md) | Emits at the head of `StatusEffectManager.cancel()`, before any lookup. |
| [player_status_effect_expired](seams/player_status_effect_expired.md) | Emits inside `update()`'s expiry branch, right after the effect is removed. |
| [npc_heart_points](seams/npc_heart_points.md) | Reroutes every villager heart-point delta through a filter before it lands. |
| [npc_receive_gift](seams/npc_receive_gift.md) | Announces every gift the moment an NPC receives it. |
| [animal_heart_points](seams/animal_heart_points.md) | Reroutes every barn-animal heart-point delta through a filter before it lands. |
| [animal_on_pet](seams/animal_on_pet.md) | Announces the moment the player pets a barn animal. |
| [animal_put_down](seams/animal_put_down.md) | Announces the moment a held animal is set back down. |
| [combat_damage_pre](seams/combat_damage_pre.md) | Threads every enqueued hit through a damage filter before it resolves. |
| [combat_damage_resolved](seams/combat_damage_resolved.md) | Announces the outcome of every hit the receiver's resolution switch lands or blocks. |
| [combat_tarball_grid](seams/combat_tarball_grid.md) | Hands every active swing's tarball to mods before the grid pick/chop/destroy blocks read it. |
| [monster_spawn](seams/monster_spawn.md) | Intercepts `spawn_monster()` so mods can move, replace, or cancel every monster spawn. |
| [monster_death](seams/monster_death.md) | Emits the moment a monster dies, one line before its instance is destroyed. |
| [monster_step_begin](seams/monster_step_begin.md) | Emits once per monster per frame, right after the aggro update. |
| [monster_draw](seams/monster_draw.md) | Emits at the end of every monster's world-space draw. |
| [monster_shroom_should_hide](seams/monster_shroom_should_hide.md) | Puts a veto check at the head of the shroom's hide decision. |
| [monster_spirit_projectile_step](seams/monster_spirit_projectile_step.md) | Puts a destroy-on-veto check into the spirit projectile's step. |
| [spells_can_cast](seams/spells_can_cast.md) | Puts an override at the head of `can_cast_spell()`. |
| [spells_cast_override](seams/spells_cast_override.md) | Puts an override at the head of `cast_spell()` that can consume the whole cast. |
| [spells_cast_done](seams/spells_cast_done.md) | Emits at the end of the engine's `cast_spell()`. |
| [spells_cost_can_cast](seams/spells_cost_can_cast.md) | Filters the mana-cost read inside `can_cast_spell()`'s mana check. |
| [spells_cost_menu](seams/spells_cost_menu.md) | Filters the mana-cost read behind the spellcasting menu's cost display. |
| [spells_cost_fsm_loop](seams/spells_cost_fsm_loop.md) | Filters the mana deduction in the player's looping cast state. |
| [spells_cost_fsm_default](seams/spells_cost_fsm_default.md) | Filters the mana deduction in the player's default cast state. |
| [fsm_transition](seams/fsm_transition.md) | Filters every executed shared-FSM state transition through one funnel. |

### Items And Crafting

| Name | Description |
| ---- | ----------- |
| [items_give](seams/items_give.md) | Routes every item grant through a struct filter at the head of the engine's one give-item entry point. |
| [items_use_guard](seams/items_use_guard.md) | Puts a veto check in front of every item use, right after the LiveItem coercion. |
| [items_consumed](seams/items_consumed.md) | Announces every item the player eats, right after the stat is recorded. |
| [items_dropped](seams/items_dropped.md) | Announces every drop before the items spawn into the world. |
| [inventory_trash_button](seams/inventory_trash_button.md) | Announces a journal-inventory trash tap while the slot still holds the item. |
| [storage_trash_button](seams/storage_trash_button.md) | Announces a chest trash tap while the slot still holds the item. |
| [archaeology_dig_artifact](seams/archaeology_dig_artifact.md) | Wraps the artifact roll so every dig spot's yield passes through a filter. |
| [items_treasure_distribution_none](seams/items_treasure_distribution_none.md) | Filters the treasure roll's empty exit so mods can inject a drop where there was none. |
| [items_treasure_distribution_result](seams/items_treasure_distribution_result.md) | Filters the treasure roll's rolled result on its way out. |
| [items_infusion_generate](seams/items_infusion_generate.md) | Puts a veto check in front of a recipe's infusion generation. |
| [item_display_description](seams/item_display_description.md) | Wraps the item-description getter, the string the tooltip body actually renders. |
| [crafting_max_crafts](seams/crafting_max_crafts.md) | Puts an override in front of the craft-count ceiling before the engine computes it. |
| [crafting_pay_component_costs](seams/crafting_pay_component_costs.md) | Puts a veto check in front of a recipe's material payment. |

### UI, Text, And Presentation

| Name | Description |
| ---- | ----------- |
| [ui_menu_opened](seams/ui_menu_opened.md) | Emits the moment the anchor spawns a menu onto `open_menus`. |
| [ui_menu_closed_drain](seams/ui_menu_closed_drain.md) | Emits when the per-frame drain removes a free-requested menu. |
| [ui_menu_closed_shutdown](seams/ui_menu_closed_shutdown.md) | Emits per menu as the anchor shuts down and closes everything. |
| [ui_toolbar_refreshed](seams/ui_toolbar_refreshed.md) | Emits at the tail of `ToolbarMenu.update()`, after the slots re-resolve from the inventory. |
| [ui_vitals_refreshed](seams/ui_vitals_refreshed.md) | Emits at the tail of `VitalsMenu.refresh_statuses()`, after the status icon strip rebuilds. |
| [ui_toolbar_tick](seams/ui_toolbar_tick.md) | Emits every toolbar tick, between the subscriber pull and press-and-hold processing. |
| [ui_draw_gui](seams/ui_draw_gui.md) | Emits on every GUI draw, right after the anchor draws the UI. |
| [ui_hud_should_show](seams/ui_hud_should_show.md) | Wraps `hud_should_show()` so mods get the last word on HUD visibility. |
| [ui_item_icon_live_item](seams/ui_item_icon_live_item.md) | Wraps `LiveItem.get_ui_icon()` so every item icon lookup is filterable. |
| [ui_item_icon_obj_item_world](seams/ui_item_icon_obj_item_world.md) | Filters the world-drop item sprite and computes the outline its companion draws. |
| [obj_item_outline_sprite](seams/obj_item_outline_sprite.md) | Reroutes the world-item outline draw to the outline its sibling seam computes. |
| [ui_item_node_set_to_item](seams/ui_item_node_set_to_item.md) | Hands every populated UI item node to mods, right after its icon is set. |
| [ui_item_node_crafting_menu](seams/ui_item_node_crafting_menu.md) | Hands each crafting-grid icon node to mods as the menu builds. |
| [ui_button_sprites](seams/ui_button_sprites.md) | Puts a filter on each built button sprite set before it enters the cache. |
| [ui_sprite_mines_backplate](seams/ui_sprite_mines_backplate.md) | Routes the mines menu backplate sprite through a filter on dungeon room start. |
| [ui_sprite_spell_card_backplate](seams/ui_sprite_spell_card_backplate.md) | Routes each spell card's backplate sprite through a filter. |
| [dialogue_play_guard](seams/dialogue_play_guard.md) | Puts a veto check at the head of `play_conversation()`. |
| [dialogue_path](seams/dialogue_path.md) | Rebuilds `play_conversation()`'s four arguments through the `dialogue.path` filter. |
| [dialogue_line](seams/dialogue_line.md) | Filters each localized dialogue line before the textbox shows it. |
| [dialogue_speaker](seams/dialogue_speaker.md) | Filters the just-built textbox speaker before it is assigned. |
| [dialogue_speaker_ctx_arg](seams/dialogue_speaker_ctx_arg.md) | Threads the ConversationDriver into the initial Speaker action so `dialogue.speaker`'s ctx is filled from line one. |
| [dialogue_npc_blip](seams/dialogue_npc_blip.md) | Filters an NPC speaker's blip sound right after the default lookup. |
| [audio_play_guard](seams/audio_play_guard.md) | Puts a veto check at the head of the engine's one sound-effect entry point. |
| [audio_music_selector](seams/audio_music_selector.md) | Puts a filter on the dungeon biome music track as the scene selector picks it. |
| [input_check_value_id](seams/input_check_value_id.md) | Puts a filter on the input id at the head of the engine's input value lookup. |
| [input_take_press](seams/input_take_press.md) | Puts a veto between an interaction's pressed read and the interaction running. |

### Engine Fixes And The Call Rewrite

Hook-less edits the catalog also carries:

| Name | Kind | Description |
| ---- | ---- | ----------- |
| [game_step_begin_installs](seams/game_step_begin_installs.md) | engine fix | Installs the MMAPI per-frame drain at the top of the game's `step_begin`, the framework's lifecycle root. |
| [shroom_puddle_mask](seams/shroom_puddle_mask.md) | engine fix | Corrects the acid puddle's damage-tarball collision mask, a beta-wiring fix. |
| [local_get_dispatch](seams/local_get_dispatch.md) | call rewrite | Reroutes every direct GML `local_get()` call through the framework's localisation waist, feeding [local.get](hooks/local.get.md) and [local.missing](hooks/local.missing.md). |

## Growing The Catalog

Hooks exist because real mods needed them. If nothing here covers your case, first check whether a direct engine call or a registered tick can do it. See [Calling The Engine Directly](API_REFERENCE.md#calling-the-engine-directly) and [Prove A New Hook Is Needed](SEAMS.md#1-prove-a-new-hook-is-needed).

For a request, describe the game moment and what your mod should be able to observe, change, block, or replace. For a contribution, the [Seam Authoring Reference](SEAMS.md#authoring-a-hook-and-seam) covers the catalog schema, template and text forms, locators, generated ops, dependencies, verification commands, and matching documentation work.
