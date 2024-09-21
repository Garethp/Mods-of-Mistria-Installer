using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
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
            validation.AddError(mod, file, Resources.ErrorOutfitNoName);
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            validation.AddError(mod, file, Resources.ErrorOutfitNoDescription);
        }

        if (string.IsNullOrWhiteSpace(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitNoUiSlot, id));
        }

        if (string.IsNullOrWhiteSpace(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitNoSubCategory, id));
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
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitNoAnimation, id));
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