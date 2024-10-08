﻿using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class AssetPartsInstaller() : GenericInstaller(["animation", "generated", "player_asset_parts"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.AssetParts;
}