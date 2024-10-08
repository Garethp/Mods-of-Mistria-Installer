﻿using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class MistGenerator(): GenericGenerator("mist")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Cutscenes.Add(json);
    }
}