# Seam: crop_harvest_destroy

Filters the crop node destruction decision after a harvest.

`crop_harvest_destroy` is a **template filter seam**. It feeds [crop.harvest_destroy](../hooks/crop.harvest_destroy.md). Mod authors register handlers for the hook; they do not write seams. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Crops.gml` |
| **Locator** | the unique destruction decision after the forageable rule in `process_crop_harvest()` |
| **Op** | template filter |
| **Feeds** | [`crop.harvest_destroy`](../hooks/crop.harvest_destroy.md) |
| **Value filtered** | the boolean `destroy` |
| **Context** | `{ node: node, harvester_cardinal: harvester_cardinal }` |
| **Marker** | `mmapi_crop_run_harvest_destroy_filters` |

## Behavior

The filter runs after the engine has selected its normal managed/regrowing/forageable outcome and before the `if destroy` branch. With no handlers, the original boolean is used unchanged. The caller still owns rewards and XP; this seam only controls the crop node's lifecycle after the harvest.
