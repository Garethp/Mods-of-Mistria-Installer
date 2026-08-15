# Engine Fix: save_load_blueprint_node_tolerance

Lets a save load when a placed construction ghost references a since-removed custom blueprint. The whole node is skipped with a logged warn instead of the load aborting.

`save_load_blueprint_node_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/__GridGeneral/Grid.gml` |
| **Locator** | text anchor on `load_objects`' unknown-object guard |
| **Op** | text (appended node guard) |
| **Marker** | `mmapi_save_blueprint_tolerance` |

## The Edit

Appended immediately after the existing unknown-object skip, before the node is written:

```gml
            var __mmapi_bp_name = obj[$ "blueprint_id"]; // mmapi_save_blueprint_tolerance
            if (__mmapi_bp_name != undefined) {
                var __mmapi_bp = undefined;
                for (var __mmapi_bi = 0; __mmapi_bi < Blueprint.LEN; __mmapi_bi++) {
                    if (blueprint_to_string(__mmapi_bi) == __mmapi_bp_name) {
                        __mmapi_bp = __mmapi_bi;
                        break;
                    }
                }
                if (__mmapi_bp == undefined) {
                    warn("MMAPI: save carried a construction node for unknown blueprint '{}' - node dropped", __mmapi_bp_name);
                    continue;
                }
            }
```

## Why

Placing a blueprint writes a construction node that records the blueprint by name, and the grid load resolves it with fatal `string_to_blueprint`. Blueprint prototypes are fiddle content, so mods can mint blueprint names, and a placed ghost for a removed custom building aborts the load natively.

Skipping the whole node is deliberate. Turn-in boxes dereference `BLUEPRINT_PROTOTYPES[node.blueprint_id]` on interact, so clearing the field alone would trade the load crash for an interact crash. The skip reuses the exact path unknown objects already take, and the cells load empty. Materials already turned in are lost with the ghost, which is the accepted amputation cost.

There is no attested `try_` variant for blueprints, so resolution is a `Blueprint.LEN` scan over `blueprint_to_string`, the same reflection the serializer uses when writing the node.

Zero-registrant inert: every vanilla blueprint name matches in the scan, so the guard passes through and the node loads exactly as before.
