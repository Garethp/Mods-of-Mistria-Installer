using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class NewObjectsGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();
        
        foreach (var file in mod.GetFilesInFolder("objects"))
        {
            var newObjects = JsonConvert.DeserializeObject<Dictionary<string, NewObject>>(mod.ReadFile(file));
            if (newObjects is null) continue;

            foreach (var objectId in newObjects.Keys)
            {
                var newObject = newObjects[objectId];
                newObject.Prefix = mod.GetId();
                newObject.Name = objectId;
                information.NewObjects.Add(newObject);
            }
        }

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("objects");

    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        foreach (var file in mod.GetFilesInFolder("objects"))
        {
            Dictionary<string, NewObject>? newObjects;

            try
            {
                newObjects = JsonConvert.DeserializeObject<Dictionary<string, NewObject>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (newObjects is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }

            if (newObjects.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.WarningObjectFileHasNoObjects);
            }
            
            foreach (var objectId in newObjects.Keys)
            {
                var newObject = newObjects[objectId];
                newObject.Name = objectId;
                validation = newObject.Validate(validation, mod, file, objectId);
            }
        }

        return validation;
    }
}