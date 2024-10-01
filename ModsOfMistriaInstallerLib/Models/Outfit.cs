using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Outfit
{
    private static Dictionary<string, List<string>> _validSlots = new()
    {
        { "back", ["back"] },
        { "facial_hair", ["facial_hair"] },
        { "top", ["dress", "robe", "top_misc", "suit", "long_sleeve", "sleeveless", "short_sleeve"] },
        { "eyes", ["eyes" ] },
        { "face_gear", ["face_accessory", "ear_accessory", "glasses"] },
        { "hair", ["medium_hair", "short_hair", "long_hair"] },
        { "head_gear", ["crown", "head_gear_misc", "hair_accessory", "hat", "helmet"] },
        { "bottom", ["pants", "bottom_misc", "shorts", "skirt"] },
        { "feet", ["boots", "feet_misc", "sandals", "shoes"] }
    };
    
    public string Name;

    public string Description;

    public string UiSlot;

    public bool DefaultUnlocked;

    public string UiSubCategory;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string LutFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string UiItem;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string OutlineFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public Dictionary<string, string> AnimationFiles;

    public Validation Validate(Validation validation, IMod mod, string file, string id)
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
        else if (!_validSlots.ContainsKey(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitUiSlotWrong, id, string.Join(", ", _validSlots.Keys)));
        }

        if (string.IsNullOrWhiteSpace(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitNoSubCategory, id));
        } else if (!string.IsNullOrEmpty(UiSlot) && _validSlots.ContainsKey(UiSlot) &&
                   !_validSlots[UiSlot].Contains(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorOutfitUiSubCategoryWrong, id, string.Join(", ", _validSlots[UiSlot])));
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
