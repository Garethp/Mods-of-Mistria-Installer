# Hook: pet.created

Customize the farm pet's instance as it spawns.

`pet.created` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `spawn_pet()`, after the `obj_pet` instance's create event has run, with the pet data attached, the FSM spawned, and the vanilla pet interactions registered. ctx is the `obj_pet` instance, not the `Animal` data struct. The pet's data is `ctx.me` (the `PET` global, whose `instance` field already points back at ctx), and `ctx.override_pet_initialization` carries the `PetInitialization` the spawn was requested with (`undefined` when the pet picks its own entry).

Every engine pet spawn routes through `spawn_pet()`: the pet management's room population, the adoption menu, the held-animal summon on a room change, and the Mist cutscene actor spawn, which requests `PetInitialization.Cutscene`. The pet is respawned as rooms change, so expect many fires per day and re-apply per-instance work on every fire.

| | |
| --- | --- |
| **Fires** | At the end of `spawn_pet()`, after the create event has run. |
| **ctx** | The `obj_pet` instance. Read `ctx.me` (the `PET` Animal struct) and `ctx.override_pet_initialization`. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. Mutating the instance is the intended use. |

## Usage

```gml
// pet.created is an EVENT: the return value is ignored.
// Fires on every pet spawn (each room entry).
function my_mod_pet_created(_pet) {
    // _pet is the obj_pet instance; _pet.me is the PET Animal struct.

    with (_pet) {
        // A "Wave" greeting on Throw, the open input (see Available Inputs).
        self.register_interaction(
            InputId.Throw,
            "my_mod_local/wave",          // ship a localization entry for this key
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
mmapi_on("pet.created", my_mod_pet_created);
```

The `local_key` argument labels the button prompt through the localizer, so ship a localization entry for it, or serve the key at runtime through [local.missing](local.missing.md).

### Available Inputs

The table below describes each input's availability for mod registration given the vanilla interactions already defined.

| Input | Availability for a mod interaction |
| --- | --- |
| Interact | Contested. `feed`, `pick_up`, and `check_pet` sit earlier, and `pick_up` is active in nearly every ordinary state, so a mod interaction fires only in narrow windows (the pet using a toy, or Ari carrying an animal). |
| SecondaryInteract | Contested. The vanilla pet registration is active in nearly every ordinary state. |
| Throw | Open. The pet registers nothing on Throw, so a mod interaction here is always reachable. |
| Ride, Jump, PickUpOne, the tool inputs | Unregistered on the pet. A take_press registration still consumes that input's global action while the prompt is active, so Jump eats jumps beside the pet. |

## Interactions

- Interactions registered here land after every vanilla registration, and `attempt_interact()` scans in registration order with the first active interaction taking the press. A handler therefore cannot shadow an active vanilla interaction on the same input. Presses on mod interactions still flow through [input.take_press](input.take_press.md).
- The pet's own vanilla interactions are all registered by its create event before this hook fires. [animal.pet](animal.pet.md) dispatches from `obj_player_animal` and never fires for the farm pet.

## Engine Wiring

- Seam [`pet_created`](../seams/pet_created.md) rewrites `spawn_pet()` in `gml/scripts/Pet.gml` to capture the created instance and emit with it.

## See Also

- [npc.created](npc.created.md) - The same moment for villagers.
- [animal.created](animal.created.md) - The same moment for barn/coop animals.
- [input.take_press](input.take_press.md) - Veto a registered interaction's press.
