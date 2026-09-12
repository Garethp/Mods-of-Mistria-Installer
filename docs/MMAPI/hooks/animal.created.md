# Hook: animal.created

Customize each barn/coop animal instance as it spawns.

`animal.created` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `spawn_animal()`, once per `obj_player_animal` instance the engine creates, after the create event has run and after `spawn_animal()` writes the new instance onto the `Animal` struct. ctx is the `obj_player_animal` instance, not the `Animal` data struct. The data is `ctx.me`, and `ctx.me.instance` already points back at ctx.

Instances are transient. The engine spawns them from `Stable.on_room_start()` whenever the player enters a location where a stable's animals are present, and from the held-animal summon when a carried animal rides along on a room change. A fire marks instance creation, not a new animal joining the farm. An adoption fires nothing until the player next enters the new animal's location. Expect many fires per animal per day, and re-apply per-instance work on every fire.

| | |
| --- | --- |
| **Fires** | At the end of `spawn_animal()`, after the create event and the `animal.instance` write-back. |
| **ctx** | The `obj_player_animal` instance. Its `Animal` data is `ctx.me`. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. Mutating the instance is the intended use. |

## Usage

```gml
// animal.created is an EVENT: the return value is ignored.
// Fires on every spawn of every present animal (each room entry).
function my_mod_animal_created(_animal) {
    // _animal is the obj_player_animal instance; _animal.me is the Animal
    // struct (kind, variant, sex, name, idx, hearts, production state).
    with (_animal) {
        // A "Wave" greeting on Throw, the open input (see Available Inputs).
        self.register_interaction(
            InputId.Throw,
            "my_mod_local/wave",        // ship a localization entry for this key
            function() {
                self.me.add_heart_points(5);
                self.bark_emitter.emit(BarkId.CuteFace, BarkType.Thought);
            },
            function() {
                return true;
            }
        );
    }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("animal.created", my_mod_animal_created);
```

The `local_key` argument labels the button prompt through the localizer, so ship a localization entry for it, or serve the key at runtime through [local.missing](local.missing.md).

### Available Inputs

The table below describes each input's availability for mod registration given the vanilla interactions already defined.

| Input | Availability for a mod interaction |
| --- | --- |
| Interact | Contested. `feed` and the per-species `pet` or `pick_up` sit earlier, and the petting registration is active in nearly every ordinary state. |
| SecondaryInteract | Contested. The `inspect` registration (the animal journal) is active in nearly every ordinary state. |
| Throw | Open. Barn/coop animals register nothing on Throw, so a mod interaction here is always reachable. |
| Ride, Jump, PickUpOne, the tool inputs | Unregistered on animals. A take_press registration still consumes that input's global action while the prompt is active, and Ride additionally fights mounting beside rideable species. |

## Interactions

- Interactions registered here land after every vanilla registration, and `attempt_interact()` scans in registration order with the first active interaction taking the press. A handler therefore cannot shadow an active vanilla interaction on the same input. Presses on mod interactions still flow through [input.take_press](input.take_press.md).
- [animal.pet](animal.pet.md) fires later, when the player pets this animal or puts it down. Its ctx is the same instance this hook handed out.

## Engine Wiring

- Seam [`animal_created`](../seams/animal_created.md) dispatches from `gml/scripts/GameplaySystems/Ranching/AnimalUtils.gml`, after `spawn_animal()`'s whole `instance_create_layer` assignment.

## See Also

- [npc.created](npc.created.md) - The same moment for villagers.
- [pet.created](pet.created.md) - The same moment for the farm pet.
- [animal.pet](animal.pet.md) - The player pets or puts down an animal.
- [animal.heart_points](animal.heart_points.md) - Adjust the heart points an animal gains.
