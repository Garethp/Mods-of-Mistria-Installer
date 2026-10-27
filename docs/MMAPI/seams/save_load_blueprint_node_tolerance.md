# Engine Fix: save_load_blueprint_node_tolerance

A construction node for an unknown blueprint is skipped with a warn instead of aborting the load.

`save_load_blueprint_node_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/__GridGeneral/Grid.gml` |
| **Locator** | text anchor on `load_objects`' unknown-object guard |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_blueprint_tolerance` |

## The Edit

The replace appends a guard immediately after the existing unknown-object skip, before the node is written. It reads the node's optional `blueprint_id` through a struct accessor, with the marker on that line, and when one is present it searches the `Blueprint` members for one whose `blueprint_to_string` equals the name. A name that matches no member logs a warn naming the blueprint and skips the node with `continue`.

Placing a blueprint writes a construction node that records the blueprint by name, and the grid load resolves it with fatal `string_to_blueprint`. Blueprint prototypes are fiddle content, so mods can mint blueprint names, and a placed ghost for a removed custom building aborts the load natively.

Skipping the whole node is deliberate. Turn-in boxes dereference `BLUEPRINT_PROTOTYPES[node.blueprint_id]` on interact, so clearing the field alone would trade the load crash for an interact crash. The skip reuses the exact path unknown objects already take, and the cells load empty. Materials already turned in are lost with the ghost, which is the accepted cost.

The engine's own GML uses no tolerant converter for blueprints, so resolution is a `Blueprint.LEN` scan over `blueprint_to_string`, the same reflection the serializer uses when writing the node.

Every vanilla blueprint name matches in the scan, so on an intact install the guard passes through and the node loads exactly as before.

## See Also

- [save_load_used_objects_tolerance](save_load_used_objects_tolerance.md) - The unknown-object skip this guard extends.
- [Engine Fixes](../CATALOG.md#engine-fixes-and-the-call-rewrite) - The rest of the save-load tolerance family.
