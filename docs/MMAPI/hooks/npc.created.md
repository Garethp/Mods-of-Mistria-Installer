# Hook: npc.created

Customize each villager instance as it spawns, fully initialized.

`npc.created` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `spawn_npc()`, once per NPC instance the engine creates, after the instance's create event chain and its `initialize(npc)` call have both run. ctx is the `par_NPC` instance. Its `Npc` data struct is `ctx.me`, its `NpcId` is `ctx.npc_id`, and its FSM is live.

NPC instances are transient. The engine destroys and recreates them constantly. Entering a room an NPC is in (`npcs_on_room_start`), an NPC walking into the current room (`npcs_on_step`), and a schedule time jump landing an NPC in the current location all spawn a fresh instance. Expect many fires per NPC per day, and re-apply per-instance work on every fire.

| | |
| --- | --- |
| **Fires** | At the end of `spawn_npc()`, after the create event chain and `initialize(npc)` have both run. |
| **ctx** | The `par_NPC` instance. Read `ctx.npc_id`, `ctx.me`, `ctx.fsm`. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. Mutating the instance is the intended use. |

Two creation paths bypass `spawn_npc()` and never fire: the Mist cutscene actor spawn (`__actor_spawn`, which builds throwaway scene actors) and the test suite's direct instance creations.

## Usage

```gml
// npc.created is an EVENT: the return value is ignored.
// Fires on every spawn of every villager, so keep the handler fast.
function my_mod_npc_created(_npc) {
    // _npc is the par_NPC instance:
    //   .npc_id - the NpcId enum id
    //   .me     - the Npc data struct (hearts, schedule, prototype)
    //   .fsm    - the live NPC state machine
    with (_npc) {
        // A "Wave" greeting on Throw, the open input (see Available Inputs).
        self.register_interaction(
            InputId.Throw,
            "my_mod_local/wave",        // ship a localization entry for this key
            function() {
                self.me.add_heart_points(5);
                self.bark_emitter.emit(BarkId.CuteFace, BarkType.Thought);
                if (instance_exists(obj_ari)) {
                    obj_ari.face_dir(point_direction(obj_ari.x, obj_ari.y, self.x, self.y));
                    obj_ari.set_idle_simple();
                }
            },
            function() {
                // Yield when the vanilla date invite would take the press.
                return !(self.me.can_go_on_dates() && ari_eligible_for_date(self.npc_id));
            }
        );
    }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("npc.created", my_mod_npc_created);
```

The `local_key` argument labels the button prompt through the localizer, so ship a localization entry for it, or serve the key at runtime through [local.missing](local.missing.md).

### Available Inputs

The table below describes each input's availability for mod registration given the vanilla interactions already defined.

| Input | Availability for a mod interaction |
| --- | --- |
| Interact | Contested. `talk` sits earlier and is active whenever the NPC can talk, with quest turn-ins ahead of it, so a mod interaction reaches the press only when the NPC cannot talk. Dozy, Henrietta, and sleeping Caldarus carry extra Interact registrations of their own. |
| SecondaryInteract | Open on villagers who are not partners, except Elsie, whose gossip registration is active once her quest completes. For partners the kiss contention applies (see Interactions). |
| Throw | Open with an empty hand on villagers who are not date-eligible partners. The engagement, family-planning, gift, and child registrations all require a held item or a specific family or festival state, but a partner's `invite_on_date` needs no held item, and festival days add their own asks. |
| Ride, Jump, PickUpOne, the tool inputs | Unregistered on NPCs. A take_press registration still consumes that input's global action while the prompt is active, so Jump eats jumps and the tool inputs eat swings beside the NPC. |

## Interactions

- Interactions registered here land after every vanilla registration, and `attempt_interact()` scans in registration order with the first active interaction that takes the press winning. A handler therefore cannot shadow an active vanilla interaction on the same input, with the one exception below, though the button prompt label can still show the later registration's key. Presses on mod interactions still flow through [input.take_press](input.take_press.md).
- The exception is the partner kiss. It registers with `take_press = false`, so it reads the press without consuming it, and a later SecondaryInteract registration that takes the press replaces it in the same scan. Therefore, a mod interaction on SecondaryInteract overrides the kiss interaction for partner NPCs unless its condition returns false for them. This is the preferred way to replace partner SecondaryInteract behavior wholesale, prompt text included, and it is fully reversible, since a condition returning false hands the input straight back to the untouched vanilla kiss interaction.
- The FSM's initial state at spawn never dispatches [fsm.transition](fsm.transition.md), so this hook is the place to see (or change) an NPC's spawn-time state.

## Engine Wiring

- Seam [`npc_created`](../seams/npc_created.md) dispatches from `gml/scripts/GameplaySystems/NPCs/NpcDatabase.gml`, between `spawn_npc()`'s `initialize(npc)` call and its return.

## See Also

- [pet.created](pet.created.md) - The same moment for the farm pet.
- [animal.created](animal.created.md) - The same moment for barn/coop animals.
- [npc.gift_received](npc.gift_received.md) - An NPC receives a gift.
- [input.take_press](input.take_press.md) - Veto a registered interaction's press.
