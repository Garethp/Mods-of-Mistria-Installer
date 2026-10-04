# Extension Points

[← MMAPI](MMAPI.md)

An **extension point** lets a mod add a new member to one of the engine's own `enums`.

While a [seam](SEAMS.md) observes or filters behavior at a point in a function, an extension point grows compile-time identity. MOMI generates the enum member, and every other line the engine needs to carry it, from templates the catalog declares for each point.

> [!TIP]
> This page is the technical reference. For a start-to-finish walkthrough that builds a working custom villager, see [Your First Custom NPC](CUSTOM_NPC.md).

## Determining When You Need One

| You want to... | Use |
| -------------- | --- |
| React to or change behavior at a point in a game function. | A [hook](HOOKS.md), no extension point needed. |
| Add a custom **spell**, **perk**, **monster**, or **blueprint**. | Plain fiddle content. Include the `fiddle/` file in your mod, and the engine derives those entries from data at boot. See [Custom Spells](CUSTOM_SPELLS.md) and [Custom Perks](CUSTOM_PERKS.md). |
| Add entirely new functions or objects. | Your mod's own `gml/`. |
| Add a custom **status effect**. | The `status_effect` extension point. See [Custom Status Effects](CUSTOM_STATUS_EFFECTS.md). |
| Add a custom **NPC**. | The `npc_roster` extension point. See [Your First Custom NPC](CUSTOM_NPC.md). |

> [!NOTE]
> The determination is ultimately decided by *what* declares the enum. If the enum is explicitly declared in shipped GML, MOMI can extend it.

## Registering

A **registration** is a single TOML file at `momi/extensions/<point>/<name>.toml`. The file name is your addition's **short name**, and the file's fields are whatever the point declares. A point with no fields takes an empty file:

```toml
# momi/extensions/status_effect/well_rested.toml
```

Each point's page lists its fields, the files it requires beside the registration, and the lines MOMI generates for it.

MOMI validates every registration before anything is written, the same way it checks GML. A registration problem skips the whole mod.

### Short Names

The **short name** is what you use to reference your addition throughout your mod's *content* (non-GML) files.

> [!IMPORTANT]
> Keep it short and simple. Use lowercase letters, numbers, and underscores only. Start with a letter, and use at most 41 characters.

### Symbols

During install, MOMI derives a full **symbol** from the short name. A **symbol** is your addition's permanent identity, which is used to avoid naming conflicts between mods.

A symbol has the form `<author>_<mod>_<short name>`. The author and mod pieces come from your manifest, lowercased with punctuation stripped. The short name piece is used verbatim.

Use the full **symbol** in `gml/` files for enum references, such as `StatusEffectId.author_mymod_well_rested`.

> [!IMPORTANT]
> The stripped `<author>` must start with a letter, and the whole symbol may not exceed 81 characters.

## The Points

| Point | Adds | Guide |
| ----- | ---- | ----- |
| [status_effect](extensions/status_effect.md) | A `StatusEffectId` member. Your GML applies the effect, draws its HUD icon, and reacts when it ends. | [Custom Status Effects](CUSTOM_STATUS_EFFECTS.md) |
| [npc_roster](extensions/npc_roster.md) | An `NpcId` member with its object mappings, manifest macro and baseline schedule. Your mod ships the prototype, art, schedule and dialogue. | [Your First Custom NPC](CUSTOM_NPC.md) |

## Named Sprites

Any sprite your content references by name, such as a HUD icon or a perk tile, ships as a meta and png pair under `animations/`, at a vanilla-shaped path. The engine registers a named sprite for every pair it finds there.

A meta must carry the fields the engine loads for its sprite kind, and no `id`. MOMI assigns ids at install.

Every sprite also requires a partner shape file: a `poly_*` meta under `shapes/`, at the mirrored path, carrying the sprite's geometry. MOMI links each shape to its paired animation.

> [!TIP]
> The vanilla metas are the reference for which fields each sprite kind carries.

## Uninstalling

Removing your mod never breaks a player's save. MOMI maintains a **ledger** holding every symbol's ordinal and persists it invisibly, so every save reference keeps resolving. Saves made before your addition existed load fine too.

For a custom NPC, hearts, gift history and the rest of the relationship restore when the mod is reinstalled. See [npc_roster](extensions/npc_roster.md#vacancy) for the one exception.

The ledger also rebuilds itself when lost, from the names the saves carry and from the markers in the installed archive. `--reseed-check` on the command line reports what that rebuild would recover, without installing. See [Seams](SEAMS.md#check-the-ledger-reseed).

## Rules

- Reference a member as `<Enum>.<symbol>` in your own GML, such as `StatusEffectId.<symbol>`, or look it up with `mmapi_ext_id("<point>", "<symbol>")` and back with `mmapi_ext_symbol(point, ordinal)`. Both come from the generated registry (`mmapi_ext.gml`).
- Persist the **symbol**, never the ordinal, in your modsave.
- Registrations require a `minInstallerVersion` of at least `0.16.0`.
