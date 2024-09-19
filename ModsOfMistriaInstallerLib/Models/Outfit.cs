using Garethp.ModsOfMistriaInstallerLib.Generator;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Outfit
{
    public string Name;

    public string Description;

    public string UiSlot;

    public bool DefaultUnlocked = false;

    public string UiSubCategory;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string LutFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string UiItem;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string OutlineFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public Dictionary<string, string> AnimationFiles;

    public Validation Validate(Validation validation, Mod mod, string file, string id)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            validation.AddError(mod, file, "Outfit has no name.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            validation.AddError(mod, file, "Outfit has no description.");
        }

        if (string.IsNullOrWhiteSpace(UiSlot))
        {
            validation.AddError(mod, file, $"Outfit {id} has not defined ui_slot.");
        }

        if (string.IsNullOrWhiteSpace(UiSubCategory))
        {
            validation.AddError(mod, file, $"Outfit {id} has not defined ui_sub_category.");
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s lutFile", LutFile) is { } lutError)
        {
            validation.AddError(mod, file, lutError);
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s uiItem", UiItem) is { } uiItemError)
        {
            validation.AddError(mod, file, uiItemError);
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s outlineFile", OutlineFile) is { } outlineError)
        {
            validation.AddError(mod, file, outlineError);
        }

        if (AnimationFiles.Count == 0)
        {
            validation.AddError(mod, file, $"Outfit {id} has no animation files.");
        }

        foreach (var animationType in AnimationFiles.Keys)
        {
            if (ValidationTools.CheckSpriteDirectoryExists(mod, $"Outfit {id}'s animation file {animationType}",
                    AnimationFiles[animationType]) is { } animationError)
            {
                validation.AddError(mod, file, animationError);
            }
        }

        return validation;
    }
}