# Hook: pet.reward_generated

Observe each pet reward item as soon as it is generated.

`pet.reward_generated` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for registration and dispatch details.

## Contract

The event fires once for every concrete item appended to `PET.items_to_pop` by a scheduled pet job reward. It runs during the afternoon management transition, before the reward is presented or collected at the end of the day.

The callback receives:

- `ctx.pet` — the global `Pet` struct;
- `ctx.job` — the `PetJob` enum value that generated the item;
- `ctx.item` — the generated item id.

For forageable jobs, the event fires only when a forageable item was successfully rolled. The event is observation-only; the original reward queue remains engine-owned.

## Usage

```gml
function show_pet_reward_early(_ctx) {
    create_notification("Pet reward: " + item_id_to_string(_ctx.item));
}

mmapi_on("pet.reward_generated", show_pet_reward_early);
```

With no handlers, each reward item is appended exactly as in the pristine game.
