# Hook: instance.created

Customize any object's instances as they appear, with the object named at registration.

`instance.created` is an **event** hook. Register a callback with `mmapi_on` and name the object in `opts.object`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the framework's per-frame instance poll for every live instance of the object a registration names in `opts.object`, once per instance per handler, on the first begin step at which that instance is active. ctx is the instance.

The poll runs after the instance's whole create chain, so the instance is complete and every vanilla `register_interaction` call has already landed, and an instance that destroyed itself during its create event never fires. It covers every creation path, including room placement, instances built inside `room_start`, mid-room spawns, the first room of a session, and a room that reloads onto itself.

| | |
| --- | --- |
| **Fires** | From the framework's per-frame poll, on the first begin step at which the instance is active. |
| **ctx** | The instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. Mutating the instance is the intended use. |

The registration names the object:

```gml
mmapi_on("instance.created", my_mod_fountain_created, { object: obj_world_fountain });
```

Passing a parent object watches every descendant, so `{ object: par_ledger }` reaches every store ledger. A registration without `object` is refused with a warning. `obj_node_renderer` and `par_interactable` are refused as watch targets, because the first is every grid object in the room and the second is every interactable. Grid objects have [object.interact](object.interact.md).

> [!NOTE]
> The poll is not synchronous with creation. An instance created and destroyed inside one frame never fires, and an instance that is inactive when first polled fires when it is next active.

> [!IMPORTANT]
> The cost of a registration is one scan of the watched object's live instances per frame, so watch the narrowest object that fits. An object the engine rebuilds on every room transition fires on every transition, because each room's instance is new. `obj_ari` is one example, and so is every room-placed interactable.

## Usage

```gml
// instance.created is an EVENT: the return value is ignored.
// Fires once per instance, so the interaction below lands once per fountain.
function my_mod_fountain_created(_fountain) {
    // _fountain is the obj_world_fountain instance, complete, with its
    // vanilla Interact prompt already registered.
    with (_fountain) {
        self.register_interaction(
            InputId.Throw,                            // open on every world object
            "my_mod_local/make_a_wish",               // ship a localization entry for this key
            function() {
                // The action, which runs on the press.
                ARI.inventory.remove(ItemId.AncientGoldCoin, 1);
                create_notification("my_mod_local/wish_made");   // ship a localization entry for this key too
            },
            function() {
                // The condition, which runs every frame in range, so it reads and returns.
                var _held = ARI.held_item();
                return _held != undefined && _held.item_id == ItemId.AncientGoldCoin;
            }
        );
    }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("instance.created", my_mod_fountain_created, { object: obj_world_fountain });
```

The `local_key` argument labels the button prompt through the localizer, and the notification text is a key too, so ship a localization entry for each, or serve them at runtime through [local.missing](local.missing.md).

The condition in the example gates the prompt on what Ari is holding, so it appears only while an Ancient Gold Coin is held and never competes with the fountain's own `Interact` prompt. The action consumes the coin through `ARI.inventory.remove(item_id, count)`.

### Available Inputs

The table below describes each usable input's availability for a mod interaction on a watched object.

| Input | Availability | Info |
| --- | --- | --- |
| Interact | Contested | Nearly every world object registers its own prompt here. A mod interaction reaches the press only while every earlier `Interact` condition on the instance is false. |
| SecondaryInteract | Contested | The dragonshrine, Seridia's shrine, the artifact replicator, furniture, villagers, the pet, and barn and coop animals register it, and no player state reads it outside a registration, so the input is open on every other object. |
| Throw | Contested | Only villagers and furniture register it, so the input is open on every other object. The scan runs before the held-item throw, so an active prompt takes the press and the held item stays in hand. |

Every other input is unsuitable. The jump, the mount summon, the pinned spell, and the menus read their inputs before the interactable scan, PickUpOne shares its default keys with Interact and the tool button, and a registration on the tool input takes the press away from the held tool.

## Interactions

- Interactions registered here land after every vanilla registration, and `attempt_interact()` scans in registration order with the first active interaction taking the press, so a handler cannot shadow an active vanilla interaction on the same input.
- Vanilla world objects register `Interact`, and a few also register `SecondaryInteract`. Presses on mod interactions still flow through [input.take_press](input.take_press.md).
- Villagers, the player pet, and barn and coop animals have spawn-site hooks that fire synchronously and a frame earlier than this poll. Use [npc.created](npc.created.md), [pet.created](pet.created.md), and [animal.created](animal.created.md) for them instead.
- This poll and the [game.room_changed](game.room_changed.md) poll both run from the begin step drain with no ordering guarantee between them.

## Engine Wiring

- This event is emitted by the MMAPI framework itself. No engine seam sits behind it. `mmapi_instances_poll()` in `mmapi/mmapi_instances.gml` runs once per frame from the Game begin_step lifecycle drain (installed by the [`game_step_begin_installs`](../seams/game_step_begin_installs.md) engine fix), scans each registration's object with `with`, and dispatches once per instance it has not marked yet.

## See Also

- [npc.created](npc.created.md) - The synchronous spawn-site hook for villagers.
- [pet.created](pet.created.md) - The same moment for the farm pet.
- [animal.created](animal.created.md) - The same moment for barn and coop animals.
- [object.interact](object.interact.md) - Replace a grid object's interaction entirely.
- [input.take_press](input.take_press.md) - Veto a registered interaction's press.
