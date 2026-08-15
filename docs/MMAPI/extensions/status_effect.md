# Extension Point: status_effect

One registration per custom status effect.

`status_effect` is an **extension point**. See [Extension Points](../EXTENSIONS.md).

## Registration

The registration is `momi/extensions/status_effect/<name>.toml` with no fields. An empty or comment-only file registers the id, and the symbol becomes the `StatusEffectId` member.

## Generated Sites

| Site | File | What one registrant gets |
| ---- | ---- | ------------------------ |
| `enum_member` | `gml/scripts/Player/StatusEffect.gml` | `<symbol> = <ordinal>,` before `LEN` |

## Driving the Effect

The manager is id-agnostic, and your mod does everything from its own `gml/`:

- Apply with `ARI.status_effects.register(mmapi_ext_id("status_effect", "<symbol>"), amount, start, finish)`. See [player.status_effect_register](../hooks/player.status_effect_register.md).
- React with [player.status_effect_expired](../hooks/player.status_effect_expired.md) or [player.status_effect_cancel](../hooks/player.status_effect_cancel.md), or poll `get_effect_value`.
- Draw a HUD icon with [status_effect.hud_icon](../hooks/status_effect.hud_icon.md). Without a handler the effect works but shows no icon.

## Vacancy

Vacancy-benign by construction: the enum member alone survives (saves round-trip active effects by name), the update loop skips absent entries, and an uninstalled mod's effect simply ticks out with no handlers.
